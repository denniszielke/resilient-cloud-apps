using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace Message.Sink.Controllers
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
            try
            {
                _logger.LogTrace($"received message {message.Id}");
               
                
                if( string.IsNullOrWhiteSpace(message.Id)){
                    return new JsonResult(Ok());
                }

                _messageStorageService.SaveMessage(message);        

                _logger.LogTrace($"written move {message}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new JsonResult(Ok());
            }

            return new JsonResult(Ok());
        }

    }

}
