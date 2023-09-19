using Microsoft.AspNetCore.Mvc;

namespace EnterpriseWarehouse.Backend.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly ILogger<MessageController> _logger;
        private readonly IMessageStorageService _messageStorageService;

        public MessageController(ILogger<MessageController> logger, IMessageStorageService messageStorageService)
        {
            _logger = logger;
            _messageStorageService = messageStorageService;
        }

        [HttpPost("receive")]
        public async Task<IActionResult> Receive([FromBody] DeviceMessage message)
        {
            MessageResponse response = null;

            try
            {
                _logger.LogTrace($"received message {message.Id}");
               
                if( string.IsNullOrWhiteSpace(message.Id)){
                    return new BadRequestResult();
                }
                response = new MessageResponse();
                response.Id = message.Id;

                var returnCode = await _messageStorageService.SaveMessage(message);  

                var receivedResponse = new MessageResponse(){
                        Id = message.Id, Status = returnCode, Sender = "cosmosdb", Host = Environment.MachineName
                    };

                response.Status = returnCode;
                response.Sender = "message-sink";
                response.Dependency = receivedResponse;
                response.Host = Environment.MachineName;
                _logger.LogTrace($"written move {message}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                response = new MessageResponse(){
                        Id = message.Id, Status = MessageStatus.Failed, Sender = "message-sink", Host = Environment.MachineName
                    };
            }

            return new JsonResult(response);
        }

    }

}
