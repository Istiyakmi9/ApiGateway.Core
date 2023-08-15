using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ApiGateway.Core.MIddleware
{
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private IConfiguration _configuration;
        private readonly string TokenName = "Authorization";

        public JwtAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;

        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                var authorizationToken = string.Empty;
                Parallel.ForEach(context.Request.Headers, header =>
                {
                    if (header.Value.FirstOrDefault() != null)
                    {
                        if (header.Key == TokenName)
                            authorizationToken = header.Value.FirstOrDefault();
                    }
                });

                string userId = string.Empty;
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

                        // JwtSecurityToken securityToken = handler.ReadToken(token) as JwtSecurityToken;
                    }
                }

                await _next(context);
            }
            catch (Exception)
            {
                throw;
            }
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
