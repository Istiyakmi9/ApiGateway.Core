using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();
builder.Services.AddOcelot(builder.Configuration)
    .AddKubernetes();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRouting();
// app.UseAuthorization();

app.UseEndpoints(endpoints => endpoints.MapControllers());
app.UseOcelot().Wait();


app.MapControllers();

app.Run();
