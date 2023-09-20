using Microsoft.AspNetCore.Mvc;

namespace Contonance.WebPortal.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AppInsightsKeyController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<AppInsightsKeyController> _logger;

    public AppInsightsKeyController(IConfiguration config, ILogger<AppInsightsKeyController> logger)
    {
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    public string GetAppInsightsKey()
    {
        return _config.GetNoEmptyStringOrThrow("ApplicationInsights__ConnectionString");
    }
}