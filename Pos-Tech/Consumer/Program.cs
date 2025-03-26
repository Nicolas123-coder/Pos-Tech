using Application.Services;
using Application.Validators;
using Domain.Interfaces;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Consumer;
using Prometheus;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddValidatorsFromAssemblyContaining<ContactValidator>();
builder.Services.AddRabbitMQServices();

builder.Services.AddSingleton<Counter>(
    Metrics.CreateCounter("consumer_messages_processed_total", "Total number of messages processed"));
builder.Services.AddSingleton<Gauge>(
    Metrics.CreateGauge("consumer_processing_queue_size", "Current size of processing queue"));

builder.Services.AddHostedService<WebMetricsServer>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();