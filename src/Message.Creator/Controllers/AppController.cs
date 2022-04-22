using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Message.Creator.Controllers { 

    [ApiController]
    [Route("[controller]")]
    public class AppController : ControllerBase
    {
        private readonly ILogger<AppController> _logger;

        public AppController(ILogger<AppController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "getappinsightskey")]
        public string GetAppInsightsKey()
        {
            return "Pong!";
        }

        [HttpGet(Name = "getname")]
        public string GetName()
        {
            string[] names = {"Peter", "Steve", "Bill", "Dave", "Tom", "Tim", "Dale", "Ben", "Andy", "Mike", "Anne", "Cat", "Maria", "Lucy", "Kye", "Paula", "Lena", "Kelly", "Ringo", "Matt"};
            Random random = new Random();
            int index = random.Next(0, names.Length);
            return names[index];
        }
    }
}