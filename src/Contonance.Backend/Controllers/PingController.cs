using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Contonance.Backend.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class PingController : ControllerBase
    {
        private readonly ILogger<PingController> _logger;

        public PingController(ILogger<PingController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/ping", Name = "Get")]
        public string Get()
        {
            return "Pong!";
        }

        [HttpGet("/getversion", Name = "getversion")]
        public string GetVersion()
        {
            return Environment.GetEnvironmentVariable("VERSION")!;
        }
    }
}