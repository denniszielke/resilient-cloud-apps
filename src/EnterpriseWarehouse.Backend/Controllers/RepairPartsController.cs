using Microsoft.AspNetCore.Mvc;

namespace EnterpriseWarehouse.Backend.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class RepairPartsController : ControllerBase
    {
        private readonly ILogger<RepairPartsController> _logger;
        private readonly IMessageStorageService _messageStorageService;

        public RepairPartsController(ILogger<RepairPartsController> logger, IMessageStorageService messageStorageService)
        {
            _logger = logger;
            _messageStorageService = messageStorageService;
        }

        [HttpPost("order")]
        public async Task Order([FromBody] int repairPartId)
        {
            _logger.LogTrace($"received order of {repairPartId}");
            await _messageStorageService.SaveOrderAsync(repairPartId);
            _logger.LogTrace($"saved order of {repairPartId}");
        }
    }
}
