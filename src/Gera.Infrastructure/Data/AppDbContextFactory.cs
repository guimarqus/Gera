using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Gera.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var configurationBuilder = new ConfigurationBuilder();
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")))
            configurationBuilder.AddUserSecrets(Assembly.Load("Gera.Api"), optional: true);
        var configuration = configurationBuilder.AddEnvironmentVariables().Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection via User Secrets before running EF migrations.");

        optionsBuilder.UseSqlServer(connectionString, x => x.UseNetTopologySuite());

        return new AppDbContext(optionsBuilder.Options);
    }
}

