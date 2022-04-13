# Building resilient applications in Azure


Chaos engineering, fault injection testing, resiliency patterns, designing for failure - so many design principles and topics and still is reliability often times an afterthought.  
Let us run together a "game day" for resilience validation of our live Azure application.  
Take a look how we use **Azure Chaos Studio** for doing resilience validation, and how we measure, understand, and improve resilience against real-world incidents using **resiliency patterns in code**.  
We will also show you how you can use **Azure Monitor with Application Insights** to compare and understand the availability impact of your patterns.

## Launch locally
- create app insights, eventhub, blob storage
- adjust environment variables in local.env accordingly
- launch debug and open http://localhost:3000

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
