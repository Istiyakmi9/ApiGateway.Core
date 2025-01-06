using ApiGateway.Core.HostedService;
using ApiGateway.Core.Interface;
using ApiGateway.Core.MIddleware;
using ApiGateway.Core.Service;
using ApiGateway.Core.Services;
using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Enums;
using Bt.Lib.Common.Service.Configserver;
using Bt.Lib.Common.Service.KafkaService.code.Producer;
using Bt.Lib.Common.Service.KafkaService.interfaces;
using Bt.Lib.Common.Service.MicroserviceHttpRequest;
using Bt.Lib.Common.Service.Model;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;
using System.Text;

namespace ApiGateway.Core.Configuration
{
    public class Startup(WebApplicationBuilder _builder)
    {
        public void ConfigureService(IServiceCollection services)
        {
            ConfigurationManager _configuration = _builder.Configuration;
            IWebHostEnvironment _environment = _builder.Environment;

            services.AddControllers();

            ConfiguraCORS(services);

            // configure appsettings file
            ConfiguraAppSettingFiles(_configuration, _environment);

            services.AddOcelot(_configuration).AddKubernetes();

            // load configuration from appsettings.{env}.json file and mapped into classes
            LoadConfigurration.LoadServiceConfigure(services, _configuration);

            // enable local ip address for debugging in dev environment only.
            LoadConfigurration.EnableLocalDebugging(_builder);

            // register service and classes
            RegisterServices(services, _environment);

            //Kafka Service
            RegisterKafkaService(services, _environment);

            // Register jwt token manager
            // RegisterJWTTokenService(services, _configuration);

            services.AddHostedService<DailyJob>();

        }

        private void ConfiguraCORS(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Content-Disposition") // if you want to expose specific headers
                        .SetPreflightMaxAge(TimeSpan.FromMinutes(10))); // Cache the preflight response
            });
        }

        private void RegisterServices(IServiceCollection services, IWebHostEnvironment _environment)
        {
            services.AddSingleton<MasterConnection>();
            services.AddScoped((IServiceProvider x) => new CurrentSession
            {
                Environment = ((!(_environment.EnvironmentName == "Development")) ? DefinedEnvironments.Production : DefinedEnvironments.Development)
            });
            services.AddScoped<RequestMicroservice>();
            services.AddScoped<IKafkaServiceHandler, KafkaServiceHandler>();
        }

        private void RegisterKafkaService(IServiceCollection services, IWebHostEnvironment _environment)
        {

            services.AddSingleton<IKafkaProducerService>(x =>
                KafkaProducerService.GetInstance(
                    ApplicationNames.EMSTUM,
                    x.GetRequiredService<ProducerConfig>(),
                    _builder.Environment
                )
            );

            services.AddSingleton<IFetchGithubConfigurationService>(x =>
            {
                var githubConfigService = FetchGithubConfigurationService.getInstance().Init(ApplicationNames.EMSTUM, _environment);
                var publicKeys = githubConfigService.GetConfiguration<PublicKeyDetail>();
                services.Configure<PublicKeyDetail>(x =>
                {
                    new PublicKeyDetail
                    {
                        CompanyCode = publicKeys.CompanyCode,
                        Key = publicKeys.Key,
                        DefaulExpiryTimeInSeconds = publicKeys.DefaulExpiryTimeInSeconds,
                        DefaultRefreshTokenExpiryTimeInSeconds = publicKeys.DefaultRefreshTokenExpiryTimeInSeconds,
                        Issuer = publicKeys.Issuer
                    };
                });
                RegisterJWTTokenService(services);
                return githubConfigService;
            });
        }

        private void ConfiguraAppSettingFiles(ConfigurationManager _configuration, IWebHostEnvironment _environment)
        {
            _configuration.SetBasePath(_environment.ContentRootPath)
                            .AddJsonFile($"appsettings.{_environment.EnvironmentName}.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();
        }

        private void RegisterJWTTokenService(IServiceCollection services)
        {
            PublicKeyDetail publicKeyDetail = services.BuildServiceProvider().GetRequiredService<PublicKeyDetail>();

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                        .AddJwtBearer(x =>
                        {
                            x.SaveToken = true;
                            x.RequireHttpsMetadata = false;
                            x.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(publicKeyDetail.Key)),
                                ClockSkew = TimeSpan.Zero
                            };
                        });
        }

        public void ConfigurePipeline(WebApplication app)
        {
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var kafkaServiceHandler = scope.ServiceProvider.GetRequiredService<IKafkaServiceHandler>();
                    // Call your method here, for example:
                    kafkaServiceHandler.ScheduledJobManager();
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
