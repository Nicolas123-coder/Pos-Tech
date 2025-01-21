using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Infrastructure.Data
{
    public static class DatabaseExtensions
    {
        public static void MigrateDatabase(this IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

                    logger.LogInformation("Iniciando migração do banco de dados...");

                    var retry = 3;
                    while (retry > 0)
                    {
                        try
                        {
                            context.Database.Migrate();
                            logger.LogInformation("Migração concluída com sucesso!");
                            break;
                        }
                        catch (Exception ex)
                        {
                            retry--;
                            if (retry == 0)
                                throw;

                            logger.LogWarning($"Erro na migração: {ex.Message}. Tentativas restantes: {retry}");
                            System.Threading.Thread.Sleep(10000); // 10 segundos
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
                    logger.LogError(ex, "Ocorreu um erro durante a migração do banco de dados.");
                    throw;
                }
            }
        }
    }
}