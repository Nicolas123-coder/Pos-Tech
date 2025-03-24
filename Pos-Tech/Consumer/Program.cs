using Infrastructure.Data;
using Infrastructure.Repositories;
using Application.Services;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Consumer;
using Application.Validators;
using FluentValidation.AspNetCore;
using FluentValidation;

var builder = Host.CreateApplicationBuilder(args);

// Conexão ao banco de dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositório e serviço de contato
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<ContactService>();

builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<ContactValidator>();

// Registro do Worker consumidor
builder.Services.AddHostedService<Worker>();

var app = builder.Build();
app.Run();
