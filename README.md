# Database Auto-Update System POC

A distributed IoT data synchronization system implementing a hub-and-spoke pattern for edge device data management.

## Architecture Overview

### Central System (Hub)
- **Central API**: ASP.NET Core Web API handling sync requests
- **Central Database**: PostgreSQL with master data and audit trails
- **Seq Logging**: Centralized log aggregation

### Edge System (Spokes)
- **Edge Service**: Background worker with sync logic
- **Edge Database**: Local PostgreSQL cache
- **Sync Worker**: Automated 5-minute sync intervals

## Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 10.0 SDK (for development)

### Running the POC

1. **Start Central System**:
   ```bash
   docker-compose -f docker-compose.central.yml up -d
   ```

2. **Start Edge System**:
   ```bash
   docker-compose -f docker-compose.edge.yml up -d
   ```

3. **Verify Services**:
   - Central API: http://localhost:8080/health
   - Edge Service: http://localhost:8081/health
   - Seq Logs: http://localhost:5341

### Manual Sync Trigger

```bash
curl -X POST http://localhost:8081/api/sync/now
```

## Key Features

### Data Flow
1. **Edge Request**: Jetson calls Central `/api/device-sync/request` with MAC address
2. **Scope Resolution**: Central determines device visibility (company → location → groups → users)
3. **Manifest Generation**: ULID-based manifest with table checksums (SHA256)
4. **Data Transfer**: Compressed JSON payload with full dataset
5. **Transactional Load**: Edge truncates + bulk inserts within single transaction
6. **Verification**: Hash comparison ensures data integrity
7. **Acknowledgment**: Edge reports results back to Central

### Security & Consistency
- **MAC-based Authentication**: Device identification via hardware address
- **Schema Versioning**: Prevents central/edge schema drift
- **Idempotency**: ManifestId prevents duplicate processing
- **ACID Transactions**: All-or-nothing data updates
- **Canonical Hashing**: Deterministic data integrity verification

## Test Device Configuration

The POC includes a pre-configured test device:
- **MAC Address**: `48:b0:2d:e9:c3:b7`
- **Location**: San Francisco Office (TechCorp Industries)
- **Model**: Jetson Xavier NX

## Database Schema

### Central Tables
- Business Data: `companies`, `locations`, `groups`, `users`, `areas`, `devices`
- Audit: `sync_requests`, `sync_manifests`, `sync_acknowledgements`

### Edge Tables
- Cached Data: Mirror of central business tables
- Audit: `edge_sync_log`, `edge_sync_tables`, `edge_sync_state`

## Monitoring & Observability

- **Structured Logging**: Serilog with Seq integration
- **Health Checks**: `/health` endpoints for container monitoring
- **Audit Trail**: Complete request/response tracking
- **Performance Metrics**: Sync duration and row counts

## Technology Stack

- **Runtime**: ASP.NET Core 10 (preview)
- **Database**: PostgreSQL 15+ with Npgsql
- **ORM**: Entity Framework Core 9.0
- **Resilience**: Polly retry policies
- **Scheduling**: Background services with Quartz.NET
- **Containerization**: Docker with multi-stage builds
- **Logging**: Serilog + Seq

## Development Commands

```bash
# Build solutions
dotnet build

# Run migrations (Central)
dotnet ef migrations add InitialCreate -p src/Central.Data -s src/Central.Api
dotnet ef database update -p src/Central.Data -s src/Central.Api

# Run migrations (Edge)
dotnet ef migrations add InitialCreate -p src/Edge.Service
dotnet ef database update -p src/Edge.Service

# Run tests
dotnet test

# Build Docker images
docker build -f src/Central.Api/Dockerfile -t central-api .
docker build -f src/Edge.Service/Dockerfile -t edge-service .
```

## POC Limitations

- Basic MAC-based authentication (production should use certificates)
- No data compression (can add gzip for large payloads)
- Single-threaded sync processing
- In-memory retry policies (consider persistent queues)
- Basic error handling (expand for production scenarios)

## Next Steps for Production

1. **Enhanced Security**: Certificate-based device authentication
2. **Performance**: Incremental sync, parallel processing
3. **Reliability**: Message queues, circuit breakers
4. **Monitoring**: Prometheus metrics, distributed tracing
5. **Deployment**: Kubernetes manifests, GitOps workflows