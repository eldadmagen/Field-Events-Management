# Field Events Management

Real-time field-event coordination system: external sources report events through an Agent, which
forwards them to a central Server that dispatchers and technicians interact with in real time.

See [`docs/Field_Events_Architecture.docx`](docs/Field_Events_Architecture.docx)  for the architectural decisions and trade-offs
(Markdown, with Mermaid diagrams - renders natively on GitHub/GitLab/VS Code). A Word version with
the same content and rendered diagram images 


## Solution layout

```
FieldEvents.sln
src/
  FieldEvents.Shared/    Contracts shared between Agent and Server (DTOs, enums, hub route names)
  FieldEvents.Server/    ASP.NET Core Web API + SignalR + EF Core/SQLite (the central server)
  FieldEvents.Agent/     Worker Service: ingest endpoint + local outbox + SignalR client to Server
  FieldEvents.Client/    Angular app (Dispatcher / Technician views)
tests/
  FieldEvents.Server.Tests/   xUnit - EventStateMachine + FieldEvent transition tests
```

## Prerequisites

- .NET SDK 9.0 (the projects target `net9.0`)
- Node.js 22.x + npm (for the Angular client)
- A trusted local HTTPS dev cert: `dotnet dev-certs https --trust`

## Running the required E2E flow

Run these in **three separate terminals**, in this order.

### 1. Server

```
dotnet run --project src/FieldEvents.Server --launch-profile https
```

Listens on `https://localhost:7180`. On first run it creates `fieldevents.db` (SQLite) and seeds
three demo users (username / password, all `Passw0rd!`):

| Username     | Role       |
|--------------|------------|
| dispatcher1  | Dispatcher |
| tech1        | Technician |
| tech2        | Technician |

Swagger UI: `https://localhost:7180/swagger`.

### 2. Agent

```
dotnet run --project src/FieldEvents.Agent
```

Listens on `http://localhost:5080` and connects out to the Server's `EventsHub`. Two demo sources
are pre-registered in `src/FieldEvents.Agent/appsettings.json`:

| Source ID       | API Key             |
|------------------|----------------------|
| sensor-1         | sensor-1-key         |
| manual-report    | manual-report-key    |

### 3. Client

```
cd src/FieldEvents.Client
npm install   # first time only
npm start
```

Open `http://localhost:4200`, log in as `dispatcher1` / `Passw0rd!`.

### 4. Trigger the flow

With Server + Agent + Client all running, simulate an external source reporting an event:

```
curl -X POST http://localhost:5080/ingest/sensor-1 \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: sensor-1-key" \
  -d '{"title":"Gas leak detected","description":"Sensor threshold exceeded on line 3","location":"Building A - Zone 3","priority":2}'
```

The event appears in the Dispatcher dashboard immediately, with no page refresh - that's the whole
flow: external source -> Agent (outbox) -> SignalR -> Server -> SQLite -> SignalR -> Dispatcher UI.

### Verified manually (not just designed on paper)

Both of these were actually run against this codebase, not just reasoned about:

- **Happy path**: `curl` to the Agent -> event visible via `GET /api/events` with a dispatcher JWT,
  Agent log shows `Forwarded event ... -> server event N`.
- **Server-outage resilience**: started the Agent with the Server stopped, POSTed an event (still got
  `202 Accepted` immediately), confirmed the Agent kept retrying the connection in the background,
  then started the Server - the queued event was forwarded and persisted automatically within
  seconds, with no manual replay. This is the "outbox" design from `docs/ARCHITECTURE.md` working
  as intended, not just described.
- **Authorization**: a technician JWT against the dispatcher-only `GET /api/events` returns `403`;
  no token returns `401`.
- Also caught and fixed a real bug this way: EF Core + SQLite cannot `ORDER BY` a `DateTimeOffset`
  column directly - event listing now orders by `Id` instead.

## Running tests

```
dotnet test tests/FieldEvents.Server.Tests
```

## What's fully implemented vs. skeleton

- **Fully implemented, E2E**: the flow above, JWT auth + role authorization, the Agent's local
  outbox (survives Server outages and Agent restarts), automatic reconnect, the State Machine and
  its audit history, dispatcher assign/transfer/status/comment REST endpoints.
- **Skeleton / stub (by design - see docs/ARCHITECTURE.md)**: offline push notifications
  (`IPushNotificationChannel` / `WebPushNotificationChannel`) - the interface, DB schema
  (`PushSubscription`) and call site are wired up, but no real Web Push delivery is implemented.
  The Technician UI reads its assigned events over REST but isn't wired to live SignalR push
  (the server-side notification call already exists in `NotificationService.NotifyTechnicianAsync`).

## Notes on configuration

`appsettings.Development.json` in the Server, and `appsettings.json` in the Agent, contain a fixed
demo JWT signing key and Agent API key **for local development only**. In any real deployment these
would come from environment variables / a secret store, not source control.

## help
- AI like clode and gemini
