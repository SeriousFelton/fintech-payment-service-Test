using FintechTask.Application.Interfaces;
using FintechTask.Infrastructure.Data;
using FintechTask.Infrastructure.ProviderClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FintechTask.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
            return services;
        }

        public static IServiceCollection AddProviderClient(this IServiceCollection services, IConfiguration configuration)
        {
            var providerUrl = configuration["PROVIDER_URL"] ?? "http://provider-simulator:8081";

            services.AddHttpClient<IProviderClient, ProviderClient>(client =>
            {
                client.BaseAddress = new Uri(providerUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
                .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
                .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

            return services;
        }

        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            //временная заглушка, где будут фоновые сервисы
            return services;
        }

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "FintechTask API",
                    Version = "v1",
                    Description = "Сервис обработки платежей с политикой идемпотентности и повторных попыток"
                });
            });

            return services;
        }
    }
}
