using API.Configuration;
using Application.Services;
using Application.Validators;
using Domain.Interfaces;
using FluentValidation.AspNetCore;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Prometheus;
using Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddRabbitMQServices();

builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<ContactValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Contacts API",
        Version = "v1",
        Description = "API para gerenciamento de contatos regionais"
    });
});

builder.Services.AddMetrics();
builder.Services.AddCustomMetrics();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("APPLY_MIGRATIONS", false))
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            bool tableExists = false;
            try
            {
                var _ = dbContext.Contacts.FirstOrDefault();
                tableExists = true;
            }
            catch { }

            if (!tableExists)
            {
                Console.WriteLine("Applying pending migrations...");
                dbContext.Database.Migrate();
            }
            else
            {
                Console.WriteLine("Table 'Contacts' already exists. Skipping migrations.");

                var conn = dbContext.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250323233129_InitialCreate') " +
                                      "INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20250323233129_InitialCreate', '9.0.3')";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during migration: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Contacts API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseMetricServer();
app.UseHttpMetrics();
app.UseRouting();
app.UseCustomMetrics();
app.UseAuthorization();
app.MapControllers();

app.Run();

namespace API
{
    public partial class Program { }
}