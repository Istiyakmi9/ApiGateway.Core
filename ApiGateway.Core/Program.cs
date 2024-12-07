using ApiGateway.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder);
startup.ConfigureService(builder.Services);

startup.ConfigurePipeline(builder.Build());


/*
// Add services to the container.
builder.Services.AddControllers();

Console.WriteLine($"appsettings.{builder.Environment.EnvironmentName}.json");

builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();
builder.Services.AddOcelot(builder.Configuration)
    .AddKubernetes();


builder.Services.Configure<MasterDatabase>(x => builder.Configuration.GetSection(nameof(MasterDatabase)).Bind(x));
builder.Services.Configure<MicroserviceRegistry>(x => builder.Configuration.GetSection(nameof(MicroserviceRegistry)).Bind(x));

builder.Services.AddSingleton<MasterConnection>();
builder.Services.AddAuthentication(x =>
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSetting:Key"])),
        ClockSkew = TimeSpan.Zero
    };
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000);
    serverOptions.Listen(System.Net.IPAddress.Parse("192.168.1.100"), 5000);
});

builder.Services.AddScoped((IServiceProvider x) => new CurrentSession
{
    Environment = ((!(builder.Environment.EnvironmentName == "Development")) ? DefinedEnvironments.Production : DefinedEnvironments.Development)
});
builder.Services.Configure<MicroserviceRegistry>(x => builder.Configuration.GetSection(nameof(MicroserviceRegistry)).Bind(x));
builder.Services.AddScoped<RequestMicroservice>();
builder.Services.AddScoped<IKafkaServiceHandler, KafkaServiceHandler>();

//Kafka Service
builder.Services.Configure(delegate (List<KafkaServiceConfig> x)
{
    builder.Configuration.GetSection("KafkaServiceConfig").Bind(x);
});
ProducerConfig producerConfig = new ProducerConfig();
builder.Configuration.Bind("KafkaServerDetail", producerConfig);
builder.Services.AddSingleton(producerConfig);
builder.Services.AddSingleton((Func<IServiceProvider, IKafkaNotificationService>)
    ((IServiceProvider x) =>
    new KafkaNotificationService(
        x.GetRequiredService<IOptions<List<KafkaServiceConfig>>>(),
        x.GetRequiredService<ProducerConfig>(),
        x.GetRequiredService<ILogger<KafkaNotificationService>>(),
        (!(builder.Environment.EnvironmentName == "Development")) ?
        DefinedEnvironments.Production : DefinedEnvironments.Development)
    ));

builder.Services.AddHostedService<DailyJob>();
var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseRouting();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
                   Path.Combine(Directory.GetCurrentDirectory())),
    RequestPath = "/bt/resources"
});

app.UseJwtAuthenticationMiddleware();
app.UseAuthentication();
app.UseAuthorization();


app.UseOcelot().Wait();

app.MapControllers();

app.Run();
*/