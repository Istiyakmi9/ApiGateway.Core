using ApiGateway.Core.Modal;
using ApiGateway.Core.Service;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ApiGateway.Core.MIddleware
{
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private IConfiguration _configuration;
        private readonly string TokenName = "Authorization";
        private readonly MasterConnection _masterConnection;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;

        public JwtAuthenticationMiddleware(RequestDelegate next,
            IConfiguration configuration,
            MasterConnection masterConnection,
            ILogger<JwtAuthenticationMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _masterConnection = masterConnection;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                var authorizationToken = string.Empty;
                var companyCode = string.Empty;

                Parallel.ForEach(context.Request.Headers, header =>
                {
                    _logger.LogInformation($"Reading: {TokenName}");
                    if (header.Value.FirstOrDefault() != null)
                    {
                        if (header.Key == TokenName)
                        {                            
                            _logger.LogInformation($"{TokenName}: {header.Value.FirstOrDefault()}");
                            authorizationToken = header.Value.FirstOrDefault();
                        }
                        if (header.Key == "companyCode")
                        {
                            _logger.LogInformation($"companyCode: {header.Value.FirstOrDefault()}");
                            companyCode = header.Value.FirstOrDefault();
                        }
                    }
                });

                string urlPath = context.Request.Path.Value!;

                string userId = string.Empty;
                string sid = string.Empty;
                string user = string.Empty;
                if (!string.IsNullOrEmpty(authorizationToken))
                {
                    string token = authorizationToken.Replace("Bearer", "").Trim();
                    if (!string.IsNullOrEmpty(token) && token != "null")
                    {
                        var handler = new JwtSecurityTokenHandler();
                        handler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = false,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = _configuration["jwtSetting:Issuer"],
                            ValidAudience = _configuration["jwtSetting:Issuer"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["jwtSetting:Key"]))
                        }, out SecurityToken validatedToken);

                        JwtSecurityToken securityToken = handler.ReadToken(token) as JwtSecurityToken;
                        ConfigDatabase(context, securityToken);
                    }
                    else if (!string.IsNullOrEmpty(companyCode))
                    {
                        ConfigDatabase(context, companyCode);
                    }
                }
                else if (!string.IsNullOrEmpty(companyCode))
                {
                    ConfigDatabase(context, companyCode);
                }

                await _next(context);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ConfigDatabase(HttpContext context, string companyCode)
        {
            var codes = companyCode.Split("-");
            if (codes.Length != 2)
            {
                throw new Exception("Invalid company code found.");
            }

            DatabaseConfiguration? databaseConfiguration = _masterConnection.GetDatabaseBasedOnCode(codes[0], codes[1]);

            context.Request.Headers.Add("database", JsonConvert.SerializeObject(databaseConfiguration));
            context.Request.Headers.Add("JBot", "[]");
        }

        private void ConfigDatabase(HttpContext context, JwtSecurityToken securityToken)
        {
            var user = securityToken.Claims.FirstOrDefault(x => x.Type == "JBot").Value;
            var companyCode = securityToken.Claims.FirstOrDefault(x => x.Type == "CompanyCode").Value;
            var sid = securityToken.Claims.FirstOrDefault(x => x.Type == "JBot").Value;

            var codes = companyCode.Split("-");
            if (codes.Length != 2)
            {
                throw new Exception("Invalid company code found.");
            }

            DatabaseConfiguration? databaseConfiguration = _masterConnection.GetDatabaseBasedOnCode(codes[0], codes[1]);

            context.Request.Headers.Add("userDetail", user);
            context.Request.Headers.Add("sid", sid);
            context.Request.Headers.Add("database", JsonConvert.SerializeObject(databaseConfiguration));
            context.Request.Headers.Add("companyCode", companyCode);
        }
    }

    public static class JwtAuthenticationMiddlewareExtension
    {
        public static IApplicationBuilder UseJwtAuthenticationMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<JwtAuthenticationMiddleware>();
        }
    }
}
