using ApiGateway.Core.Interface;
using ApiGateway.Core.Modal;
using ApiGateway.Core.Service;
using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using Bt.Lib.Common.Service.MicroserviceHttpRequest;
using Bt.Lib.Common.Service.Model;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using ModalLayer;
using ModalLayer.Modal;
using Newtonsoft.Json;
using System.Net;

namespace ApiGateway.Core.Services
{
    public class KafkaServiceHandler : IKafkaServiceHandler
    {
        private readonly ILogger<KafkaServiceHandler> _logger;
        private readonly List<KafkaServiceConfig> _kafkaServiceConfig;
        private readonly RequestMicroservice _requestMicroservice;
        private readonly MicroserviceRegistry _microserviceRegistry;
        private readonly MasterConnection _masterConnection;

        public KafkaServiceHandler(
            ILogger<KafkaServiceHandler> logger,
            IOptions<List<KafkaServiceConfig>> kafkaOptions,
            IOptions<MicroserviceRegistry> microserviceOptions,
            RequestMicroservice requestMicroservice,
            MasterConnection masterConnection)
        {
            _logger = logger;
            _kafkaServiceConfig = kafkaOptions.Value;
            _microserviceRegistry = microserviceOptions.Value;
            _requestMicroservice = requestMicroservice;
            _masterConnection = masterConnection;
        }

        public async Task ScheduledJobManager()
        {
            var kafkaConfig = _kafkaServiceConfig.Find(x => x.Topic == LocalConstants.DailyJobsManager);
            if (kafkaConfig == null)
            {
                throw new HiringBellException($"No configuration found for the kafka", "service name", LocalConstants.DailyJobsManager, HttpStatusCode.InternalServerError);
            }

            var config = new ConsumerConfig
            {
                GroupId = kafkaConfig.GroupId,
                BootstrapServers = $"{kafkaConfig.ServiceName}:{kafkaConfig.Port}"
            };

            _logger.LogInformation($"[Kafka] Start listening kafka topic: {kafkaConfig.Topic}");
            using (var consumer = new ConsumerBuilder<Null, string>(config).Build())
            {
                consumer.Subscribe(kafkaConfig.Topic);
                while (true)
                {
                    try
                    {
                        _logger.LogInformation($"[Kafka] Waiting on topic: {kafkaConfig.Topic}");
                        var message = consumer.Consume();

                        _logger.LogInformation($"[Kafka] Message received: {message}");
                        if (message != null && !string.IsNullOrEmpty(message.Message.Value))
                        {
                            _logger.LogInformation(message.Message.Value);
                            await RunJobAsync(message.Message.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Kafka Error]: Got exception - {ex.Message}");
                    }

                    await Task.CompletedTask;
                }
            }
        }

        public async Task RunJobAsync(string payload)
        {
            KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(payload);

            // Load all database configuration from master database
            if (_masterConnection.GetDatabaseConfiguration == null || _masterConnection.GetDatabaseConfiguration.Count == 0)
            {
                _masterConnection.LoadMasterConnection();
            }

            List<DbConfigModal> dbConfig = _masterConnection.GetDatabaseConfiguration;

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
                    _logger.LogError($"[DAIALY JOB ERROR]: Got error: {ex.Message}");
                }
            }
        }

        private async Task<MicroserviceRequest> CreateRequestObject(string payloadMessage, DbConfigModal dbConfig)
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

        private async Task<string> GetJwtToken(DbConfigModal dbConfig)
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
