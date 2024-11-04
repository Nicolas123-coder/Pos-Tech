using FluentValidation;
using FluentValidation.AspNetCore;
using Application.Validators;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Domain.Interfaces;
using Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuração do banco de dados
// Substitua "Data Source=contacts.db" pela sua conexão de banco de dados, se estiver usando um banco diferente
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Registro do repositório para injeção de dependência
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<ContactService>();

// Configuração de controllers e FluentValidation
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();

// Registro dos validadores
builder.Services.AddValidatorsFromAssemblyContaining<ContactValidator>();

// Configuração do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configuração do pipeline de requisição HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
