using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.API.Data;
using OrderManagement.API.Messaging;
using Moq;

namespace Integration.Tests;

public class OrderApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real DB with in-memory SQLite
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            services.AddDbContext<OrderDbContext>(opts =>
                opts.UseSqlite("Data Source=:memory:"));

            // Replace RabbitMQ publisher with mock
            var rabbitDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRabbitMqPublisher));
            if (rabbitDescriptor != null) services.Remove(rabbitDescriptor);
            var mockPublisher = new Mock<IRabbitMqPublisher>();
            services.AddSingleton(mockPublisher.Object);

            // Remove background consumers (need real RabbitMQ)
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                .ToList();
            foreach (var svc in hostedServices) services.Remove(svc);

            // Ensure DB created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
