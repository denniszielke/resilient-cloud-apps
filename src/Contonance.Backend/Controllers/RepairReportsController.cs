using Contonance.Backend.Repositories;
using Contonance.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Contonance.Backend.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class RepairReportsController : ControllerBase
    {
        private readonly RepairReportsRepository _repairReportsRepository;
        private readonly ILogger<RepairReportsController> _logger;

        public RepairReportsController(RepairReportsRepository repairReportsRepository, ILogger<RepairReportsController> logger)
        {
            _repairReportsRepository = repairReportsRepository;
            _logger = logger;
        }

        [HttpGet]
        public IList<RepairReport> GetAllRepairReports()
        {
            return _repairReportsRepository.GetAll();
        }
    }
}
