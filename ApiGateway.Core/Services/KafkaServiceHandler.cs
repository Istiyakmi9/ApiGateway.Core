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

namespace ApiGateway.Core.Services
{
    public class KafkaServiceHandler
    {
        private readonly ILogger<KafkaServiceHandler> _logger;
        private readonly MasterDatabase _masterDatabase;
        private readonly List<KafkaServiceConfig> _kafkaServiceConfig;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly RequestMicroservice _requestMicroservice;
        private readonly MicroserviceRegistry _microserviceRegistry;
        private readonly MasterConnection _masterConnection;

        public KafkaServiceHandler(
            ILogger<KafkaServiceHandler> logger,
            IOptions<MasterDatabase> options,
            IOptions<List<KafkaServiceConfig>> kafkaOptions,
            ITimezoneConverter timezoneConverter,
            IOptions<MicroserviceRegistry> microserviceOptions,
            RequestMicroservice requestMicroservice,
            MasterConnection masterConnection)
        {
            _logger = logger;
            _masterDatabase = options.Value;
            _kafkaServiceConfig = kafkaOptions.Value;
            _timezoneConverter = timezoneConverter;
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
            List<DatabaseConfiguration> dbConfig = new List<DatabaseConfiguration>(); ;

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

        public async Task CallLeaveAccrualJobAsync(string payload)
        {
            string url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<string>(MicroserviceRequest.Builder(url, payload));
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
    }
}
