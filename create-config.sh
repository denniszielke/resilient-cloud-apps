#!/bin/bash

set -e

# infrastructure deployment properties

PROJECT_NAME="$1" # here enter unique deployment name (ideally short and with letters for global uniqueness)

if [ "$PROJECT_NAME" == "" ]; then
echo "No project name provided - aborting"
exit 0;
fi

if [[ $PROJECT_NAME =~ ^[a-z0-9]{5,9}$ ]]; then
    echo "project name $PROJECT_NAME is valid"
else
    echo "project name $PROJECT_NAME is invalid - only numbers and lower case min 5 and max 8 characters allowed - aborting"
    exit 0;
fi

RESOURCE_GROUP="$PROJECT_NAME"

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

cat template.env > local.env

echo "ApplicationInsights__ConnectionString=\"$AI_CONNECTIONSTRING\"" >> local.env
echo "EventHub__EventHubConnectionString=\"$EVENTHUB_CONNECTIONSTRING\"" >> local.env
echo "EventHub__EventHubName=\"$EVENTHUB_NAME\"" >> local.env
echo "EventHub__BlobConnectionString=\"$BLOB_CONNECTIONSTRING\"" >> local.env
echo "ConnectionStrings__CosmosApi=\"$COSMOS_CONNECTIONSTRING\"" >> local.env