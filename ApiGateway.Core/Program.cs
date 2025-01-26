using ApiGateway.Core;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder);
startup.ConfigureService(builder.Services);

startup.ConfigurePipeline(builder.Build());