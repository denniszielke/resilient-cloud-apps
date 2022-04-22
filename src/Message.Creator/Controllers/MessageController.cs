using Message.Creator.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;  
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

namespace Message.Creator.Controllers
{

    [ApiController]
    [Route("api")]
    public class MessageController : ControllerBase
    {
        private readonly SinkClient _sinkClient;
        private readonly ILogger<MessageController> _logger;

        private readonly EventHubProducerClient _eventHubClient;
        public MessageController(SinkClient sinkClient,
                                 ILogger<MessageController> logger,
                                 EventHubProducerClient eventHubClient)
        {
            _sinkClient = sinkClient;
            _logger = logger;
            _eventHubClient = eventHubClient;
        }

        [HttpPost("/receive")]
        public async Task<IActionResult> Receive([FromBody] DeviceMessage message)
        {
            MessageResponse response = null;
            try
            {
                _logger.LogTrace($"received message {message.Id}");

                if (string.IsNullOrWhiteSpace(message.Id))
                {
                    return new BadRequestResult();
                }

                response = await _sinkClient.SendMessageAsync(message);   

                _logger.LogTrace($"written move {message}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                response = new MessageResponse(){
                        Id = message.Id, Status = MessageStatus.Failed, Sender = "message-creator", Host = Environment.MachineName
                    };
            }

            return new JsonResult(response);
        }

        [HttpPost("/publish")]
        public async Task<IActionResult> Publish([FromBody] DeviceMessage message)
        {
            MessageResponse response = null;
            try
            {
                _logger.LogTrace($"received message {message.Id}");

                if (string.IsNullOrWhiteSpace(message.Id))
                {
                    return new BadRequestResult();
                }
                
                using EventDataBatch eventBatch = await _eventHubClient.CreateBatchAsync();
                string jsonString = JsonSerializer.Serialize(message);

                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(jsonString)));
                
                await _eventHubClient.SendAsync(eventBatch);

                response = new MessageResponse(){
                        Id = message.Id, Status = MessageStatus.Ok, Sender = "message-creator", Host = Environment.MachineName
                    };

                _logger.LogTrace($"written move {message}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                response = new MessageResponse(){
                        Id = message.Id, Status = MessageStatus.Failed, Sender = "message-creator", Host = Environment.MachineName
                    };
            }

            return new JsonResult(response);
        }

    }

}
