# Development Guide

## Prerequisites

- .NET 8+ SDK
- Docker + Docker Compose
- Git
- A text editor or IDE (VS Code, Rider, VS)

## Quick Start

### 1. Clone & Setup

```bash
git clone <repo>
cd cardgameapi
chmod +x start-dev.sh
./start-dev.sh
```

This will:
- Start PostgreSQL + Redis in Docker
- Run database migrations
- Show you the next steps

### 2. Run API

```bash
dotnet watch run
```

The API will start on `http://localhost:5000` with hot reload enabled.

### 3. Test

Visit:
- **Swagger UI**: http://localhost:5000/swagger
- **Health check**: http://localhost:5000/api/health

## Database

### Manual Migrations

```bash
# Create new migration after model changes
dotnet ef migrations add <MigrationName>

# Apply to database
dotnet ef database update

# View migration history
dotnet ef migrations list

# Remove last migration (if not applied)
dotnet ef migrations remove

# Drop entire database (WARNING!)
dotnet ef database drop
```

### Connection String

Edit `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=5432;Database=cardduel;User Id=postgres;Password=postgres;"
}
```

## Project Structure

```
cardgameapi/
├── Controllers/          # API endpoints
│   ├── AuthController.cs           (login/register)
│   ├── UsersController.cs          (profile/stats)
│   ├── DecksController.cs          (deck CRUD)
│   ├── CardsController.cs          (card catalog)
│   ├── MatchmakingController.cs    (queue)
│   ├── MatchesController.cs        (match actions)
│   ├── MatchHistoryController.cs   (history)
│   ├── AdminController.cs          (moderation)
│   └── HealthController.cs         (health check)
├── Services/            # Business logic
│   ├── InMemoryServices.cs         (legacy in-memory)
│   ├── RatingService.cs            (ELO calculation)
│   └── DeckValidationService.cs    (deck rules)
├── Infrastructure/      # Data access + utilities
│   ├── AppDbContext.cs             (EF Core context)
│   ├── Models/
│   │   ├── UserAccount.cs
│   │   ├── PlayerDeck.cs
│   │   ├── PlayerRating.cs
│   │   ├── MatchRecord.cs
│   │   └── ReplayLog.cs
│   ├── GlobalExceptionHandlerMiddleware.cs
│   └── Migrations/      (EF Core migrations)
├── Game/                # Game logic
│   └── MatchEngine.cs   (authoritative game rules)
├── Contracts/           # DTOs
│   └── ApiDtos.cs
├── Hubs/                # SignalR
│   └── MatchHub.cs
├── appsettings.json              (base config)
├── appsettings.Development.json  (local dev config)
├── Program.cs                    (startup)
├── Dockerfile
└── docker-compose.yml

```

## Common Tasks

### Adding a New Endpoint

1. Create controller in `Controllers/`
2. Add authorization if needed (`[Authorize]`)
3. Define DTOs in `Contracts/`
4. Add service if needed in `Services/`
5. Inject dependencies via constructor
6. Update `Program.cs` if adding new services

Example:

```csharp
[ApiController]
[Authorize]
[Route("api/example")]
public class ExampleController(IService service) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await service.GetAsync(id);
        return Ok(result);
    }
}
```

### Adding a Database Model

1. Create model in `Infrastructure/Models/`
2. Add `DbSet<Model>` to `AppDbContext.cs`
3. Configure in `OnModelCreating()` (indices, relationships, etc.)
4. Run migration:

```bash
dotnet ef migrations add Add<ModelName>
dotnet ef database update
```

### Running Tests

```bash
# All tests
dotnet test

# Specific test file
dotnet test --filter "NamespaceName.ClassName"

# Watch mode (re-run on change)
dotnet watch test
```

### Code Style

- Use C# 11+ features (records, nullable reference types)
- Prefix interface names with `I`
- Use `sealed` on concrete classes (for performance)
- Use `required` on record properties
- Validate inputs at API boundary, trust internal code
- Use async/await throughout
- Keep methods focused and small

Example:

```csharp
public sealed record UserCreateRequest(
    [property: Required] string Email,
    [property: Required] string Username);

public sealed class UserService(AppDbContext db)
{
    public async Task<User> CreateAsync(UserCreateRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
        {
            throw new InvalidOperationException("Email taken");
        }
        // ...
    }
}
```

## Debugging

### Local Debugging with VS Code

Install C# DevKit extension, then F5 to debug.

Breakpoints work in:
- Controllers
- Services
- Game logic

### Logging

Check logs in `logs/` directory:

```bash
tail -f logs/cardduel-*.txt
```

Or watch real-time console output.

### Database Inspection

Connect to PostgreSQL with:

```bash
docker-compose exec postgres psql -U postgres -d cardduel
```

Useful queries:

```sql
-- List all tables
\dt

-- View users
SELECT id, username, email, is_active FROM "Users";

-- View ratings
SELECT user_id, rating_value, wins, losses FROM "Ratings" ORDER BY rating_value DESC;

-- View recent matches
SELECT match_id, player1_id, player2_id, winner_id FROM "Matches" ORDER BY created_at DESC LIMIT 10;
```

## Performance Tips

1. **Database queries**: Use `.Include()` to avoid N+1
2. **Caching**: Add Redis caching for card catalog + leaderboard
3. **Async**: Always use `async/await`, never `.Result`
4. **Indices**: Add indices on frequently queried columns
5. **Batch operations**: Combine multiple DB writes when possible

## Environment Variables

For Docker deployment, set:

```bash
ConnectionStrings:DefaultConnection=...
Jwt:SigningKey=<secure-32-char-key>
ASPNETCORE_ENVIRONMENT=Production
SignalR:UseRedisBackplane=true
SignalR:RedisConnectionString=redis-host:6379
```

## Troubleshooting

### "postgres is not ready"

```bash
# Restart Docker
docker-compose down
docker-compose up -d
docker-compose logs postgres
```

### "migration failed"

```bash
# Reset database
dotnet ef database drop --force
dotnet ef database update
```

### "address already in use"

```bash
# Kill process on port 5000
lsof -ti:5000 | xargs kill -9
```

### Tests not finding dependencies

```bash
# Rebuild solution
dotnet clean
dotnet restore
dotnet build
```

## Git Workflow

```bash
# Feature branch
git checkout -b feature/your-feature

# Make changes, test locally
dotnet watch run

# Commit
git add .
git commit -m "feat: describe change"

# Push & create PR
git push origin feature/your-feature
```

## Deployment Checklist

- [ ] All tests pass
- [ ] No compiler warnings
- [ ] Database migrations tested
- [ ] Configuration validated (no hardcoded secrets)
- [ ] Dockerfile builds successfully
- [ ] Health endpoint returns `ok: true`
- [ ] API documentation updated
- [ ] Git commits are atomic and clean
- [ ] PR reviewed and approved

## Resources

- [.NET docs](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/)
- [SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr)
- [xUnit](https://xunit.net/)
- [PostgreSQL docs](https://www.postgresql.org/docs/)

---

**Last updated**: 2026-04-14
**Maintainer**: Team CardDuel
