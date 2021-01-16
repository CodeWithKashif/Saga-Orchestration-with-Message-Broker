using System.Net.Http;
using System.Threading.Tasks;

namespace WebApp.RESTClients
{
    public interface ICustomerManagementAPI
    {
        Task<HttpResponseMessage> SendWelcomeEmail(string emailAddress);
    }
}
