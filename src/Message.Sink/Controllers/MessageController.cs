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
            MessageStatus returnCode = MessageStatus.Failed;

            try
            {
                _logger.LogTrace($"received message {message.Id}");
               
                
                if( string.IsNullOrWhiteSpace(message.Id)){
                    return new JsonResult(BadRequest());
                }

                // int n = 100;
                // Parallel.For(0, n, async i => {
                //     message.Id = message.Id + "i";
                    returnCode = await _messageStorageService.SaveMessage(message);  
                    // if (returnCode == MessageStatus.Throttled) 
                    // {
                    //     returnCode = MessageStatus.Throttled;
                    // }
                // });

                _logger.LogTrace($"written move {message}");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new JsonResult(new StatusCodeResult(500));
            }

            if(returnCode == MessageStatus.Throttled){
                return new JsonResult(new StatusCodeResult(429));
            }else if (returnCode == MessageStatus.Ok)
            {
                return new JsonResult(Ok());
            }

            return new JsonResult(new StatusCodeResult(500));
        }

    }

}
