using System.Collections.Generic;
using System.Linq;

namespace WebApp.Saga
{
    public class SagaMemoryStorage : ISagaMemoryStorage
    {
        private readonly IList<RegisterAndPlanJobSagaModel> _sagaModels = new List<RegisterAndPlanJobSagaModel>();

        public void Add(RegisterAndPlanJobSagaModel sagaModel)
        {
            _sagaModels.Add(sagaModel);
        }

        public IEnumerable<RegisterAndPlanJobSagaModel> Get()
        {
            return _sagaModels;
        }

        public RegisterAndPlanJobSagaModel GetByEmailAddress(string emailAddress)
        {
            return _sagaModels.FirstOrDefault(x => x.EmailAddress==emailAddress);
        }

        public RegisterAndPlanJobSagaModel GetByLicenseNumber(string licenseNumber)
        {
            return _sagaModels.FirstOrDefault(x => x.LicenseNumber == licenseNumber);
        }

        public void Remove(RegisterAndPlanJobSagaModel sagaModel)
        {
            _sagaModels.Remove(sagaModel);
        }
    }
}