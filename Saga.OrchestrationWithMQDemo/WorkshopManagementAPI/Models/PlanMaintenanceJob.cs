using System;

namespace WorkshopManagementAPI.Models
{
    public class PlanMaintenanceJob
    {
        public Guid JobId { get; set; }
        public DateTime PlanningDate { get; set; }
        public string OwnerId { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Notes { get; set; }

        //Added for Demonstration purpose - this will throw exception in api
        public bool GenerateDemoError { get; set; }

    }

}