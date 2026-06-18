# Mosaic

Headless CMS on ASP.NET Core with runtime content modeling and GraphQL API.

## Stack

<video src="assets/system_demo.mp4" controls width="100%"></video>



- .NET 10, ASP.NET Core
- Hot Chocolate (GraphQL)
- PostgreSQL + EF Core
- Serilog

## Modules

| Module | Responsibility |
|---|---|
| Content | Content types, fields, items, versioning, dynamic GraphQL schema |
| Identity | Users, roles, groups, tokens, OAuth/OIDC |
| Media | File upload and storage |
| Search | PostgreSQL full-text search |

## Running locally

```bash
docker compose up -d postgres

dotnet tool run dotnet-ef database update \
  --project src/Modules/Content/Mosaic.Modules.Content.Infrastructure/Mosaic.Modules.Content.Infrastructure.csproj \
  --startup-project src/Mosaic.Api/Mosaic.Api.csproj \
  --context ContentDbContext

dotnet run --project src/Mosaic.Api/Mosaic.Api.csproj --urls http://localhost:5038
```

GraphQL API: `http://localhost:5038/graphql`  
GraphQL IDE: `http://localhost:5038/graphql/ui`  
Default credentials: `admin` / `admin`
