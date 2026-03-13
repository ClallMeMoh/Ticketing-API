using System.Security.Claims;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Repositories;
using Ticketing.Infrastructure.Persistence;
using Ticketing.Infrastructure.Repositories;
using Ticketing.Infrastructure.Services;

namespace Ticketing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddMessaging(configuration);
        services.AddApiInfrastructure(configuration);
        return services;
    }

    public static IServiceCollection AddWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        return services;
    }

    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null,
        bool includeAssignmentEndpoint = false)
    {
        var disableMassTransit = configuration.GetValue<bool>("Messaging:DisableMassTransit");
        if (disableMassTransit)
        {
            services.AddScoped<IMessagePublisher, NoOpMessagePublisher>();
            return services;
        }

        services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

        services.AddMassTransit(x =>
        {
            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMq:Host"] ?? "localhost";
                var virtualHost = configuration["RabbitMq:VirtualHost"] ?? "/";
                var username = configuration["RabbitMq:Username"] ?? "guest";
                var password = configuration["RabbitMq:Password"] ?? "guest";

                cfg.Host(host, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                if (includeAssignmentEndpoint)
                {
                    cfg.ReceiveEndpoint("ticket-assignment", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry =>
                            retry.Exponential(
                                retryLimit: 5,
                                minInterval: TimeSpan.FromSeconds(1),
                                maxInterval: TimeSpan.FromSeconds(30),
                                intervalDelta: TimeSpan.FromSeconds(2)));

                        endpoint.ConfigureConsumeTopology = false;
                        endpoint.Bind("ticketing.events", bind =>
                        {
                            bind.ExchangeType = ExchangeType.Topic;
                            bind.RoutingKey = "ticket.created";
                        });
                    });
                }

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IAgentProfileRepository, AgentProfileRepository>();
        services.AddScoped<IAssignmentHistoryRepository, AssignmentHistoryRepository>();
        services.AddScoped<ITicketReadService, TicketReadService>();
        services.AddScoped<ICommentReadService, CommentReadService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    private static IServiceCollection AddApiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        var jwtSettings = configuration.GetSection("Jwt");
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
                RoleClaimType = ClaimTypes.Role
            };
        });

        return services;
    }
}
