using Bt.Lib.Common.Service.Model;

namespace ApiGateway.Core.Modal
{
    public class MicroserviceRegistry : MicroserviceBaseRegistry
    {
        public string RunPayroll { get; set; }
        public string SalaryDeclarationCalculation { set; get; }
        public string UpdateBulkDeclarationDetail { get; set; }
        public string CalculateSalaryDetail { get; set; }
        public string GetEmployeeDeclarationDetailById { get; set; }
    }
}
