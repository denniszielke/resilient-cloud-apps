# Building resilient applications in Azure


Chaos engineering, fault injection testing, resiliency patterns, designing for failure - so many design principles and topics and still is reliability often times an afterthought.  
Let us run together a "game day" for resilience validation of our live Azure application.  
Take a look how we use **Azure Chaos Studio** for doing resilience validation, and how we measure, understand, and improve resilience against real-world incidents using **resiliency patterns in code**.  
We will also show you how you can use **Azure Monitor with Application Insights** to compare and understand the availability impact of your patterns.

High Level Architecture:
![](/architecture.png)


## Deploy Azure resources
Possible reagions (Azure Chaos Studio Preview restriction):  
'westcentralus,eastus,westus,centralus,uksouth,westeurope,japaneast,northcentralus,eastus2'

```
PROJECT_NAME="reliabl6"
LOCATION="westus"

bash ./deploy-infra.sh $PROJECT_NAME $LOCATION

```

## Create config file
```
PROJECT_NAME="reliabl6"
bash ./create-config.sh $PROJECT_NAME
```

## Launch locally
- create azure resources by running infra script 
- create local config by running create config script or adjust environment variables in local.env accordingly
- launch debug and open http://localhost:5025


## Deploy Apps into Cluster

```
PROJECT_NAME="reliabl6"
GITHUB_REPO_OWNER="denniszielke"
IMAGE_TAG="latest"
ENABLE_RATE_LIMITING="true"
ENABLE_RETRY="false"
ENABLE_BREAKER="false"

bash ./deploy-apps.sh $PROJECT_NAME $GITHUB_REPO_OWNER $ENABLE_RATE_LIMITING $ENABLE_RETRY $ENABLE_BREAKER

```

## Resiliency patterns shown in this sample

* [**Health Endpoint Monitoring**](https://docs.microsoft.com/en-us/azure/architecture/patterns/health-endpoint-monitoring)  
  Implement functional checks in an application that external tools can access through exposed endpoints at regular intervals
* [**Queue-Based Load Leveling**](https://docs.microsoft.com/en-us/azure/architecture/patterns/queue-based-load-leveling)   
  Use a queue that acts as a buffer between a task and a service that it invokes, to smooth intermittent heavy loads
* [**Throttling**](https://docs.microsoft.com/en-us/azure/architecture/patterns/throttling)  
  Control the consumption of resources by an instance of an application, an individual tenant, or an entire service
* [**Circuit Breaker**](https://docs.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker)  
  Handle faults that might take a variable amount of time to fix when connecting to a remote service or resource
* [**Retry**](https://docs.microsoft.com/en-us/azure/architecture/patterns/retry)  
  Enable an application to handle anticipated, temporary failures when it tries to connect to a service or network resource by transparently retrying an operation that's previously failed
