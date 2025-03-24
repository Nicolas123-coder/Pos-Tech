using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Application.Services;
using Application.DTOs;
using Domain.Interfaces;
using MassTransit;
using Consumidor;

var builder = Host.CreateApplicationBuilder(args);

// Configuração do SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<ContactService>();

// Configuração do MassTransit e registro do consumer
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EnvelopeConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("envelope-queue", e =>
        {
            e.ConfigureConsumer<EnvelopeConsumer>(context);
        });
    });
});

// Adiciona o hosted service do MassTransit
builder.Services.AddMassTransitHostedService();

// Se desejar manter um Worker para outras tarefas, pode ser algo assim:
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
