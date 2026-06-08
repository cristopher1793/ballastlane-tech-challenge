# TaskApp — Technical Interview Exercise

A fullstack task management application built with Clean Architecture principles.

## User Story

> As a user, I want to manage my daily tasks through a web application where I can register an account, log in securely, and perform full CRUD operations on my tasks — with a dashboard showing productivity analytics.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [MongoDB](https://www.mongodb.com/try/download/community) running on `localhost:27017`
- **Visual Studio 2022** (the solution requires VS 2022 or later — VS 2019 is not supported)

---

## Setup & Run

### Backend

```bash
cd c:\Pro\BallastLane
dotnet run --project TaskApp.API
```

API runs at `https://localhost:7020`  
Swagger UI: `https://localhost:7020/swagger`

### Frontend

```bash
cd c:\Pro\BallastLane\taskapp-frontend
npm install
npm run dev
```

Frontend runs at `http://localhost:5173`

---

## Demo Credentials

| Username | Email | Password | Role |
|---|---|---|---|
| `demo` | `demo@taskapp.com` | `Demo1234!` | User |
| `admin` | `admin@taskapp.com` | `Admin1234!` | Admin |

Seed data runs automatically on backend startup (idempotent).

> **Tip:** After logging in, open the user menu (top-right) and click **Generate Demo Data** to populate 13 realistic tasks across all statuses and story point values — this gives the dashboard charts meaningful data immediately.

---

## Features

### Task Management
- Create, read, update, and delete tasks
- Inline status and due-date editing directly in the task list (no dialog required)
- Story points per task — the UI exposes Fibonacci values (1, 2, 3, 5, 8); the API accepts any integer from 1–100
- Label tagging with a Jira-style combobox (create labels on the fly)
- Filter by status and label; pagination with configurable page size

### Dashboard Analytics
- **Stat cards** — total tasks broken down by status
- **Pie chart** — visual distribution across all statuses
- **Completion timing chart** — dumbbell chart showing due date vs actual completed date per task; green = on time, red = late
- **Weekly velocity** — story points and task count completed per week
- **Estimation accuracy** — average days to complete grouped by story point value

### Authentication & Security
- JWT Bearer authentication, token stored in `sessionStorage`
- Automatic session expiry detection — any 401 from an authenticated endpoint logs the user out immediately
- Account lockout after 3 consecutive failed logins; admin can lock or unlock any account
- Rate limiting: 10 req/min on `/api/auth/*`, 60 req/min on all other endpoints

### Administration (Admin role)
- Admin users see all tasks in the system, not just their own; an owner filter in the task list lets admins narrow to a specific user
- Users page: lists all registered users with role badges, status badges, failed-login-attempt count, and inline Lock/Unlock actions
- Admin can lock any active account or unlock any locked account

### UX
- Responsive layout: left sidebar navigation on desktop, bottom tab bar on mobile
- Sticky top header with user menu (edit profile, generate demo data, logout)
- Global toast notifications for all actions

---

## Architecture Diagram

```
┌──────────────────────────────────────────────────┐
│  Frontend (React + TypeScript + Vite)            │
│  Tailwind CSS  ·  Radix UI  ·  Recharts          │
└─────────────────────────┬────────────────────────┘
                          │ HTTPS / JSON
┌─────────────────────────▼────────────────────────┐
│  API Layer  (ASP.NET Core 8)                     │
│  Controllers  ·  Middleware  ·  Program.cs       │
└──────┬───────────────────┬────────────────────────┘
       │                   │
┌──────▼──────┐   ┌────────▼────────────────────────┐
│  Application│   │  Infrastructure                 │
│  Services   │   │  MongoDB Repositories           │
│  DTOs       │   │  JWT Generator                  │
│  Interfaces │   │  Password Hasher                │
└──────┬──────┘   └────────┬────────────────────────┘
       │                   │
┌──────▼───────────────────▼────────────────────────┐
│  Domain                                           │
│  Entities  ·  Enums  ·  Repository Interfaces    │
└───────────────────────────────────────────────────┘
```

---

## Layer Responsibilities

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, enums, exceptions, repository interfaces. Depends on `MongoDB.Driver` for BSON attribute mapping on entities — the only pragmatic exception; see Architecture Decisions. |
| **Application** | DTOs, service interfaces, service implementations, business validation. Depends on Domain only. |
| **Infrastructure** | MongoDB repositories, JWT generator, password hasher, `CurrentUserService`, database seeder. |
| **API** | ASP.NET Core controllers, rate limiting, DI wiring in `Program.cs`. |
| **Tests** | Unit (xUnit + Moq) and integration (real MongoDB + WebApplicationFactory) tests. |

---

## API Endpoints

### Auth (`/api/auth`) — 10 req/min/IP

| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/api/auth/ping` | Public | Health check |
| POST | `/api/auth/register` | Public | Returns `UserResponseDto` 201 |
| POST | `/api/auth/login` | Public | Returns `LoginResponseDto`; 403 if locked |
| GET | `/api/auth/me` | JWT | Current user |
| PUT | `/api/auth/profile` | JWT | Update name, username, email, password |
| GET | `/api/auth/users` | Admin JWT | List all registered users |
| POST | `/api/auth/lock/{userId}` | Admin JWT | Lock an active account |
| POST | `/api/auth/unlock/{userId}` | Admin JWT | Unlock a locked account; resets failed attempts |

### Tasks (`/api/tasks`) — 60 req/min/IP

| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/api/tasks` | JWT | Own tasks (regular user) or all tasks in the system with owner info (admin) |
| GET | `/api/tasks/labels` | JWT | Own distinct labels (regular user) or all labels across the system (admin) |
| GET | `/api/tasks/dashboard` | JWT | Own aggregated stats and charts (regular user) or system-wide stats (admin) |
| GET | `/api/tasks/{id}` | JWT | Single task |
| POST | `/api/tasks` | JWT | Create task (default status: ToDo) |
| PUT | `/api/tasks/{id}` | JWT | Update task |
| DELETE | `/api/tasks/{id}` | JWT | Delete task |

### Seed (`/api/seed`)

| Method | Route | Auth | Notes |
|---|---|---|---|
| POST | `/api/seed/me` | JWT | Delete all user tasks and seed 13 randomised demo tasks |

---

## Architecture Decisions

### MongoDB over Entity Framework
The raw `MongoDB.Driver` is used directly, demonstrating native document database usage without ORM abstraction layers that would hide the query structure.

### Clean Architecture Layer Separation
Each layer has a single, well-defined responsibility. As a general rule, dependencies flow inward — outer layers depend on inner layers. The one intentional exception is the Domain layer's use of `MongoDB.Driver` for BSON attribute mapping on entities; this is a documented pragmatic decision and is covered in detail under MongoDB.Driver in Domain below.

### Repository Pattern
`ITaskRepository` and `IUserRepository` abstract the MongoDB persistence layer from the Application layer, enabling unit testing without database mocks.

### Service Interfaces and Dependency Inversion
Controllers depend on `ITaskService` and `IUserService`, never on concrete classes. This enforces the Dependency Inversion Principle and makes each layer independently testable.

### JWT Authentication
Stateless authentication using short-lived JWT Bearer tokens. Token claims carry `userId`, `username`, and `role` to avoid round-trips to the database on every request.

### IJwtTokenGenerator / IPasswordHasher / ICurrentUserService Abstractions
These infrastructure concerns are hidden behind Application-layer interfaces. `UserService` never references BCrypt, JWT libraries, or `HttpContext` directly — it works only through abstractions.

### CurrentUserService in Infrastructure (not API)
Accessing `HttpContext` and reading claims is an infrastructure concern. Keeping it in Infrastructure prevents the Application layer from depending on ASP.NET Core.

### UserRole Enum over Magic Strings
`UserRole.Admin` and `UserRole.User` everywhere. No hardcoded `"Admin"` strings that could silently break.

### Admin Access Pattern
The controller layer is responsible for determining whether the current caller has administrative privileges, using the role embedded in the JWT. Service operations use this to scope data access accordingly: admin callers receive and can operate on all records in the system, while regular callers are scoped to their own data. The service layer enforces ownership rules and data scoping; the controller communicates the caller's privilege level without the service layer being coupled to the HTTP context.

### MongoDB.Driver in Domain (Pragmatic Trade-off)
The Domain layer references `MongoDB.Driver` for BSON attribute mapping on `TaskItem` and `User` entities (`[BsonId]`, `[BsonElement]`, `[BsonRepresentation]`). A fully dependency-free domain would require a separate persistence model and a mapping layer. Given the scope of this exercise, the pragmatic trade-off is to keep typed entity mapping in Domain at the cost of one external dependency.

### Manual Validation in Application Services
Business rules live in `TaskService` and `UserService` as explicit, readable code. No framework annotations or FluentValidation pipelines — the rules are immediately visible during code review.

### Strongly Typed DTOs Throughout
Every API boundary uses typed DTOs. No `dynamic`, no `object`, no `Dictionary<string, object>`. Controllers always return `TaskResponseDto`, `UserResponseDto`, or `LoginResponseDto`.

### Account Lockout Persisted in MongoDB
`failedLoginAttempts`, `isLocked`, and `lockedAt` live on the `User` document. Lockout state survives application restarts, which would not be the case with in-memory state.

### Rate Limiting with .NET 8 Built-in Middleware
`System.Threading.RateLimiting` — no third-party packages. Two Fixed Window policies: 10 req/min on `/api/auth/*`, 60 req/min default.

### Integration Tests over MongoDB Mocks
Repository integration tests use a real `taskapp_test` database with `IAsyncLifetime` for setup/teardown. Mocking `IMongoCollection<T>` would hide actual query behavior and index issues.

### Tailwind CSS + Radix UI for Frontend
Tailwind v4 provides utility-first styling; Radix UI primitives (via shadcn component conventions) provide accessible, unstyled base components. No heavy component framework dependency.

### React + TypeScript with Strict Mode
`strict: true` in `tsconfig.app.json`. No `any` types. All API contracts typed in `src/types/index.ts` and consumed consistently across services, hooks, and components.

### TDD Approach
Unit tests were written first (failing), then service implementations were written to make them pass. 24 unit tests cover the core business rules implemented in the service layer.

---

## Type Safety Strategy

### Backend (.NET)
- No `dynamic`, no anonymous API response objects, no `Dictionary<string, object>`
- All repositories typed as `IMongoCollection<TaskItem>` and `IMongoCollection<User>`
- All BSON mapping via typed attributes on entities
- Controllers always return explicit generic types: `Task<ActionResult<TaskResponseDto>>`

### Frontend (TypeScript)
- `strict: true` — zero `any` types
- All API shapes defined in `src/types/index.ts`
- Axios calls typed as `AxiosResponse<T>`
- React Context, custom hooks, and component props all fully typed

**Goal**: every layer communicates through well-defined typed contracts, providing compile-time type safety end to end.

---

## Security Features

### Session Expiry
Any 401 response from an authenticated endpoint triggers an immediate client-side logout and toast notification — no stale sessions linger in the UI.

### Account Lockout
- After 3 consecutive failed logins, the account is permanently locked
- State stored in MongoDB (`isLocked`, `failedLoginAttempts`, `lockedAt`)
- `POST /api/auth/lock/{userId}` — Admin can manually lock any active account
- `POST /api/auth/unlock/{userId}` — Admin can unlock any locked account; resets failed-attempt counter
- HTTP 403 returned on locked account login attempt

### Rate Limiting
- `/api/auth/*` — 10 requests per minute per IP
- All other endpoints — 60 requests per minute per IP
- HTTP 429 with JSON body: `{"error": "Too many requests. Please wait before trying again."}`

---

## Notes on AI-Assisted Development

Claude Code (Anthropic) was used as an AI coding assistant throughout development. All generated output was reviewed, validated, and modified where necessary by the author. Final architecture decisions, acceptance of generated code, and overall design direction remained under human authorship and supervision. Validation steps performed by the author included:
- Reviewing all generated code for adherence to Clean Architecture boundaries
- Running and verifying all 24 unit tests with `dotnet test`
- Running TypeScript type-check with `tsc --noEmit`
- Verifying compliance with hard constraints (no EF, no FluentValidation, no MongoDB mocks, no magic strings, etc.)
