# Librarium

A library management backend built with ASP.NET Core 8, Entity Framework Core, and PostgreSQL.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker](https://www.docker.com/products/docker-desktop) (for running PostgreSQL)
- [dotnet-ef CLI tool](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

Install the EF Core CLI tool if you haven't already:

```bash
dotnet tool install --global dotnet-ef
```

## Setup

### 1. Start the database

```bash
docker run -d \
  --name librarium-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=librarium \
  -p 5432:5432 \
  postgres:16
```

If you have already created the container before, start it with:

```bash
docker start librarium-postgres
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Apply migrations

```bash
dotnet ef database update --project src/Librarium.Data --startup-project src/Librarium.Api
```

This applies all migrations in order, bringing the database to the latest schema.

### 4. Run the API

```bash
dotnet run --project src/Librarium.Api
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`).
Swagger UI is available at `https://localhost:5001/swagger` in development.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/books` | List all non-retired books with authors |
| GET | `/api/v2/books` | v2 — same as above, with string ISBN (may be null during transition) |
| GET | `/api/members` | List all members |
| POST | `/api/loans` | Create a loan record |
| GET | `/api/loans/{memberId}` | Get all loans for a member |

## Connection String

The default connection string is in `src/Librarium.Api/appsettings.json`:

```
Host=localhost;Port=5432;Database=librarium;Username=postgres;Password=postgres
```

To override it (e.g. for a different environment), set the environment variable:

```bash
ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=librarium;Username=...;Password=..."
```

## Project Structure

```
/src
  /Librarium.Api      # ASP.NET Core Web API — controllers, Program.cs
  /Librarium.Data     # EF Core DbContext, entities, migrations
/migrations
  /sql                # SQL artifacts, one per migration
  /README.md          # Migration log — decisions and tradeoffs
```
