using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.Services.Code;
using Bt.Lib.Common.Service.Model;
using Bt.Lib.Common.Service.Services;
using ModalLayer.Modal;
using MySql.Data.MySqlClient;
using System.Data;

namespace ApiGateway.Core.Service
{
    public class MasterConnection
    {
        private List<DbConfigModal> _dbConfigModal;
        private DatabaseConfiguration _databaseConfiguration;
        private readonly GitHubConnector _gitHubConnector;
        private readonly string _dbConfigUrl;

        public MasterConnection(string url)
        {
            _dbConfigUrl = url;
            _gitHubConnector = new GitHubConnector();
            _dbConfigModal = new List<DbConfigModal>();
            LoadMasterConnection().GetAwaiter().GetResult();
        }

        public List<DbConfigModal> GetDatabaseConfiguration { get { return _dbConfigModal; } }

        public async Task<bool> LoadMasterConnection()
        {
            var flag = false;
            _databaseConfiguration = await _gitHubConnector.FetchTypedConfiguraitonAsync<DatabaseConfiguration>(_dbConfigUrl);
            var cs = DatabaseConfiguration.BuildConnectionString(_databaseConfiguration);

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

                            _dbConfigModal = Converter.ToList<DbConfigModal>(dataSet.Tables[0]);
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

        public async Task<DbConfigModal> GetDatabaseBasedOnCode(string orgCode, string companyCode)
        {
            DbConfigModal configuration = null;
            if (_dbConfigModal != null)
            {
                configuration = _dbConfigModal!.FirstOrDefault(x => x.OrganizationCode == orgCode && x.Code == companyCode);
                if (configuration == null)
                {
                    await LoadMasterConnection();
                    if (_dbConfigModal == null)
                    {
                        throw HiringBellException.ThrowBadRequest("Master data configuration detail not found");
                    }
                    else
                    {
                        configuration = _dbConfigModal!.FirstOrDefault(x => x.OrganizationCode == orgCode && x.Code == companyCode);
                        if (configuration == null)
                        {
                            throw HiringBellException.ThrowBadRequest("Invalid organization access. Please contact to admin.");
                        }
                    }
                }
            }

            return configuration;
        }

        public async Task<List<DbConfigModal>> GetAllConnections()
        {
            if (_dbConfigModal == null)
            {
                await LoadMasterConnection();
                if (_dbConfigModal == null)
                    throw new Exception("Master data configuration detail not found");
            }

            return _dbConfigModal;
        }
    }
}
