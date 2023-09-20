using Contonance.Shared;

namespace Contonance.Backend.Repositories
{
    public class RepairReportsRepository
    {
        private readonly ILogger<RepairReportsRepository> _logger;
        private readonly List<RepairReport> _inmemoryData;
        private static readonly string[] Titles = new[]
            {
                "Hull breach in section 12",
                "Engine malfunction",
                "Broken airlock",
                "Broken toilet",
                "Broken replicator"
            };

        public RepairReportsRepository(ILogger<RepairReportsRepository> logger)
        {
            _logger = logger;

            // Just as a placeholder, we'll use an in-memory list of repair reports, normall this class will reach out to a database
            _inmemoryData = Enumerable.Range(0, 4).Select(index => new RepairReport
            {
                Title = Titles[index],
                DueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Severity = (Severity)Random.Shared.Next(0, 3)
            }).ToList();
        }

        public IList<RepairReport> GetAll()
        {
            return _inmemoryData;
        }

        public void AddIfNew(RepairReport repairReport)
        {
            var alreadySavedReport = _inmemoryData.FirstOrDefault(_ => _.Id == repairReport.Id);
            if (alreadySavedReport == null)
            {
                _inmemoryData.Add(repairReport);
            }
        }
    }
}
