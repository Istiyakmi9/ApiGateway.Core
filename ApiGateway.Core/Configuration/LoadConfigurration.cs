using ApiGateway.Core.Modal;
using Microsoft.Extensions.Options;

namespace ApiGateway.Core.Configuration
{
    public class LoadConfigurration
    {
        public static void LoadServiceConfigure(IServiceCollection services, ConfigurationManager _configuration)
        {
            services.Configure<MicroserviceRegistry>(x => _configuration.GetSection(nameof(MicroserviceRegistry)).Bind(x));
            services.AddSingleton(resolver =>
                resolver.GetRequiredService<IOptions<MicroserviceRegistry>>().Value
            );
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
