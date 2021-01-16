using System.Threading.Tasks;
using CustomerManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CustomerManagementAPI.Services
{
    public interface ICustomerService
    {
        Task<bool> RegisterAsync([FromBody] Customer customer);
        Task<bool> UndoRegisterAsync(string emailAddress);
    }
}