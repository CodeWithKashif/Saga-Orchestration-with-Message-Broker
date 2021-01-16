using System;

namespace WebApp.Models
{
    public class MaintenanceJob : ModelBase
    {
        public Guid JobId { get; set; }
        public DateTime PlanningDate { get; set; }
        public string OwnerId { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Notes { get; set; }
        public bool GenerateDemoError { get; set; }

    }
}