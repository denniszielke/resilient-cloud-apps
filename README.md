# Building resilient applications in Azure


Chaos engineering, fault injection testing, resiliency patterns, designing for failure - so many design principles and topics and still is reliability often times an afterthought.  
Let us run together a "game day" for resilience validation of our live Azure application.  
Take a look how we use **Azure Chaos Studio** for doing resilience validation, and how we measure, understand, and improve resilience against real-world incidents using **resiliency patterns in code**.  
We will also show you how you can use **Azure Monitor with Application Insights** to compare and understand the availability impact of your patterns.

High Level Architecture:
![](/architecture.png)

## Launch locally
- create app insights, eventhub, blob storage
- adjust environment variables in local.env accordingly
- launch debug and open http://localhost:3000

## Deploy Azure resources

```
RESOURCE_GROUP="reliabl6"
PROJECT_NAME="reliabl6"
LOCATION="northeurope"

bash ./deploy-infra.sh $RESOURCE_GROUP $LOCATION $PROJECT_NAME

```

## Deploy Apps into Cluster

```
RESOURCE_GROUP="reliabl6"
PROJECT_NAME="reliabl6"
GITHUB_REPO_OWNER="denniszielke"
IMAGE_TAG="latest"
ENABLE_RATE_LIMITING="true"
ENABLE_RETRY="false"
ENABLE_BREAKER="false"

bash ./deploy-apps.sh $RESOURCE_GROUP $PROJECT_NAME $GITHUB_REPO_OWNER $ENABLE_RATE_LIMITING $ENABLE_RETRY $ENABLE_BREAKER

```

# OPENING / on the UI does not work - YOU HAVE TO USE /index.html

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
