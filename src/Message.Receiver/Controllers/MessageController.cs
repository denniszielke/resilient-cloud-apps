using Message.Receiver.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace Message.Receiver.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly SinkClient _sinkClient;
        private readonly ILogger<MessageController> _logger;


        public MessageController(SinkClient sinkClient,
                                 ILogger<MessageController> logger)
        {
            _sinkClient = sinkClient;
            _logger = logger;
        }

        [HttpPost("receive")]
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
                        Id = message.Id, Status = MessageStatus.Failed, Sender = "message-receiver", Host = Environment.MachineName
                    };
            }

            return new JsonResult(response);
        }

    }

}
