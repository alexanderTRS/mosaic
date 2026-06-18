using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mosaic.Modules.Content.Infrastructure.Persistence;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Identity.Infrastructure.Persistence;
using Mosaic.Modules.Identity.Infrastructure.Security;
using Mosaic.Modules.Media.Infrastructure.Persistence;
using Mosaic.Modules.Search.Infrastructure.Persistence;
using Npgsql;

namespace Mosaic.Api.IntegrationTests;

public sealed class MosaicApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string databaseName = $"mosaic_integration_{Guid.NewGuid():N}";

    public string AdminPassword => "Admin1234";

    public string DynamicContentTypeApiName => "dynamicProduct";

    public HttpClient HttpClient { get; private set; } = null!;

    private string ConnectionString =>
        $"Host=localhost;Port=5432;Database={databaseName};Username=mosaic;Password=mosaic";

    private static string AdminConnectionString =>
        "Host=localhost;Port=5432;Database=postgres;Username=mosaic;Password=mosaic";

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Mosaic", ConnectionString);

        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""CREATE DATABASE "{databaseName}";""";
            await command.ExecuteNonQueryAsync();
        }

        await using var contentDbContext = CreateContentDbContext();
        await contentDbContext.Database.MigrateAsync();

        var missingContentTables = await contentDbContext.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'content'
                    AND table_name IN ('content_types', 'content_fields', 'content_items', 'content_item_versions')
                """)
            .SingleAsync();
        if (missingContentTables != 4)
        {
            throw new InvalidOperationException("Content database schema was not fully migrated.");
        }

        await SeedDynamicContentType(contentDbContext);

        await using var identityDbContext = CreateIdentityDbContext();
        await identityDbContext.Database.MigrateAsync();

        var passwordHasher = new Pbkdf2PasswordHasher();
        var admin = identityDbContext.Users.SingleOrDefault(user => user.UserName == "admin");
        if (admin is null)
        {
            await identityDbContext.Users.AddAsync(
                new UserRecord
                {
                    Id = Guid.NewGuid(),
                    UserName = "admin",
                    PasswordHash = passwordHasher.Hash(AdminPassword),
                    IsAdministrator = true,
                    CanViewGraphQLSchema = true
                });
        }
        else
        {
            admin.PasswordHash = passwordHasher.Hash(AdminPassword);
            admin.IsAdministrator = true;
            admin.CanViewGraphQLSchema = true;
        }

        await identityDbContext.SaveChangesAsync();

        await using var mediaDbContext = CreateMediaDbContext();
        await mediaDbContext.Database.MigrateAsync();

        await using var searchDbContext = CreateSearchDbContext();
        await searchDbContext.Database.MigrateAsync();

        HttpClient = CreateClient();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        HttpClient.Dispose();
        Environment.SetEnvironmentVariable("ConnectionStrings__Mosaic", null);

        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        await using (var terminateCommand = connection.CreateCommand())
        {
            terminateCommand.CommandText = """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName;
                """;
            terminateCommand.Parameters.AddWithValue("databaseName", databaseName);
            await terminateCommand.ExecuteNonQueryAsync();
        }

        await using (var dropCommand = connection.CreateCommand())
        {
            dropCommand.CommandText = $"""DROP DATABASE IF EXISTS "{databaseName}";""";
            await dropCommand.ExecuteNonQueryAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Mosaic"] = ConnectionString,
                    ["Identity:DefaultAdmin:UserName"] = "admin",
                    ["Identity:DefaultAdmin:Password"] = AdminPassword,
                    ["Identity:Security:AccessTokenLifetimeMinutes"] = "15",
                    ["Media:Storage:Local:RootPath"] = Path.Combine(Path.GetTempPath(), $"mosaic-media-{Guid.NewGuid():N}"),
                    ["Serilog:MinimumLevel:Default"] = "Warning",
                    ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning",
                    ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command"] = "Warning"
                });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ContentDbContext>>();
            services.RemoveAll<ContentDbContext>();
            services.AddDbContext<ContentDbContext>(options => options.UseNpgsql(ConnectionString));

            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(ConnectionString));

            services.RemoveAll<DbContextOptions<MediaDbContext>>();
            services.RemoveAll<MediaDbContext>();
            services.AddDbContext<MediaDbContext>(options => options.UseNpgsql(ConnectionString));

            services.RemoveAll<DbContextOptions<SearchDbContext>>();
            services.RemoveAll<SearchDbContext>();
            services.AddDbContext<SearchDbContext>(options => options.UseNpgsql(ConnectionString));
        });
    }

    private ContentDbContext CreateContentDbContext()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ContentDbContext(options);
    }

    private IdentityDbContext CreateIdentityDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new IdentityDbContext(options);
    }

    private MediaDbContext CreateMediaDbContext()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new MediaDbContext(options);
    }

    private SearchDbContext CreateSearchDbContext()
    {
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new SearchDbContext(options);
    }

    private async Task SeedDynamicContentType(ContentDbContext dbContext)
    {
        if (await dbContext.ContentTypes.AnyAsync(type => type.ApiName == DynamicContentTypeApiName))
        {
            return;
        }

        var contentTypeId = Guid.NewGuid();
        await dbContext.ContentTypes.AddAsync(
            new ContentTypeRecord
            {
                Id = contentTypeId,
                ApiName = DynamicContentTypeApiName,
                DisplayName = "Dynamic Product",
                Status = "Published",
                PublishedAt = DateTimeOffset.UtcNow,
                SchemaVersion = 2,
                Fields =
                [
                    new ContentFieldRecord
                    {
                        Id = Guid.NewGuid(),
                        ContentTypeId = contentTypeId,
                        ApiName = "title",
                        DisplayName = "Title",
                        Kind = "String",
                        Localization = "NonLocalized",
                        IsRequired = true
                    }
                ]
            });
        await dbContext.SaveChangesAsync();
    }
}
