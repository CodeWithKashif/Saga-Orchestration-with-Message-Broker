using System.Threading.Tasks;
using VehicleManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace VehicleManagementAPI.Services
{
    public interface IVehicleService
    {
        Task<bool> RegisterAsync([FromBody] Vehicle vehicle);
        Task<bool> UndoRegisterAsync(string licenseNumber);
    }
}