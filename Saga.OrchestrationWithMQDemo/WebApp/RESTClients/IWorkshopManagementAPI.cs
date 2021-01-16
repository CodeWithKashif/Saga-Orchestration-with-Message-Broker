using System.Net.Http;
using System.Threading.Tasks;

namespace WebApp.RESTClients
{
    public interface IWorkshopManagementAPI
    {
        Task<HttpResponseMessage> SendMaintenanceJobScheduleDetailEmail(string emailAddress);
    }
}
