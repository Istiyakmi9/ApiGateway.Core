using ApiGateway.Core.Modal;
using ModalLayer;

namespace ApiGateway.Core.Configuration
{
    public class LoadConfigurration
    {
        public static void LoadServiceConfigure(IServiceCollection services, ConfigurationManager _configuration)
        {
            services.Configure<MasterDatabase>(x => _configuration.GetSection(nameof(MasterDatabase)).Bind(x));
            services.Configure<MicroserviceRegistry>(x => _configuration.GetSection(nameof(MicroserviceRegistry)).Bind(x));
            services.Configure<MicroserviceRegistry>(x => _configuration.GetSection(nameof(MicroserviceRegistry)).Bind(x));
            services.Configure(delegate (List<KafkaServiceConfig> x)
            {
                _configuration.GetSection("KafkaServiceConfig").Bind(x);
            });
        }

        public static void EnableLocalDebugging(WebApplicationBuilder builder)
        {
            if (builder.Environment.IsDevelopment())
            {
                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ListenAnyIP(5000);
                    serverOptions.Listen(System.Net.IPAddress.Parse("192.168.0.117"), 5000);
                });
            }
        }
    }
}
