using ApiGateway.Core.Modal;
using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.Services.Code;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using System.Data;

namespace ApiGateway.Core.Service
{
    public class MasterConnection
    {
        private readonly MasterDatabase _masterDatabase;
        private List<DbConfigModal> _dbConfigModal;

        public MasterConnection(IOptions<MasterDatabase> options)
        {
            _masterDatabase = options.Value;
            LoadMasterConnection();
            _dbConfigModal = new List<DbConfigModal>();
        }

        public List<DbConfigModal> GetDatabaseConfiguration { get { return _dbConfigModal; } }

        public bool LoadMasterConnection()
        {
            var flag = false;
            string cs = $"server={_masterDatabase.Server};port={_masterDatabase.Port};database={_masterDatabase.Database};User Id={_masterDatabase.User_Id};password={_masterDatabase.Password};Connection Timeout={_masterDatabase.Connection_Timeout};Connection Lifetime={_masterDatabase.Connection_Lifetime};Min Pool Size={_masterDatabase.Min_Pool_Size};Max Pool Size={_masterDatabase.Max_Pool_Size};Pooling={_masterDatabase.Pooling};";
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

        public DbConfigModal GetDatabaseBasedOnCode(string orgCode, string companyCode)
        {
            DbConfigModal configuration = null;
            if (_dbConfigModal != null)
            {
                configuration = _dbConfigModal!.FirstOrDefault(x => x.OrganizationCode == orgCode && x.Code == companyCode);
                if (configuration == null)
                {
                    LoadMasterConnection();
                    if (_dbConfigModal == null)
                    {
                        throw new Exception("Master data configuration detail not found");
                    }
                    else
                    {
                        configuration = _dbConfigModal!.FirstOrDefault(x => x.OrganizationCode == orgCode && x.Code == companyCode);
                        if (configuration == null)
                        {
                            throw new Exception("Invalid organization access. Please contact to admin.");
                        }
                    }
                }
            }

            return configuration;
        }

        public List<DbConfigModal> GetAllConnections()
        {
            if (_dbConfigModal == null)
            {
                LoadMasterConnection();
                if (_dbConfigModal == null)
                    throw new Exception("Master data configuration detail not found");
            }

            return _dbConfigModal;
        }
    }
}
