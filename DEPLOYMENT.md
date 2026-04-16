# Production Deployment Guide

## Prerequisites

- Docker + Docker Compose
- Kubernetes (optional, for k8s deployment)
- PostgreSQL 14+ (managed or self-hosted)
- Redis 7+ (for SignalR backplane, optional)
- Domain name + SSL certificate

## Environment Variables (Production)

```bash
# Database
ConnectionStrings:DefaultConnection=Server=prod-postgres.example.com;Port=5432;Database=cardduel_prod;User Id=pguser;Password=<SECURE_PASSWORD>;

# JWT
Jwt:SigningKey=<64-char-random-secure-key>
Jwt:Issuer=cardduel-server
Jwt:Audience=cardduel-clients

# Logging
Logging:LogLevel:Default=Warning
Logging:LogLevel:Microsoft=Error

# SignalR Backplane (Redis)
SignalR:UseRedisBackplane=true
SignalR:RedisConnectionString=prod-redis.example.com:6379

# ASP.NET
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# Game Config
Game:DisconnectGraceSeconds=30
Game:MaxDeckSize=30
Game:MinDeckSize=20
```

## Docker Build & Push

```bash
# Build image
docker build -t cardduel-api:latest .
docker tag cardduel-api:latest myregistry.azurecr.io/cardduel-api:latest

# Push to registry
docker push myregistry.azurecr.io/cardduel-api:latest

# Verify image
docker run --rm myregistry.azurecr.io/cardduel-api:latest dotnet CardDuel.ServerApi.dll --version
```

## Database Setup

### PostgreSQL Setup

```bash
# Create database
createdb -U postgres -h prod-postgres.example.com cardduel_prod

# Restore from backup (if applicable)
pg_restore -U postgres -h prod-postgres.example.com -d cardduel_prod backup.dump

# Enable extensions
psql -U postgres -h prod-postgres.example.com -d cardduel_prod -c "CREATE EXTENSION IF NOT EXISTS uuid-ossp;"
psql -U postgres -h prod-postgres.example.com -d cardduel_prod -c "CREATE EXTENSION IF NOT EXISTS json1;"
```

### Run Migrations

```bash
# In container or locally
dotnet ef database update --connection "Server=prod-postgres.example.com;..."
```

## Docker Compose (Single Server)

```yaml
version: '3.8'

services:
  api:
    image: myregistry.azurecr.io/cardduel-api:latest
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Server=postgres;Port=5432;Database=cardduel_prod;..."
      Jwt__SigningKey: "${JWT_SIGNING_KEY}"
      SignalR__UseRedisBackplane: "true"
      SignalR__RedisConnectionString: "redis:6379"
      ASPNETCORE_ENVIRONMENT: "Production"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
      interval: 30s
      timeout: 3s
      retries: 3
    restart: unless-stopped

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: cardduel_prod
      POSTGRES_USER: pguser
      POSTGRES_PASSWORD: "${DB_PASSWORD}"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U pguser"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

Deploy with:

```bash
docker-compose -f docker-compose.prod.yml up -d
```

## Kubernetes Deployment

### ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: cardduel-config
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  Logging__LogLevel__Default: "Warning"
  Game__DisconnectGraceSeconds: "30"
```

### Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: cardduel-secrets
type: Opaque
stringData:
  Jwt__SigningKey: "<SECURE_KEY>"
  ConnectionStrings__DefaultConnection: "Server=postgres-svc;..."
  SignalR__RedisConnectionString: "redis-svc:6379"
```

### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: cardduel-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: cardduel-api
  template:
    metadata:
      labels:
        app: cardduel-api
    spec:
      containers:
      - name: api
        image: myregistry.azurecr.io/cardduel-api:latest
        ports:
        - containerPort: 8080
        envFrom:
        - configMapRef:
            name: cardduel-config
        - secretRef:
            name: cardduel-secrets
        livenessProbe:
          httpGet:
            path: /api/health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /api/health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
        resources:
          requests:
            cpu: "500m"
            memory: "512Mi"
          limits:
            cpu: "1000m"
            memory: "1Gi"
```

### Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: cardduel-api-svc
spec:
  selector:
    app: cardduel-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: LoadBalancer
```

Deploy with:

```bash
kubectl apply -f configmap.yaml
kubectl apply -f secret.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
```

## Nginx Reverse Proxy

```nginx
upstream cardduel {
    server api:8080;
}

server {
    listen 80;
    server_name api.cardduel.io;

    # Redirect to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name api.cardduel.io;

    ssl_certificate /etc/ssl/certs/api.cardduel.io.crt;
    ssl_certificate_key /etc/ssl/private/api.cardduel.io.key;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # CORS
    add_header Access-Control-Allow-Origin "https://game.cardduel.io" always;
    add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
    add_header Access-Control-Allow-Headers "Content-Type, Authorization" always;

    # Proxy
    location / {
        proxy_pass http://cardduel;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # WebSocket support
    location /hubs/match {
        proxy_pass http://cardduel;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_read_timeout 86400;
    }
}
```

## Monitoring

### Health Checks

```bash
# Basic health
curl https://api.cardduel.io/api/health

# Expected response
{
  "ok": true,
  "service": "cardduel-server-api",
  "version": "1.0.0",
  "environment": "Production",
  "utc": "2026-04-14T20:30:00Z",
  "database": "healthy"
}
```

### Logging & Observability

```bash
# View logs
docker logs -f cardduel-api

# Or k8s
kubectl logs -f deployment/cardduel-api

# Application logs in `/var/lib/cardduel/logs/`
tail -f /var/lib/cardduel/logs/cardduel-*.txt
```

### Metrics

Integrate with:
- Prometheus (metrics scraping)
- Grafana (dashboards)
- Application Insights (Azure)
- DataDog (observability)

## Backup & Recovery

### Database Backup

```bash
# Full backup
pg_dump -U postgres -h prod-postgres.example.com cardduel_prod | gzip > backup-$(date +%Y%m%d).sql.gz

# Automated daily
0 2 * * * pg_dump -U postgres -h prod-postgres.example.com cardduel_prod | gzip > /backups/cardduel-$(date +%Y%m%d).sql.gz
```

### Restore from Backup

```bash
# Stop API
docker stop cardduel-api

# Restore
gunzip < backup-20260414.sql.gz | psql -U postgres -h prod-postgres.example.com -d cardduel_prod

# Start API
docker start cardduel-api
```

## Scaling Considerations

### Horizontal Scaling

1. **Stateless API**: Multiple instances behind load balancer
2. **Redis Backplane**: For SignalR hub coordination
3. **Database**: Connection pooling, read replicas
4. **Caching**: Distributed cache (Redis)

### Load Balancing

```nginx
upstream cardduel_pool {
    least_conn;
    server api-1:8080;
    server api-2:8080;
    server api-3:8080;
    keepalive 32;
}
```

### Database Optimization

- Add indices on frequently queried columns
- Enable query caching
- Monitor slow queries
- Use connection pooling (min 10, max 50)

## Security Checklist

- ✅ HTTPS only
- ✅ JWT signing key (64+ chars)
- ✅ Database password (30+ chars, special chars)
- ✅ Network isolation (VPC, security groups)
- ✅ WAF rules (if using CDN)
- ✅ Rate limiting per IP
- ✅ DDoS protection
- ✅ Audit logging
- ✅ Regular updates

## Rollback Plan

```bash
# Keep previous version
docker tag cardduel-api:latest cardduel-api:previous

# If issues, revert
docker pull myregistry.azurecr.io/cardduel-api:previous
docker tag myregistry.azurecr.io/cardduel-api:previous cardduel-api:latest
docker-compose up -d
```

## Incident Response

### API Down

```bash
# Check status
curl -I https://api.cardduel.io/api/health

# Check logs
docker logs cardduel-api | tail -100

# Restart
docker restart cardduel-api
```

### Database Down

```bash
# Check connection
psql -U postgres -h prod-postgres.example.com -d cardduel_prod -c "SELECT 1;"

# If unresponsive, failover to replica
# (assumes HA setup)
```

### High Latency

```bash
# Check database query times
SELECT query, mean_time, calls FROM pg_stat_statements ORDER BY mean_time DESC;

# Check API metrics
# (DataDog / Application Insights)

# Scale API instances if needed
docker-compose scale api=5
```

---

**Last updated**: 2026-04-14
**Status**: Production ready
