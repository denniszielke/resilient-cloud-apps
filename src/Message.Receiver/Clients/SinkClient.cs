using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;

namespace Message.Receiver.Clients
{
    public class SinkClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SinkClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendMessageAsync(DeviceMessage message)
        {
            var client = _httpClientFactory.CreateClient("Sink"); 
            // var client = _httpClientFactory.CreateClient("Sink_WithRetry"); 
            // var client = _httpClientFactory.CreateClient("Sink_WithRetryANdCircuitBreaking"); 

            await client.PostAsJsonAsync("/api/message/receive", 
            message, 
            new System.Text.Json.JsonSerializerOptions(){
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });
        }
    }
}