namespace WorkshopManagementAPI.Controllers
{
    public class ApiEndPoint
    {
        public const string PlanMaintenanceJobAsync = "PlanMaintenanceJob";
        public const string UndoPlanMaintenanceJobAsync = "UndoPlanMaintenanceJob/{jobId}";
        public const string SendMaintenanceJobScheduleDetailEmail = "SendMaintenanceJobScheduleDetailEmail/{emailAddress}";
    }
}