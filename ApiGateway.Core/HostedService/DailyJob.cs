using ApiGateway.Core.Modal;
using ApiGateway.Core.Service;
using NCrontab;
using System.Threading;

namespace ApiGateway.Core.HostedService
{
    public class DailyJob : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DailyJob> _logger;
        private readonly CrontabSchedule _cron;
        private readonly IServiceProvider _serviceProvider;

        DateTime _nextCron;
        public DailyJob(ILogger<DailyJob> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;

            _logger.LogInformation($"Cron value: {configuration.GetSection("DailyEarlyHourJob").Value}");

            _cron = CrontabSchedule.Parse(configuration.GetSection("DailyEarlyHourJob").Value,
                new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            _nextCron = _cron.GetNextOccurrence(DateTime.UtcNow);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Enabling the logging.");

            // _ = Task.Run(() => StartJobAsync(cancellationToken));
            await Task.CompletedTask;
        }

        public async Task StartJobAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int value = WaitForNextCronValue();
                _logger.LogInformation($"Cron job will run: {_nextCron}. Utc time: {DateTime.UtcNow} Wait time in ms: {value}");

                await Task.Delay(value, cancellationToken);
                _logger.LogInformation($"Daily cron job started at {DateTime.Now} (utc time: {DateTime.UtcNow})   ...............");

                await this.RunJobAsync();

                _logger.LogInformation($"Daily cron job ran successfully at {DateTime.Now} (utc time: {DateTime.UtcNow})   .................");
                _nextCron = _cron.GetNextOccurrence(DateTime.Now);
            }
        }

        private async Task RunJobAsync()
        {
            _logger.LogInformation("Leave Accrual cron job started.");
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                MasterConnection masterConnection = scope.ServiceProvider.GetRequiredService<MasterConnection>();
                List<DatabaseConfiguration> connections = masterConnection.GetAllConnections();

                foreach (DatabaseConfiguration connection in connections)
                {

                }
            }

            _logger.LogInformation("Leave Accrual cron job ran successfully.");
            await Task.CompletedTask;
        }

        private int WaitForNextCronValue() => Math.Max(0, (int)_nextCron.Subtract(DateTime.UtcNow).TotalMilliseconds);

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
