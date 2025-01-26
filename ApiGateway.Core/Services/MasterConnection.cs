using Bot.CoreBottomHalf.CommonModal;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using Bt.Lib.PipelineConfig.Services;
using ModalLayer.Modal;

namespace ApiGateway.Core.Service
{
    public class MasterConnection
    {
        private List<DbConfig> _dbConfig;
        private readonly string _dbConfigUrl;
        private readonly RequestMicroservice _requestMicroservice;

        public MasterConnection(string url, RequestMicroservice requestMicroservice)
        {
            _dbConfigUrl = url;
            _requestMicroservice = requestMicroservice;
            _dbConfig = new List<DbConfig>();
            LoadMasterConnection().GetAwaiter().GetResult();
        }

        public List<DbConfig> GetDatabaseConfiguration { get { return _dbConfig; } }

        public async Task<bool> LoadMasterConnection()
        {
            try
            {
                _dbConfig = await _requestMicroservice.GetRequest<List<DbConfig>>(MicroserviceRequest.Builder(_dbConfigUrl));
                return true;
            }
            catch
            {
                throw HiringBellException.ThrowBadRequest("Fail to load master database collection");
            }
        }

        public async Task<DbConfig> GetDatabaseBasedOnCode(string orgCode, string companyCode)
        {
            DbConfig configuration = null;
            if (_dbConfig != null)
            {
                configuration = _dbConfig!.FirstOrDefault(x => x.OrganizationCode == orgCode && x.Code == companyCode);
                if (configuration == null)
                {
                    await LoadMasterConnection();
                    if (_dbConfig == null)
                    {
                        throw HiringBellException.ThrowBadRequest("Master data configuration detail not found");
                    }
                    else
                    {
                        configuration = _dbConfig!.FirstOrDefault(x => x.OrganizationCode == orgCode && x.Code == companyCode);
                        if (configuration == null)
                        {
                            throw HiringBellException.ThrowBadRequest("Invalid organization access. Please contact to admin.");
                        }
                    }
                }
            }

            return configuration;
        }
    }
}
