using ApiGateway.Core.Interface;
using ApiGateway.Core.Modal;
using ApiGateway.Core.Service;
using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using BottomhalfCore.Services.Interface;
using Confluent.Kafka;
using ems_CommonUtility.MicroserviceHttpRequest;
using ems_CommonUtility.Model;
using Microsoft.Extensions.Options;
using ModalLayer;
using ModalLayer.Modal;
using Newtonsoft.Json;
using System.Net;
using System.Security.Policy;

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
            List<DbConfigModal> dbConfig = new List<DbConfigModal>(); ;

            // Load all database configuration from master database
            if (_masterConnection.GetDatabaseConfiguration == null || _masterConnection.GetDatabaseConfiguration.Count == 0)
            {
                _masterConnection.LoadMasterConnection();
            }

            dbConfig = _masterConnection.GetDatabaseConfiguration;

            foreach (var x in dbConfig)
            {
                try
                {
                    CreateRequestObject(payload, x);
                    switch (kafkaPayload.kafkaServiceName)
                    {
                        case KafkaServiceName.MonthlyLeaveAccrualJob:
                            await CallLeaveAccrualJobAsync(payload);
                            break;
                        case KafkaServiceName.WeeklyTimesheetJob:
                            await CallTimesheetJobAsync(payload);
                            break;
                        case KafkaServiceName.MonthlyPayrollJob:
                            await CallPayrollJobAsync(payload);
                            break;
                        case KafkaServiceName.YearEndLeaveProcessingJob:
                            await CallLeaveYearEndJobAsync(payload);
                            break;
                        case KafkaServiceName.NewRegistration:
                            await CallGenerateAttendanceAsync(payload);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[DAIALY JOB ERROR]: Got error: {ex.Message}");
                }
            }
        }

        private MicroserviceRequest CreateRequestObject(string payload, DbConfigModal dbConfig)
        {
            MicroserviceRequest microserviceRequest = MicroserviceRequest.Builder(string.Empty);
            microserviceRequest.Database = dbConfig;
            microserviceRequest.Payload = payload;

            return microserviceRequest;
        }

        public async Task CallLeaveAccrualJobAsync(string payload)
        {
            string url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(MicroserviceRequest.Builder(url, payload, ));
        }

        public async Task CallTimesheetJobAsync(string payload)
        {
            string url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(MicroserviceRequest.Builder(url, payload));
        }

        public async Task CallPayrollJobAsync(string payload)
        {
            string url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(MicroserviceRequest.Builder(url, payload));
        }

        public async Task CallLeaveYearEndJobAsync(string payload)
        {
            string url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(MicroserviceRequest.Builder(url, payload));
        }

        public async Task CallGenerateAttendanceAsync(string payload)
        {
            string url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(MicroserviceRequest.Builder(url, payload));
        }

        public Task SendEmailNotification(dynamic attendanceRequestModal)
        {
            throw new NotImplementedException();
        }
    }
}
