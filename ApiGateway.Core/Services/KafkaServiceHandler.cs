using ApiGateway.Core.Interface;
using ApiGateway.Core.Modal;
using ApiGateway.Core.Service;
using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using Confluent.Kafka;
using ModalLayer.Modal;
using Newtonsoft.Json;

namespace ApiGateway.Core.Services
{
    public class KafkaServiceHandler : IKafkaServiceHandler
    {
        private readonly RequestMicroservice _requestMicroservice;
        private readonly MicroserviceRegistry _microserviceRegistry;
        private readonly MasterConnection _masterConnection;

        public KafkaServiceHandler(
            MicroserviceRegistry microserviceRegistry,
            RequestMicroservice requestMicroservice,
            MasterConnection masterConnection)
        {
            _microserviceRegistry = microserviceRegistry;
            _requestMicroservice = requestMicroservice;
            _masterConnection = masterConnection;
        }

        public async Task DailyJobManager(ConsumeResult<Ignore, string> result)
        {
            KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(result.Message.Value);

            // Load all database configuration from master database
            if (_masterConnection.GetDatabaseConfiguration == null || _masterConnection.GetDatabaseConfiguration.Count == 0)
            {
                await _masterConnection.LoadMasterConnection();
            }

            List<DbConfig> dbConfig = _masterConnection.GetDatabaseConfiguration;

            foreach (var x in dbConfig)
            {
                try
                {
                    var microserviceRequest = await CreateRequestObject(kafkaPayload.Message, x);
                    switch (kafkaPayload.kafkaServiceName)
                    {
                        case KafkaServiceName.MonthlyLeaveAccrualJob:
                            await CallLeaveAccrualJobAsync(microserviceRequest);
                            break;
                        case KafkaServiceName.WeeklyTimesheetJob:
                            await CallTimesheetJobAsync(microserviceRequest);
                            break;
                        case KafkaServiceName.MonthlyPayrollJob:
                            await CallPayrollJobAsync(microserviceRequest);
                            break;
                        case KafkaServiceName.YearEndLeaveProcessingJob:
                            await CallLeaveYearEndJobAsync(microserviceRequest);
                            break;
                        case KafkaServiceName.NewRegistration:
                            await CallGenerateAttendanceAsync(microserviceRequest);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw HiringBellException.ThrowBadRequest($"[DAIALY JOB ERROR]: Got error: {ex.Message}");
                }
            }
        }

        private async Task<MicroserviceRequest> CreateRequestObject(string payloadMessage, DbConfig dbConfig)
        {
            MicroserviceRequest microserviceRequest = MicroserviceRequest.Builder(string.Empty);
            microserviceRequest.Database = dbConfig;
            microserviceRequest.Payload = payloadMessage;
            microserviceRequest.CompanyCode = $"{dbConfig.OrganizationCode}{dbConfig.Code}";
            microserviceRequest.Token = await GetJwtToken(dbConfig);

            if (string.IsNullOrEmpty(microserviceRequest.Token))
            {
                throw HiringBellException.ThrowBadRequest("Invalid token received");
            }

            microserviceRequest.Token = $"{ApplicationConstants.JWTBearer} {microserviceRequest.Token}";
            return microserviceRequest;
        }

        private async Task<string> GetJwtToken(DbConfig dbConfig)
        {
            MicroserviceRequest microserviceRequest = MicroserviceRequest.Builder(string.Empty);
            microserviceRequest.Url = _microserviceRegistry.GenerateJWtToken;
            microserviceRequest.CompanyCode = dbConfig.OrganizationCode + dbConfig.Code;
            microserviceRequest.Token = ApplicationConstants.JWTBearer;
            microserviceRequest.Database = dbConfig;
            microserviceRequest.Payload = JsonConvert.SerializeObject(new CurrentSession { CompanyCode = microserviceRequest.CompanyCode });

            return await _requestMicroservice.PostRequest<string>(microserviceRequest);
        }

        public async Task CallLeaveAccrualJobAsync(MicroserviceRequest microserviceRequest)
        {
            microserviceRequest.Url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(microserviceRequest);
        }

        public async Task CallTimesheetJobAsync(MicroserviceRequest microserviceRequest)
        {
            microserviceRequest.Url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(microserviceRequest);
        }

        public async Task CallPayrollJobAsync(MicroserviceRequest microserviceRequest)
        {
            microserviceRequest.Url = $"{_microserviceRegistry.RunPayroll}/{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}";
            await _requestMicroservice.GetRequest<string>(microserviceRequest);
        }

        public async Task CallLeaveYearEndJobAsync(MicroserviceRequest microserviceRequest)
        {
            microserviceRequest.Url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(microserviceRequest);
        }

        public async Task CallGenerateAttendanceAsync(MicroserviceRequest microserviceRequest)
        {
            microserviceRequest.Url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(microserviceRequest);
        }

        public Task SendEmailNotification(dynamic attendanceRequestModal)
        {
            throw new NotImplementedException();
        }
    }
}