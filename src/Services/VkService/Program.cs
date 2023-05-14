using Microsoft.AspNetCore.Connections;
using MssDevLab.Common.Extensions;
using MssDevLab.Common.Models;
using MssDevLab.CommonCore.Interfaces.EventBus;
using MssDevLab.CommonCore.Services;
using MssDevLab.EventBusRabbitMQ;
using MssDevLab.VkService.Services;
using RabbitMQ.Client;

namespace MssDevLab.VkService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.ConfigureElasticSerilog("VkService");
            builder.Configuration.AddJsonFile("/run/secrets/app_secret");

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "VkService", Version = "v1" });
            });
            builder.Services.AddEventBus(builder.Configuration);

            builder.Services.AddTransient<ISearchService, SearchService>();
            builder.Services.AddTransient<INotificationService, NotificationService>();

            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseEventBus();

            app.MapControllers();

            app.Run();
        }

        private static void UseEventBus(this IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

            eventBus.Subscribe<SearchRequestedEvent, IIntegrationEventHandler<SearchRequestedEvent>>();
        }

        private static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
        {
            var retryCount = 5;
            if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
            {
                if (!int.TryParse(configuration["EventBusRetryCount"], out retryCount))
                {
                    retryCount = 5;
                }
            }

            services.AddSingleton<IEventBus, EventBusRabbitMQ.EventBusRabbitMQ>(sp =>
            {
                var subscriptionClientName = configuration["SubscriptionClientName"];
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ.EventBusRabbitMQ>>();
                var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                return new EventBusRabbitMQ.EventBusRabbitMQ(rabbitMQPersistentConnection, logger, sp, eventBusSubcriptionsManager, subscriptionClientName, retryCount);
            });

            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
            services.AddTransient<IIntegrationEventHandler<SearchRequestedEvent>, SearchRequestedEventHandler>();

            services.AddSingleton<IRabbitMQPersistentConnection>(sp => {
                var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();

                var factory = new ConnectionFactory()
                {
                    HostName = configuration["EventBusConnection"],
                    DispatchConsumersAsync = true
                };

                if (!string.IsNullOrEmpty(configuration["EventBusUserName"]))
                {
                    factory.UserName = configuration["EventBusUserName"];
                }

                if (!string.IsNullOrEmpty(configuration["EventBusPassword"]))
                {
                    factory.Password = configuration["EventBusPassword"];
                }

                return new DefaultRabbitMQPersistentConnection(factory, logger, retryCount);
            });

            return services;
        }
    }
}