using System.Threading.Tasks;
using WebApp.ViewModels;

namespace WebApp.Saga
{
    public interface ISagaOrchestratorBackgroundService
    {
        Task StartProcessing(WorkShopManagementNewVM inputModel);
        Task<RegisterAndPlanJobSagaModel> GetDetailOnSagaComplete(string emailAddress);
    }
}