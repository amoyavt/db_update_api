# Database Update System

The design should be built with containers, focused on API-driven full refresh of each Jetson's local Postgres, plus proper auditing/observability.

## Problem

Our company has several Edge IoT devices (Nvidia Jetson boards) running several services in Docker containers. Each device is identified with its MAC address and some services need to access a local PostgreSQL DB. This project builds a system that has Central services (running inside a cloud provider) and Edge services (running inside the Jetson).

## Goals & Ground Rules

- Single source of truth = Central Postgres
- Jetsons hold a read-optimized local subset (full overwrite per sync)
- Sync trigger: Jetson calls a Central "DB Update" API
- Auditable end-to-end: who requested what, when, what version, how many rows, checksums, success/failure. All services write logs. Every step should log events for start and finish, success and error

## High-Level Architecture (POC)

### Central System (Compose stack A)

- **central-postgres**: PostgreSQL 15+ with seed data
- **central-db-update-api**: ASP.NET Core 10 Web API that:
  - Authenticates device (by MAC in POC; recommend a stable device GUID too)
  - Computes the device's visibility scope (company → locations ↔ groups → users, areas, device)
  - Returns a manifest + JSON snapshot (the minimal set a device needs)
  - Logs/audits every request and the exact payload hash sent

### Jetson System (Compose stack B, per device)

- **edge-postgres**: PostgreSQL 15+ for the local cache
- **dbupdate-service**: ASP.NET Core 10 Web API/worker that:
  - Request and update every 5 mins
  - Validates manifest, writes data from zero (transactional truncate+insert)
  - Computes row counts & checksums, stores sync ledger locally, and posts an ack to central (optional for POC)

## Data Model Scope (what the device receives)

From central perspective, given device MAC (and known device→location mapping), return:

- **companies**: the company (or companies) the device's location belongs to
- **locations**: the device's location (and optionally related locations in the same company if needed)
- **groups**: groups linked to those locations
- **users**: users that belong to those groups and/or locations/companies per your rule set
- **areas**: areas assigned to the device's location
- **devices**: the device itself (and optionally peers in same location, if needed)

**POC simplification**: pick one device → one location → one company; include all groups attached to that location and all users in those groups.

## Central API — Responsibilities & Contracts

### Endpoints (POC)

#### Request Device Sync

```http
POST /api/device-sync/request
```

**Body:**
```json
{ "mac": "48:b0:2d:e9:c3:b7" }
```

**Response:**

**Manifest:**
```json
{
  "manifestId": "ulid",
  "generatedAt": "...",
  "schemaVersion": 1,
  "tables": [
    {
      "name": "users",
      "rowCount": 123,
      "sha256": "..."
    }
  ],
  "expiresAt": "...",
  "filters": {
    "locationId": "..."
  }
}
```

**Payload:** a single compressed JSON document or NDJSON per table (POC: 1 JSON with arrays per table).

#### Acknowledge Sync

```http
POST /api/device-sync/ack
```

**Body:**
```json
{
  "manifestId": "...",
  "mac": "...",
  "status": "Success|Failed",
  "localCounts": {...},
  "localChecksums": {...},
  "durationMs": 12345,
  "error": null
}
```

### Central API Internals

#### Scope Calculation

1. Resolve device by MAC → location → company
2. Collect related groups, users, areas by join tables

#### Snapshot Builder

1. Query each table deterministically (stable ordering)
2. Produce canonical JSON and table checksums (sha256)
3. Wrap in a Manifest with manifestId (ULID), counts, per-table hashes, and a global hash

#### Audit Tables (Central)

- `sync_requests(id, mac, manifest_id, requested_at, status, reason)`
- `sync_manifests(manifest_id, generated_at, table_name, row_count, sha256, filter_desc)`
- `sync_acknowledgements(manifest_id, mac, completed_at, result, duration_ms, device_counts_json, device_hashes_json, error_text)`

### Technologies (Central)

- **ASP.NET Core 10** (Minimal APIs or Controllers)
- **Data**: Npgsql (+ EF Core for relational mapping) and/or Dapper for fast read queries
- **Mapping**: AutoMapper (optional)
- **Validation**: FluentValidation
- **Resilience**: Polly (retry/timeout/backoff)
- **Logging**: Serilog + Seq
- **Tracing/Metrics**: OpenTelemetry (HTTP + DB)
- **Packaging**: Docker, docker-compose
- **DB migrations/seed**: EF Core Migrations; fake data with Bogus

## Edge (Jetson) — DB Update Service Design

### Workflow (full overwrite)

#### 1. Trigger (timer/manual/HTTP)

- Cron via Hangfire or Quartz.NET (e.g., every N minutes) in dbupdate-service
- Local API endpoint `/sync/now`

#### 2. Pull

- Call central `POST /device-sync/request` with device MAC
- Validate manifest signature/hash (basic POC: compare SHA and schemaVersion)

#### 3. Load (transactional)

1. Start transaction
2. For each table in a known order (respect FKs):
   - `TRUNCATE TABLE {table} RESTART IDENTITY CASCADE;`
   - Bulk insert from payload (COPY via Npgsql COPY API or PgBulk/SqlBulkCopy equivalent for Postgres)
3. Commit

#### 4. Verify

- Count rows and compute local table hashes (canonical JSON, same order) → compare with manifest

#### 5. Record & Ack

- Write local audit row with counts/hashes, started/completed, manifestId, and result
- Optionally call central `/ack` with results

### Edge Audit Tables

- `edge_sync_log(id, manifest_id, started_at, completed_at, status, duration_ms, error_text)`
- `edge_sync_tables(edge_sync_id, table_name, row_count, sha256)`
- `edge_sync_state(key, value)` for last successful manifest_id, timestamps, etc.

### Technologies (Edge)

- **ASP.NET Core 10** (BackgroundService + small API for health/trigger)
- **Postgres client**: Npgsql + COPY support for speed
- **Scheduler**: Hangfire (uses edge-postgres as storage) or Quartz.NET
- **Resilience**: Polly
- **Logging/Metrics**: Serilog + OpenTelemetry
- **Health**: ASP.NET HealthChecks + container HEALTHCHECK

## Consistency & Safety Tactics

### Data Consistency

- Canonical ordering when generating hashes (e.g., by PK ascending, stable column order, normalized JSON)
- One transaction per full load; if it fails, nothing changes

### Schema Drift Control

- Include `schemaVersion` in manifest and in edge service config; if mismatch, refuse sync and raise alert
- Use EF Core Migrations or Flyway for both central and edge schemas; ship migrations with images

### Idempotency

- `manifestId` ensures the same snapshot isn't applied twice (edge can check last applied)

### Performance

- Use COPY (binary preferred) to bulk load
- Disable/reenable non-essential indexes for large loads in non-POC stages (optional)

## Observability & Auditing

### Central

- Every `/request` and `/ack` stored with manifest, MAC, row counts, hashes, duration
- **Dashboards**: "requests by hour", "success/failure rates", "row deltas by table", "devices without ack in N hours"

### Edge

- `edge_sync_log` keeps start/finish, duration, result, last error
- Expose `/healthz` and `/metrics` (Prometheus)

### Logs

- **Serilog sinks**: console + Seq (fast POC visibility)

### Tracing

- **OpenTelemetry traces**: request → snapshot build → edge pull → load; correlate via manifestId
