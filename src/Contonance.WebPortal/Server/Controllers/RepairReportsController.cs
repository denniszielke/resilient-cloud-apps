using Microsoft.AspNetCore.Mvc;
using Contonance.WebPortal.Shared;

namespace Contonance.WebPortal.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class RepairReportsController : ControllerBase
{
    private static readonly string[] Titles = new[]
    {
        "Hull breach in section 12",
        "Engine malfunction",
        "Broken airlock",
        "Broken toilet",
        "Broken replicator"
    };

    private readonly ILogger<RepairReportsController> _logger;

    public RepairReportsController(ILogger<RepairReportsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IEnumerable<RepairReport> Get()
    {
        return Enumerable.Range(0, 4).Select(index => new RepairReport
        {
            Title = Titles[index],
            DueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Severity = (Severity)Random.Shared.Next(0, 3)
        })
        .ToArray();
    }
}
