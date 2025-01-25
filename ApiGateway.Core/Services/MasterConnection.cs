using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.Services.Code;
using Bt.Lib.PipelineConfig.Services;
using ModalLayer.Modal;
using MySql.Data.MySqlClient;
using System.Data;

namespace ApiGateway.Core.Service
{
    public class MasterConnection
    {
        private List<DbConfig> _dbConfig;
        private readonly GitHubConnector _gitHubConnector;
        private readonly string _dbConfigUrl;

        public MasterConnection(string url)
        {
            _dbConfigUrl = url;
            _gitHubConnector = new GitHubConnector();
            _dbConfig = new List<DbConfig>();
            LoadMasterConnection().GetAwaiter().GetResult();
        }

        public List<DbConfig> GetDatabaseConfiguration { get { return _dbConfig; } }

        public async Task<bool> LoadMasterConnection()
        {
            var flag = false;
            var cs = await _gitHubConnector.FetchTypedConfiguraitonAsync<string>(_dbConfigUrl);

            using (var connection = new MySqlConnection(cs))
            {
                using (MySqlCommand command = new MySqlCommand())
                {
                    try
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = "select * from database_connections";
                        using (MySqlDataAdapter dataAdapter = new MySqlDataAdapter())
                        {
                            var dataSet = new DataSet();
                            connection.Open();
                            dataAdapter.SelectCommand = command;
                            dataAdapter.Fill(dataSet);

                            if (dataSet.Tables == null || dataSet.Tables.Count != 1 || dataSet.Tables[0].Rows.Count == 0)
                            {
                                throw new Exception("Fail to load the master data");
                            }

                            _dbConfig = Converter.ToList<DbConfig>(dataSet.Tables[0]);
                            flag = true;
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            return flag;
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

        public async Task<List<DbConfig>> GetAllConnections()
        {
            if (_dbConfig == null)
            {
                await LoadMasterConnection();
                if (_dbConfig == null)
                    throw new Exception("Master data configuration detail not found");
            }

            return _dbConfig;
        }
    }
}
