using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Message.Sink.Controllers { 

    [ApiController]
    [Route("[controller]")]
    public class PingController : ControllerBase
    {
        private readonly ILogger<PingController> _logger;

        public PingController(ILogger<PingController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "Get")]
        public string Get()
        {
            return "Pong!";
        }
    }
}