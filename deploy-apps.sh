#!/bin/bash

set -e

# infrastructure deployment properties
RESOURCE_GROUP="$1"
PROJECT_NAME="$2" # here enter unique deployment name (ideally short and with letters for global uniqueness)
REGISTRY_OWNER="$3"
IMAGE_TAG="$4"

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

AI_CONNECTIONSTRING=$(az resource show -g $RESOURCE_GROUP -n appi-$PROJECT_NAME --resource-type "Microsoft.Insights/components" --query properties.ConnectionString -o tsv | tr -d '[:space:]')
BLOB_CONNECTIONSTRING=$(az storage account show-connection-string --name st$PROJECT_NAME --resource-group $RESOURCE_GROUP --query "connectionString" -o tsv)
EVENTHUB_CONNECTIONSTRING=$(az eventhubs namespace authorization-rule keys list --name RootManageSharedAccessKey --namespace-name evhns-$PROJECT_NAME --resource-group $RESOURCE_GROUP --query "primaryConnectionString" | tr -d '"')
EVENTHUB_NAME=$(az eventhubs eventhub show -g $RESOURCE_GROUP -n events --namespace-name evhns-$PROJECT_NAME --query name --output tsv)
COSMOS_CONNECTIONSTRING=$(az cosmosdb keys list --resource-group $RESOURCE_GROUP --name dbs$PROJECT_NAME --type connection-strings --query "connectionStrings[0].connectionString" -o tsv)

echo $AI_CONNECTIONSTRING
echo $BLOB_CONNECTIONSTRING
echo $EVENTHUB_CONNECTIONSTRING
echo $EVENTHUB_NAME
echo $COSMOS_CONNECTIONSTRING

kubectl create secret generic appconfig \
   --from-literal=applicationInsightsConnectionString=$AI_CONNECTIONSTRING \
   --from-literal=eventHubConnectionString=$EVENTHUB_CONNECTIONSTRING \
   --from-literal=eventHubName=$EVENTHUB_NAME \
   --from-literal=blobConnectionString=$BLOB_CONNECTIONSTRING 

kubectl apply -f ./deploy-k8s/svc-message-creator.yaml
kubectl apply -f ./deploy-k8s/svc-message-receiver.yaml
kubectl apply -f ./deploy-k8s/svc-message-sink.yaml

replaces="s/{.registry}/$REGISTRY_OWNER/;";
replaces="$replaces s/{.tag}/$IMAGE_TAG/; ";

cat ./deploy-k8s/depl-message-creator.yaml | sed -e "$replaces" #| ./kubectl apply -f -
cat ./deploy-k8s/depl-message-receiver.yaml | sed -e "$replaces" #| ./kubectl apply -f -
cat ./deploy-k8s/depl-message-sink.yaml | sed -e "$replaces" #| ./kubectl apply -f -

