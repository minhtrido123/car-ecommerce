using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sorcha.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddServiceDefaults()
        {
            services.AddEndpointsApiExplorer();
            return services;
        }

        public IServiceCollection AddJwtAuthentication(string issuer, string audience, string secretKey)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = issuer,
                                ValidAudience = audience,
                                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                                    System.Text.Encoding.UTF8.GetBytes(secretKey))
                            };
                        });
            return services;
        }
    }

    extension(WebApplication app)
    {
        public IApplicationBuilder MapDefaultEndpoints()
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }
    }

}
