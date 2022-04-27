using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Message.Receiver.Clients;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Message.Receiver.Background
{
    public class EventConsumer : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly SinkClient _sinkClient;
        private readonly ILogger<EventConsumer> _logger;

        private EventProcessorClient _processor;

        private const int EventsBeforeCheckpoint = 25;
        private ConcurrentDictionary<string, int> _partitionEventCount = new ConcurrentDictionary<string, int>();

        public EventConsumer(IConfiguration configuration,
                             BlobServiceClient blobServiceClient,
                             SinkClient sinkClient,
                             ILogger<EventConsumer> logger)
        {
            _configuration = configuration;
            _blobServiceClient = blobServiceClient;
            _sinkClient = sinkClient;
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

        private async Task ProcessEventHandler(ProcessEventArgs arg)
        {
            try
            {
                if (arg.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _logger.LogTrace($"received message {arg.Data.MessageId}");
                var data = Encoding.UTF8.GetString(arg.Data.Body.ToArray());
                _logger.LogInformation(data);
                // "{\"id\":\"bd1ea8fa-1540-4eb4-b77e-fdf8998967a7\",\"temperature\":15,\"humidity\":20,\"name\":\"Dave\",\"message\":\"hi from Dave\",\"timestamp\":\"2022-04-19T16:33:26.818Z\"}"
                DeviceMessage message = null;
                try
                {
                    JsonDocument document = JsonDocument.Parse(data);
                    message = new DeviceMessage();
                    JsonElement root = document.RootElement;

                    message.Id = root.GetProperty("id").ToString();
                    message.Humidity = int.Parse(root.GetProperty("humidity").ToString());
                    message.Temperature = int.Parse(root.GetProperty("temperature").ToString());
                    message.Message = root.GetProperty("message").ToString();
                    message.Name = root.GetProperty("name").ToString();
                    message.Timestamp = root.GetProperty("timestamp").ToString();
                }
                catch (System.Exception ex)
                {
                    _logger.LogError("Failed to receive message", ex);
                }

                if (message == null)
                {
                    message = new DeviceMessage();
                    message.Id = Guid.NewGuid().ToString();
                    message.Humidity = 23;
                    message.Temperature = 34;
                    message.Message = data;
                    message.Name = "Random";
                }

                await _sinkClient.SendMessageAsync(message);

                // If the number of events that have been processed
                // since the last checkpoint was created exceeds the
                // checkpointing threshold, a new checkpoint will be
                // created and the count reset.

                string partition = arg.Partition.PartitionId;

                int eventsSinceLastCheckpoint = _partitionEventCount.AddOrUpdate(
                    key: partition,
                    addValue: 1,
                    updateValueFactory: (_, currentCount) => currentCount + 1);

                if (eventsSinceLastCheckpoint >= EventsBeforeCheckpoint)
                {
                    await arg.UpdateCheckpointAsync();
                    _partitionEventCount[partition] = 0;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessEventHandler");
            }
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs arg)
        {
            _logger.LogError(arg.Exception, "exception while processing");

            return Task.CompletedTask;
        }
    }
}