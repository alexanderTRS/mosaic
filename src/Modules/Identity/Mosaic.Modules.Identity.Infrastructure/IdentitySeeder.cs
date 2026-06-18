using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Identity.Infrastructure.Persistence;
using Mosaic.Modules.Identity.Infrastructure.Security;

namespace Mosaic.Modules.Identity.Infrastructure;

public sealed class IdentitySeeder : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ILogger<IdentitySeeder> logger;

    public IdentitySeeder(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<IdentitySeeder> logger)
    {
        this.scopeFactory = scopeFactory;
        this.configuration = configuration;
        this.environment = environment;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordPolicy = scope.ServiceProvider.GetRequiredService<IPasswordPolicy>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        var adminUserName = configuration["Identity:DefaultAdmin:UserName"] ?? "admin";
        var adminPassword = configuration["Identity:DefaultAdmin:Password"] ?? "admin";

        if (!environment.IsDevelopment() && adminPassword == "admin")
        {
            throw new InvalidOperationException(
                "Identity:DefaultAdmin:Password must be configured outside Development.");
        }

        if (adminPassword != "admin")
        {
            passwordPolicy.EnsureValid(adminPassword);
        }

        if (await dbContext.Users.AnyAsync(user => user.UserName == adminUserName, cancellationToken))
        {
            return;
        }

        await dbContext.Users.AddAsync(
            new UserRecord
            {
                Id = Guid.NewGuid(),
                UserName = adminUserName,
                PasswordHash = passwordHasher.Hash(adminPassword),
                IsAdministrator = true,
                CanViewGraphQLSchema = true
            },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Default administrator user {AdminUserName} was created. Change the default password outside development.",
            adminUserName);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
