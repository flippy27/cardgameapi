# CardDuel Server API - Deployment Guide

## Pre-Deployment Checklist

- [ ] PostgreSQL 16+ running
- [ ] Redis 7+ (optional for SignalR backplane)
- [ ] JWT_SIGNING_KEY env var set (min 32 chars)
- [ ] All tests passing: `dotnet test` (25/25)
- [ ] Build clean: `dotnet build` (0 errors)
- [ ] CORS origins restricted
- [ ] Secrets not in appsettings.json

## Docker Deployment

```bash
docker build -t cardduel-api:latest .

docker run -d --name cardduel-api -p 5000:5000 \
  -e JWT_SIGNING_KEY="your-secret-32-chars" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=postgres;Port=5432;Database=cardduel;User Id=postgres;Password=postgres;" \
  cardduel-api:latest
```

## Health & Monitoring

```bash
# Health check
curl http://localhost:5000/api/v1/health

# Prometheus metrics (no auth needed)
curl http://localhost:5000/metrics

# Admin metrics (needs auth)
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/api/v1/admin/metrics
```

## Key Endpoints

| Endpoint | Auth | Purpose |
|----------|------|---------|
| `/api/v1/health` | No | Service health |
| `/metrics` | No | Prometheus metrics |
| `/api/v1/admin/metrics` | Yes | Detailed metrics |
| `/swagger` | No | API documentation |
| `/api/v1/replays/{matchId}/validate` | Yes | Replay validation |

## Environment Variables

- `JWT_SIGNING_KEY` - Required, min 32 chars
- `ASPNETCORE_ENVIRONMENT` - Development/Staging/Production
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection
- `SignalR__UseRedisBackplane` - true/false for multi-instance

## Monitoring Metrics

```
cardduel_requests_total         - Total HTTP requests
cardduel_errors_total           - Total HTTP errors
cardduel_request_duration_avg_ms - Average latency
cardduel_error_rate_percent     - Error rate %
```

## Troubleshooting

```bash
# Check logs
docker logs cardduel-api

# Verify DB
curl http://localhost:5000/api/v1/health

# Test auth
curl -H "Authorization: Bearer <invalid>" \
  http://localhost:5000/api/v1/admin/metrics
```
