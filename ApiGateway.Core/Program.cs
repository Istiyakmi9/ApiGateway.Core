using ApiGateway.Core.HostedService;
using ApiGateway.Core.MIddleware;
using ApiGateway.Core.Modal;
using ApiGateway.Core.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();
builder.Services.AddOcelot(builder.Configuration)
    .AddKubernetes();


builder.Services.Configure<MasterDatabase>(x => builder.Configuration.GetSection(nameof(MasterDatabase)).Bind(x));
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

builder.Services.AddHostedService<DailyJob>();
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRouting();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
                   Path.Combine(Directory.GetCurrentDirectory())),
    RequestPath = "/Files"
});

app.UseJwtAuthenticationMiddleware();
app.UseAuthentication();
app.UseAuthorization();


app.UseEndpoints(endpoints => endpoints.MapControllers());
app.UseOcelot().Wait();


app.MapControllers();

app.Run();
