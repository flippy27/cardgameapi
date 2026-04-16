# Troubleshooting Guide

## Common Issues & Solutions

### Development

#### Problem: "dotnet watch run" fails to start
```bash
# Solution: Clean and rebuild
dotnet clean
dotnet restore
dotnet build
dotnet watch run
```

#### Problem: "Port 5000 already in use"
```bash
# Solution: Kill process on port
lsof -ti:5000 | xargs kill -9
# Or use different port
dotnet run --urls "http://localhost:5001"
```

#### Problem: Database migration fails
```bash
# Solution: Reset database
dotnet ef database drop --force
dotnet ef database update
```

#### Problem: "No space left on device"
```bash
# Solution: Clean .nuget cache
rm -rf ~/.nuget/packages
dotnet restore
```

#### Problem: Hot reload not working
```bash
# Solution: Ensure watch tool installed
dotnet tool install -g dotnet-watch
dotnet watch run
```

### Docker

#### Problem: "docker-compose up" fails
```bash
# Solution: Restart Docker + clean volumes
docker-compose down -v
docker system prune -a
docker-compose up -d
```

#### Problem: Container crashes immediately
```bash
# Solution: Check logs
docker-compose logs api
# Check health check
docker-compose ps
```

#### Problem: PostgreSQL won't start
```bash
# Solution: Check disk space
df -h
# Reset volumes
docker volume prune
docker-compose up -d
```

### Database

#### Problem: "Connection refused" to PostgreSQL
```bash
# Solution: Wait for DB startup
docker-compose logs postgres
# or manually connect
psql -h localhost -U postgres
# If that fails, restart
docker-compose restart postgres
```

#### Problem: "permission denied" on database
```bash
# Solution: Check user permissions
psql -U postgres -c "GRANT ALL ON DATABASE cardduel TO postgres;"
```

#### Problem: Migration has syntax errors
```bash
# Solution: Edit migration file
vim CardDuel.ServerApi/Infrastructure/Migrations/[timestamp]_*.cs
# Re-apply
dotnet ef database update
```

#### Problem: Circular dependency in models
```bash
# Solution: Use OnDelete cascade or nullable FK
modelBuilder.Entity<A>()
    .HasMany(a => a.Bs)
    .WithOne(b => b.A)
    .OnDelete(DeleteBehavior.Cascade);
```

### API

#### Problem: 401 Unauthorized on protected endpoint
```
Cause: Missing or invalid JWT token
Solution:
1. Ensure token in Authorization header: "Authorization: Bearer <token>"
2. Verify token not expired (24h validity)
3. Check token signature (JWT:SigningKey must match)
4. Validate token includes 'sub' claim
```

#### Problem: 400 Bad Request on valid request
```
Cause: Input validation failed
Solution:
1. Check request body matches DTO schema
2. All [Required] fields must be present
3. String lengths must be within limits
4. Dates must be valid ISO 8601 format
5. Use Swagger UI to test: http://localhost:5000/swagger
```

#### Problem: 404 Not Found on known endpoint
```
Cause: Route mismatch or controller not registered
Solution:
1. Verify controller inherits from ControllerBase
2. Check [Route] attribute on controller
3. Verify controller registered in Program.cs (app.MapControllers())
4. Check HTTP method (GET vs POST, etc)
5. Swagger UI should show all routes
```

#### Problem: 500 Internal Server Error
```
Cause: Unhandled exception
Solution:
1. Check application logs: logs/cardduel-*.txt
2. Check console output
3. Verify database is accessible
4. Check configuration (appsettings.json)
5. Enable Debug logging in appsettings.Development.json
```

#### Problem: SignalR connection fails
```
Cause: Connection issues or auth failure
Solution:
1. Verify WebSocket support enabled
2. Check access_token query parameter
3. Verify JWT token is valid
4. Check CORS if cross-origin
5. Test with Swagger/Postman first
```

### Performance

#### Problem: API responding slowly
```bash
# Solution: Check slow queries
psql -d cardduel -c "SELECT query, mean_time FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 10;"

# Add missing indices
CREATE INDEX idx_matches_player ON "Matches"(player1_id, player2_id);
CREATE INDEX idx_ratings_user ON "Ratings"(user_id);
CREATE INDEX idx_decks_user ON "PlayerDecks"(user_id);
```

#### Problem: High memory usage
```bash
# Solution: Restart API
docker restart cardduel-api

# Or scale down
docker-compose scale api=1
```

#### Problem: Database connection pool exhausted
```
Cause: Too many open connections
Solution:
1. Increase max connection pool size in connection string
2. Max Pool Size=100
3. Implement connection pooling
4. Check for connection leaks (ensure using 'using' statement)
```

### Testing

#### Problem: Tests fail with "No space left on device"
```bash
# Solution: Clean NuGet cache then test
rm -rf ~/.nuget/packages
dotnet test
```

#### Problem: Test timeout
```bash
# Solution: Increase timeout
dotnet test --logger "console;verbosity=detailed" -- RunConfiguration.TargetPlatform=x64
```

#### Problem: Mock not working as expected
```csharp
# Solution: Verify setup() call
var mock = new Mock<IService>();
mock.Setup(x => x.GetAsync(It.IsAny<string>()))
    .ReturnsAsync(new Result()); // Must match return type
```

### Deployment

#### Problem: Docker image won't push to registry
```bash
# Solution: Authenticate and retry
docker login myregistry.azurecr.io
docker push myregistry.azurecr.io/cardduel-api:latest
```

#### Problem: Kubernetes pod keeps restarting
```bash
# Solution: Check logs and events
kubectl logs pod/cardduel-api-xxx
kubectl describe pod/cardduel-api-xxx
kubectl get events
```

#### Problem: Health check failing in production
```bash
# Solution: Verify endpoint
curl -v https://api.cardduel.io/api/health
# Check database connection
# Verify all required env vars set
```

## Debug Techniques

### Enable Debug Logging
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}
```

### SQL Query Logging
```csharp
optionsBuilder
    .EnableSensitiveDataLogging()
    .LogTo(Console.WriteLine);
```

### SignalR Logging
```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
});
```

### Request/Response Logging
```csharp
app.Use(async (context, next) =>
{
    _logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
    _logger.LogInformation($"Response: {context.Response.StatusCode}");
});
```

### Database Connection Test
```csharp
using (var connection = new NpgsqlConnection(connectionString))
{
    connection.Open();
    using (var cmd = connection.CreateCommand())
    {
        cmd.CommandText = "SELECT 1";
        var result = cmd.ExecuteScalar();
        Console.WriteLine($"Database OK: {result}");
    }
}
```

## Performance Profiling

### Database Query Analysis
```sql
-- Slow queries
SELECT query, mean_time, calls 
FROM pg_stat_statements 
ORDER BY mean_time DESC 
LIMIT 10;

-- Missing indices
SELECT schemaname, tablename, indexname 
FROM pg_indexes 
WHERE schemaname NOT IN ('pg_catalog', 'information_schema');
```

### Memory Profiling
```bash
# Use dotMemory or profiler
dotnet new profiler --type memory
```

### HTTP Request Profiling
```bash
# Use Fiddler or similar
# Or Swagger's built-in network tab
```

## Common Error Messages

| Error | Cause | Solution |
|-------|-------|----------|
| `NETSDK1064` | Package not found | `dotnet restore --force` |
| `Unable to connect to the database` | DB down | Restart PostgreSQL |
| `Invalid token` | JWT expired/invalid | Re-login for new token |
| `Rate limit exceeded` | Too many requests | Reduce request rate |
| `Deck validation failed` | Invalid cards | Check deck constraints |
| `Connection refused` | Port in use | Kill process or use different port |
| `No matching overload` | Type mismatch | Check parameter types |
| `Null reference exception` | Null object access | Add null checks |

## Getting Help

1. **Check logs**: `logs/cardduel-*.txt`
2. **Search docs**: README.md, DEVELOPMENT.md
3. **Run tests**: `dotnet test`
4. **Use Swagger**: http://localhost:5000/swagger
5. **Check Git history**: `git log --oneline` for recent changes
6. **Ask in Discord**: #support channel

---

**Last updated**: 2026-04-14  
**Maintained by**: Development Team
