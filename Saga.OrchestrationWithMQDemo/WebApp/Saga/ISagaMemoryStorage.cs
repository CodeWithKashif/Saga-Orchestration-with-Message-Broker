using System.Collections.Generic;

namespace WebApp.Saga
{
    public interface ISagaMemoryStorage
    {
        void Add(RegisterAndPlanJobSagaModel report);
        IEnumerable<RegisterAndPlanJobSagaModel> Get();
        RegisterAndPlanJobSagaModel GetByEmailAddress(string emailAddress);
        RegisterAndPlanJobSagaModel GetByLicenseNumber(string licenseNumber);
        void Remove(RegisterAndPlanJobSagaModel sagaModel);
    }
}