using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderManagement.API.Data;
using OrderManagement.API.Messaging;
using Moq;

namespace Integration.Tests;

public class OrderApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public OrderApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Esto hace que isTest = true en Program.cs
        builder.UseEnvironment("Test");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureServices(services =>
        {
            // Quitar DB real
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            // SQLite in-memory con conexión persistente
            services.AddDbContext<OrderDbContext>(opts =>
                opts.UseSqlite(_connection));

            // Mock RabbitMQ
            var rabbitDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IRabbitMqPublisher));
            if (rabbitDescriptor != null) services.Remove(rabbitDescriptor);

            var mockPublisher = new Mock<IRabbitMqPublisher>();
            services.AddSingleton(mockPublisher.Object);

            // Quitar background services
            var hostedServices = services
                .Where(d => d.ServiceType ==
                    typeof(Microsoft.Extensions.Hosting.IHostedService))
                .ToList();
            foreach (var svc in hostedServices)
                services.Remove(svc);

            // Crear schema
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}