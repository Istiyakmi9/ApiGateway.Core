using ApiGateway.Core.HostedService;
using ApiGateway.Core.MIddleware;
using ApiGateway.Core.Modal;
using ApiGateway.Core.Service;
using ApiGateway.Core.Services;
using Bot.CoreBottomHalf.CommonModal.Enums;
using Bot.CoreBottomHalf.CommonModal;
using Confluent.Kafka;
using ems_CommonUtility.KafkaService.code;
using ems_CommonUtility.KafkaService.interfaces;
using ems_CommonUtility.MicroserviceHttpRequest;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModalLayer;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;
using System.Configuration;
using System.Text;
using ApiGateway.Core.Interface;
using ApiGateway.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder);
startup.ConfigureService(builder.Services);

startup.ConfigurePipeline(builder.Build());