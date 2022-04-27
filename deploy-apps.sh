#!/bin/bash

set -e

# infrastructure deployment properties

PROJECT_NAME="$1" # here enter unique deployment name (ideally short and with letters for global uniqueness)
REGISTRY_OWNER="$2"
IMAGE_TAG="$3"
ENABLE_RATE_LIMITING="$4"
ENABLE_RETRY="$5"
ENABLE_BREAKER="$6"

if [ "$PROJECT_NAME" == "" ]; then
echo "No project name provided - aborting"
exit 0;
fi

if [[ $PROJECT_NAME =~ ^[a-z0-9]{5,8}$ ]]; then
    echo "project name $PROJECT_NAME is valid"
else
    echo "project name $PROJECT_NAME is invalid - only numbers and lower case min 5 and max 8 characters allowed - aborting"
    exit 0;
fi

RESOURCE_GROUP="$PROJECT_NAME"

if [ "$ENABLE_RATE_LIMITING" == "" ]; then
    echo "setting rate limiting to false"
    ENABLE_RATE_LIMITING=false
fi
if [ "$ENABLE_RETRY" == "" ]; then
    echo "setting retry to false"
    ENABLE_RETRY=false
fi
if [ "$ENABLE_BREAKER" == "" ]; then
    echo "setting breaker to false"
    ENABLE_BREAKER=false
fi

AZURE_CORE_ONLY_SHOW_ERRORS="True"

if [ $(az group exists --name $RESOURCE_GROUP) = false ]; then
    echo "resource group $RESOURCE_GROUP does not exist"
    error=1
else   
    echo "resource group $RESOURCE_GROUP already exists"
    LOCATION=$(az group show -n $RESOURCE_GROUP --query location -o tsv)
fi

KUBE_NAME=$(az aks list -g $RESOURCE_GROUP --query '[0].name' -o tsv)

if [ "$KUBE_NAME" == "" ]; then
    echo "no AKS cluster found in Resource Group $RESOURCE_GROUP"
    error=1
fi

echo "found cluster $KUBE_NAME"
echo "getting kubeconfig for cluster $KUBE_NAME"

az aks get-credentials --resource-group=$RESOURCE_GROUP --name=$KUBE_NAME --admin

CONTROLLER_ID=$(az aks show -g $RESOURCE_GROUP -n $KUBE_NAME --query identity.principalId -o tsv)
echo "controller id is $CONTROLLER_ID"
NODE_GROUP=$(az aks show -g $RESOURCE_GROUP -n $KUBE_NAME --query nodeResourceGroup -o tsv)
 
IP_ID=$(az network public-ip list -g $NODE_GROUP --query "[?contains(name, '$PROJECT_NAME')].id" -o tsv)
if [ "$IP_ID" == "" ]; then
    echo "creating ingress ip $NODE_GROUP"
    az network public-ip create -g $NODE_GROUP -n $PROJECT_NAME --sku STANDARD --dns-name $KUBE_NAME -o none
    IP_ID=$(az network public-ip show -g $NODE_GROUP -n $PROJECT_NAME -o tsv --query id)
    IP=$(az network public-ip show -g $NODE_GROUP -n $PROJECT_NAME -o tsv --query ipAddress)
    DNS=$(az network public-ip show -g $NODE_GROUP -n $PROJECT_NAME -o tsv --query dnsSettings.fqdn)
    echo "created ip $IP_ID with $IP on $DNS"
    az role assignment create --role "Contributor" --assignee $CONTROLLER_ID --scope $IP_ID -o none
else
    IP=$(az network public-ip show -g $NODE_GROUP -n $PROJECT_NAME -o tsv --query ipAddress)
    DNS=$(az network public-ip show -g $NODE_GROUP -n $PROJECT_NAME -o tsv --query dnsSettings.fqdn)
    echo "created ip $IP on $DNS"
fi

AI_CONNECTIONSTRING=$(az resource show -g $RESOURCE_GROUP -n appi-$PROJECT_NAME --resource-type "Microsoft.Insights/components" --query properties.ConnectionString -o tsv | tr -d '[:space:]')
BLOB_CONNECTIONSTRING=$(az storage account show-connection-string --name st$PROJECT_NAME --resource-group $RESOURCE_GROUP --query "connectionString" -o tsv)
EVENTHUB_CONNECTIONSTRING=$(az eventhubs namespace authorization-rule keys list --name RootManageSharedAccessKey --namespace-name evhns-$PROJECT_NAME --resource-group $RESOURCE_GROUP --query "primaryConnectionString" | tr -d '"')
EVENTHUB_NAME=$(az eventhubs eventhub show -g $RESOURCE_GROUP -n events --namespace-name evhns-$PROJECT_NAME --query name --output tsv)
COSMOS_CONNECTIONSTRING=$(az cosmosdb keys list --resource-group $RESOURCE_GROUP --name dbs$PROJECT_NAME --type connection-strings --query "connectionStrings[0].connectionString" -o tsv)

# echo $AI_CONNECTIONSTRING
# echo $BLOB_CONNECTIONSTRING
# echo $EVENTHUB_CONNECTIONSTRING
# echo $EVENTHUB_NAME
# echo $COSMOS_CONNECTIONSTRING


helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx

helm repo add chaos-mesh https://charts.chaos-mesh.org

helm repo update

if kubectl get namespace ingress; then
  echo -e "Namespace ingress found."
else
  kubectl create namespace ingress
  echo -e "Namespace ingress created."
fi

helm upgrade nginx-ingress ingress-nginx/ingress-nginx --install \
    --namespace ingress \
    --set controller.replicaCount=3 \
    --set controller.metrics.enabled=true \
    --set defaultBackend.enabled=true \
    --set controller.service.loadBalancerIP="$IP" \
    --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz  \
    --set controller.service.externalTrafficPolicy=Local --wait


kubectl create secret generic appconfig \
   --from-literal=applicationInsightsConnectionString=$AI_CONNECTIONSTRING \
   --from-literal=eventHubConnectionString=$EVENTHUB_CONNECTIONSTRING \
   --from-literal=eventHubName=$EVENTHUB_NAME \
   --from-literal=blobConnectionString=$BLOB_CONNECTIONSTRING \
   --from-literal=cosmosConnectionString=$COSMOS_CONNECTIONSTRING \
   --save-config --dry-run=client -o yaml | kubectl apply -f -

kubectl apply -f ./deploy-k8s/svc-message-creator.yaml
kubectl apply -f ./deploy-k8s/svc-message-receiver.yaml
kubectl apply -f ./deploy-k8s/svc-message-sink.yaml

replaces="s/{.registry}/$REGISTRY_OWNER/;";
replaces="$replaces s/{.tag}/$IMAGE_TAG/; ";
replaces="$replaces s/{.version}/$IMAGE_TAG/; ";
replaces="$replaces s/{.enableRateLimiting}/$ENABLE_RATE_LIMITING/; ";
replaces="$replaces s/{.enableRetry}/$ENABLE_RETRY/; ";
replaces="$replaces s/{.enableBreaker}/$ENABLE_BREAKER/; ";
replaces="$replaces s/{.dns}/$DNS/; ";

cat ./deploy-k8s/depl-message-creator.yaml | sed -e "$replaces" | kubectl apply -f -
cat ./deploy-k8s/depl-message-receiver.yaml | sed -e "$replaces" | kubectl apply -f -
cat ./deploy-k8s/depl-message-sink.yaml | sed -e "$replaces" | kubectl apply -f -

kubectl apply -f ./deploy-k8s/hpa-message-creator.yaml
kubectl apply -f ./deploy-k8s/hpa-message-receiver.yaml
kubectl apply -f ./deploy-k8s/hpa-message-sink.yaml

cat ./deploy-k8s/ingress.yaml | sed -e "$replaces" | kubectl apply -f -

if kubectl get namespace chaos-testing; then
  echo -e "Namespace chaos-testing found."
else
  kubectl create namespace chaos-testing
  echo -e "Namespace chaos-testing created."
fi

helm upgrade chaos-mesh chaos-mesh/chaos-mesh --install -n=chaos-testing --set chaosDaemon.runtime=containerd --set chaosDaemon.socketPath=/run/containerd/containerd.sock --version 2.1.5 --wait

echo "exposed nginx ingress on this ip:"
kubectl --namespace ingress get services -o wide nginx-ingress-ingress-nginx-controller

echo "deployed app into:"
echo $DNS