#!/bin/bash

set -e

# infrastructure deployment properties
PROJECT_NAME="$1"
LOCATION="$2"
REGISTRY_OWNER="$3"
IMAGE_TAG="$4"

if [ "$PROJECT_NAME" == "" ]; then
echo "No project name provided - aborting"
exit 0;
fi

if [ "$LOCATION" == "" ]; then
echo "No location provided - aborting"
exit 0;
fi

if [ "$REGISTRY_OWNER" == "" ]; then
echo "No registry owner provided - aborting"
exit 0;
fi

if [ "$IMAGE_TAG" == "" ]; then
echo "No image tag provided - aborting"
exit 0;
fi

if [[ $PROJECT_NAME =~ ^[a-z0-9]{5,9}$ ]]; then
    echo "project name $PROJECT_NAME is valid"
else
    echo "project name $PROJECT_NAME is invalid - only numbers and lower case min 5 and max 8 characters allowed - aborting"
    exit 0;
fi

RESOURCE_GROUP="$PROJECT_NAME-rg"

AZURE_CORE_ONLY_SHOW_ERRORS="True"

az deployment sub create \
  --name "deploy-infra_for_resilient-cloud-apps" \
  --location $LOCATION \
  --template-file ./infrastructure/main.bicep \
  --parameters projectName=$PROJECT_NAME registryOwner=$REGISTRY_OWNER imageTag=$IMAGE_TAG location=$LOCATION