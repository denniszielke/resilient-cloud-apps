# Building resilient applications in Azure


Chaos engineering, fault injection testing, resiliency patterns, designing for failure - so many design principles and topics and still is reliability often times an afterthought.  
Let us run together a "game day" for resilience validation of our live Azure application.  

Take a look how we use **Azure App Configuration** to toggle various resilience scenarios, and how we measure, understand, and improve resilience against real-world incidents using **resiliency patterns in code**.
We will also show you how you can use **Azure Monitor with Application Insights** to compare and understand the availability impact of your patterns.

High Level Architecture:
![](/architecture.png)


## Deploy Azure resources
e.g. possible reagions:  
'westcentralus,eastus,westus,centralus,uksouth,westeurope,japaneast,northcentralus,eastus2'

```bash
PROJECT_NAME="asresapp1"
LOCATION="westeurope"
GITHUB_REPO_OWNER="/jplck"
IMAGE_TAG="latest"

bash ./deploy-infra.sh $PROJECT_NAME $LOCATION $GITHUB_REPO_OWNER $IMAGE_TAG
```

## Create config file
```bash
PROJECT_NAME="asresapp1"

bash ./create-config.sh $PROJECT_NAME
```

## Launch locally
- create azure resources by running infra script 
- create local config by running create config script or adjust environment variables in local.env accordingly
- launch debug and open http://localhost:5025
- 

## Resiliency patterns shown in this sample

* [**Queue-Based Load Leveling**](https://docs.microsoft.com/en-us/azure/architecture/patterns/queue-based-load-leveling)   
  Use a queue that acts as a buffer between a task and a service that it invokes, to smooth intermittent heavy loads
* [**Throttling**](https://docs.microsoft.com/en-us/azure/architecture/patterns/throttling)  
  Control the consumption of resources by an instance of an application, an individual tenant, or an entire service
* [**Circuit Breaker**](https://docs.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker)  
  Handle faults that might take a variable amount of time to fix when connecting to a remote service or resource
* [**Retry**](https://docs.microsoft.com/en-us/azure/architecture/patterns/retry)  
  Enable an application to handle anticipated, temporary failures when it tries to connect to a service or network resource by transparently retrying an operation that's previously failed

## Walkthrough

1. Opening 9 tabs and start the loop, showing that we are getting **throttled**
    ![](/img/throttling.png)
2. Showing source code of sink (program.cs), discuss throttling of Azure Services, custom rate limiting 
3. Discussing retries, showing retry mechanism in receiver, activating it,
    ![](/img/retry.png)
4. Discussing circuit breaker, showing circuit breaker mechanism in receiver, activating it
    ![](/img/retry.png)
5. showing AppInsights, with retries
    ![](/img/appmap.png)
6. Discussing chaos engineering, showing Chaos Studio and start experiment
    ![](/img/chaos_experiment.png)
7. Show dashboard, show numbers
8. Discuss fail fast, discuss Queue-Based Load Leveling
