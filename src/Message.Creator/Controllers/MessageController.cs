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
    [Route("api/[controller]")]
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

        [HttpPost("receive")]
        public async Task<IActionResult> Receive([FromBody] DeviceMessage message)
        {
            int responseCode = 200;
            try
            {
                _logger.LogTrace($"received message {message.Id}");

                if (string.IsNullOrWhiteSpace(message.Id))
                {
                    return new JsonResult(BadRequest());
                }

                responseCode = await _sinkClient.SendMessageAsync(message);

                _logger.LogTrace($"written move {message}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new JsonResult(BadRequest());
            }

            return new JsonResult(new StatusCodeResult(responseCode));
        }

        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromBody] DeviceMessage message)
        {
            int responseCode = 200;
            try
            {
                _logger.LogTrace($"received message {message.Id}");

                if (string.IsNullOrWhiteSpace(message.Id))
                {
                    return new JsonResult(BadRequest());
                }

                using EventDataBatch eventBatch = await _eventHubClient.CreateBatchAsync();
                string jsonString = JsonSerializer.Serialize(message);

                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(jsonString)));
                
                await _eventHubClient.SendAsync(eventBatch);

                _logger.LogTrace($"written move {message}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new JsonResult(BadRequest());
            }

            return new JsonResult(new StatusCodeResult(responseCode));
        }


    }

}
