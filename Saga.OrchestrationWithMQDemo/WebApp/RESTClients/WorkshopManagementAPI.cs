using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApp.RESTClients
{
    public class WorkshopManagementAPI : RESTClientsBase, IWorkshopManagementAPI
    {
        private readonly ILogger<WorkshopManagementAPI> _logger;

        public WorkshopManagementAPI(IConfiguration config, ILogger<WorkshopManagementAPI> logger) 
            : base(config, "WorkshopManagementAPI")
        {
            _logger = logger;
        }

        public async Task<HttpResponseMessage> SendMaintenanceJobScheduleDetailEmail(string emailAddress)
        {
            HttpResponseMessage httpResponse= await Get("WorkshopPlanning/SendMaintenanceJobScheduleDetailEmail/", emailAddress);
            _logger.LogInformation($"Maintenance Job Schedule Detail Email sent to {emailAddress}");

            return httpResponse;
        }
    }
}
