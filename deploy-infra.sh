#!/bin/bash

set -e

# infrastructure deployment properties
RESOURCE_GROUP="$1"
LOCATION="$2"
PROJECT_NAME="$3" # here enter unique deployment name (ideally short and with letters for global uniqueness)

AZURE_CORE_ONLY_SHOW_ERRORS="True"

RESOURCE_GROUP=$DEPLOYMENT_NAME # here enter the resources group

if [ $(az group exists --name $RESOURCE_GROUP) = false ]; then
    echo "creating resource group $RESOURCE_GROUP..."
    az group create -n $RESOURCE_GROUP -l $LOCATION -o none
    echo "resource group $RESOURCE_GROUP created"
else   
    echo "resource group $RESOURCE_GROUP already exists"
    LOCATION=$(az group show -n $RESOURCE_GROUP --query location -o tsv)
fi

az deployment group create -g $RESOURCE_GROUP -f ./infrastructure/main.bicep \
          -p projectName=$PROJECT_NAME



