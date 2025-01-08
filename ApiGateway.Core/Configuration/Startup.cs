using ApiGateway.Core.Interface;
using ApiGateway.Core.MIddleware;
using ApiGateway.Core.Modal;
using ApiGateway.Core.Service;
using ApiGateway.Core.Services;
using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Enums;
using Bt.Lib.Common.Service.KafkaService.interfaces;
using Bt.Lib.Common.Service.MicroserviceHttpRequest;
using Bt.Lib.Common.Service.Middlewares;
using Bt.Lib.Common.Service.Model;
using Bt.Lib.Common.Service.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;

namespace ApiGateway.Core.Configuration
{
    public class Startup(WebApplicationBuilder _builder)
    {
        public void ConfigureService(IServiceCollection services)
        {
            ConfigurationManager _configuration = _builder.Configuration;
            IWebHostEnvironment _environment = _builder.Environment;

            // configure appsettings file
            ConfiguraAppSettingFiles(_configuration, _environment);

            // load configuration from appsettings.{env}.json file and mapped into classes
            LoadConfigurration.LoadServiceConfigure(services, _configuration);

            services.AddControllers();

            // register service and classes
            RegisterServices(services, _environment);

            services.AddOcelot(_configuration).AddKubernetes();

            // enable local ip address for debugging in dev environment only.
            LoadConfigurration.EnableLocalDebugging(_builder);


            var commonRegistry = new CommonRegistry(services, _environment, _configuration);

            commonRegistry
            .AddCORS("EmstumGatewayPolicy")
            .AddKafkaConsumerService()
            .AddKafkaProducerService()
            .AddPublicKeyConfiguration()
            .AddJWTSupport()
            .AddCurrentSessionClass();
        }

        private void RegisterServices(IServiceCollection services, IWebHostEnvironment _environment)
        {
            var serviceProvider = services.BuildServiceProvider();
            var microserviceRegistry = serviceProvider.GetRequiredService<MicroserviceRegistry>();

            services.AddSingleton<MasterConnection>(x =>
                new MasterConnection(
                    microserviceRegistry.DatabaseConfigurationUrl
                )
            );
            services.AddScoped((IServiceProvider x) => new CurrentSession
            {
                Environment = ((!(_environment.EnvironmentName == "Development")) ? DefinedEnvironments.Production : DefinedEnvironments.Development)
            });
            services.AddScoped<RequestMicroservice>();
            services.AddSingleton<ApplicationConfiguration>();
            services.AddScoped<IKafkaServiceHandler, KafkaServiceHandler>();
        }

        private void ConfiguraAppSettingFiles(ConfigurationManager _configuration, IWebHostEnvironment _environment)
        {
            _configuration.SetBasePath(_environment.ContentRootPath)
                            .AddJsonFile($"appsettings.{_environment.EnvironmentName}.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();
        }

        public void ConfigurePipeline(WebApplication app)
        {
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var kafkaServiceHandler = scope.ServiceProvider.GetRequiredService<IKafkaServiceHandler>();
                    var kafkaConsumerService = scope.ServiceProvider.GetRequiredService<IKafkaConsumerService>();

                    kafkaConsumerService.SubscribeTopic(kafkaServiceHandler.DailyJobManager, nameof(KafkaTopicNames.DAILY_JOBS_MANAGER));
                }
            });

            // Configure the HTTP request pipeline.
            app.UseCors("CorsPolicy");

            app.UseRouting();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                               Path.Combine(Directory.GetCurrentDirectory())),
                RequestPath = "/bts/resources"
            });

            app.UseMiddleware<ExceptionHandlerMiddleware>();
            app.UseJwtAuthenticationMiddleware();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.UseOcelot().Wait();

            app.MapControllers();

            app.Run();
        }
    }
}
