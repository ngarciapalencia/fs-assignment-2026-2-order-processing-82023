using Microsoft.EntityFrameworkCore;
using OrderManagement.API.Data;
using OrderManagement.API.Mappings;
using OrderManagement.API.Messaging;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog only when not in test
var isTest = builder.Environment.EnvironmentName == "Test" ||
             builder.Environment.EnvironmentName == "Development" &&
             AppDomain.CurrentDomain.GetAssemblies()
                 .Any(a => a.FullName?.Contains("xunit") == true);

if (!isTest)
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/order-api-.log", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Host.UseSerilog();
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Order Management API", Version = "v1" }));

builder.Services.AddDbContext<OrderDbContext>(opts =>
    opts.UseSqlite(
        builder.Configuration.GetConnectionString("OrderDb") ?? "Data Source=orders.db"));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<OrderResultConsumer>();

builder.Services.AddCors(opts => opts.AddPolicy("AllowAll", p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("AllowAll");

if (!isTest)
    app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

public partial class Program { }