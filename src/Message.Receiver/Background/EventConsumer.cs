using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;

namespace Message.Receiver.Background
{
    public class EventConsumer : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<EventConsumer> _logger;

        private EventProcessorClient _processor;

        public EventConsumer(IConfiguration configuration,
                             BlobServiceClient blobServiceClient,
                             ILogger<EventConsumer> logger)
        {
            _configuration = configuration;
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"EventConsumer is starting.");

            string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

            var eventHubName = _configuration.GetValue<string>("EventHub:EventHubName");
            var eventHubConnectionString = _configuration.GetValue<string>("EventHub:EventHubConnectionString");

            var blobContainerClient = _blobServiceClient.GetBlobContainerClient("checkpoint-store");
            await blobContainerClient.CreateIfNotExistsAsync();
            _processor = new EventProcessorClient(blobContainerClient, consumerGroup, eventHubConnectionString, eventHubName);

            _processor.ProcessEventAsync += ProcessEventHandler;
            _processor.ProcessErrorAsync += ProcessErrorHandler;

            await _processor.StartProcessingAsync();

            _logger.LogDebug($"EventConsumer started.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"EventConsumer is stopping.");

            await _processor.StopProcessingAsync();
        }

        private Task ProcessEventHandler(ProcessEventArgs arg)
        {
            _logger.LogTrace($"received message {arg.Data.MessageId}");

            return Task.CompletedTask;
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs arg)
        {
            _logger.LogError($"exception while processing {arg.Exception}");

            return Task.CompletedTask;
        }
    }
}