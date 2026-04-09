using Microsoft.EntityFrameworkCore;
using OrderManagement.API.Data;
using OrderManagement.API.Mappings;
using OrderManagement.API.Messaging;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/order-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Order Management API", Version = "v1" }));

    // Database - SQLite for portability
    builder.Services.AddDbContext<OrderDbContext>(opts =>
        opts.UseSqlite(builder.Configuration.GetConnectionString("OrderDb") ?? "Data Source=orders.db"));

    // MediatR - CQRS
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // AutoMapper
    builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

    // RabbitMQ
    builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
    builder.Services.AddHostedService<OrderResultConsumer>();

    // CORS for frontend apps
    builder.Services.AddCors(opts => opts.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    var app = builder.Build();

    // Auto-migrate and seed database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        db.Database.EnsureCreated();
    }

    app.UseCors("AllowAll");
    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
        {
            diagCtx.Set("ServiceName", "OrderManagement.API");
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderManagement.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { } // for integration tests
