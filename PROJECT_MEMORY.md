# PROJECT_MEMORY.md

_Last updated: 2026-06-08 (users page + lock/unlock + owner column + docs audit)_

---

## Project Structure

```
c:\Pro\BallastLane\
├── TaskApp.sln                          (MinimumVisualStudioVersion=17 — VS2022 required)
├── README.md
├── PROJECT_MEMORY.md
├── CHANGELOG.md
│
├── TaskApp.Domain\
│   ├── Entities\
│   │   ├── TaskItem.cs                  (Labels, UpdatedBy, CompletedAt added)
│   │   └── User.cs                      (FirstName, LastName added)
│   ├── Enums\
│   │   ├── TaskStatus.cs                (Pending=0, InProgress=1, Completed=2, ToDo=3)
│   │   └── UserRole.cs                  (User, Admin)
│   ├── Exceptions\
│   │   └── DomainException.cs
│   └── Interfaces\Repositories\
│       ├── ITaskRepository.cs           (GetAllAsync, GetAllLabelsAsync, GetAllByUserIdAsync, GetAllLabelsByUserIdAsync)
│       └── IUserRepository.cs           (+ GetAllAsync)
│
├── TaskApp.Application\
│   ├── DTOs\Auth\
│   │   ├── LoginRequestDto.cs
│   │   ├── LoginResponseDto.cs
│   │   ├── RegisterRequestDto.cs        (+ FirstName, LastName)
│   │   ├── UpdateProfileDto.cs          (new: FirstName, LastName, Username, Email, optional passwords)
│   │   └── UserResponseDto.cs           (+ FirstName, LastName, FullName computed)
│   ├── DTOs\Tasks\
│   │   ├── CreateTaskDto.cs             (+ Labels)
│   │   ├── DashboardStatsDto.cs         (new: DashboardStatsDto + CompletionTimingDto)
│   │   ├── TaskResponseDto.cs           (+ Labels, UpdatedBy, CompletedAt, OwnerUsername)
│   │   └── UpdateTaskDto.cs             (+ Labels)
│   ├── DTOs\Seed\
│   │   └── DemoSeedResultDto.cs         (new: Message, TasksDeleted, TasksCreated)
│   ├── Interfaces\
│   │   ├── ICurrentUserService.cs
│   │   ├── IDemoSeedService.cs          (new: SeedForUserAsync)
│   │   ├── IJwtTokenGenerator.cs
│   │   ├── IPasswordHasher.cs
│   │   ├── ITaskService.cs              (all methods have bool isAdmin = false; admin bypasses ownership checks)
│   │   └── IUserService.cs              (+ GetAllUsersAsync, LockAsync, UpdateProfileAsync)
│   └── Services\
│       ├── DemoSeedService.cs           (new: 13 realistic tasks across all statuses/SP values)
│       ├── TaskService.cs
│       └── UserService.cs
│
├── TaskApp.Infrastructure\
│   ├── Auth\
│   │   ├── CurrentUserService.cs        (uses FindFirst()?.Value — not FindFirstValue())
│   │   ├── JwtTokenGenerator.cs
│   │   └── PasswordHasher.cs
│   ├── Persistence\
│   │   ├── MongoDbContext.cs
│   │   └── Repositories\
│   │       ├── TaskRepository.cs        (+ GetAllAsync, GetAllLabelsAsync, GetAllByUserIdAsync, GetAllLabelsByUserIdAsync)
│   │       └── UserRepository.cs
│   └── Seeder\
│       └── DatabaseSeeder.cs            (seed users have FirstName/LastName)
│
├── TaskApp.API\
│   ├── Controllers\
│   │   ├── AuthController.cs            (+ PUT /api/auth/profile, GET /api/auth/users, POST /api/auth/lock/{userId})
│   │   ├── SeedController.cs            (new: POST /api/seed/me — requires JWT)
│   │   └── TasksController.cs           (IsAdmin property; passes isAdmin to all service calls)
│   ├── Middleware\
│   │   └── RateLimitingSetup.cs
│   ├── Program.cs                       (JsonStringEnumConverter registered globally)
│   └── appsettings.json
│
├── TaskApp.Tests\
│   ├── Unit\Services\
│   │   ├── TaskServiceTests.cs          (14 tests)
│   │   └── UserServiceTests.cs          (10 tests — updated for FirstName/LastName)
│   └── Integration\
│       ├── ApiTestFactory.cs
│       ├── AuthControllerTests.cs
│       ├── TaskRepositoryTests.cs
│       ├── TasksControllerTests.cs
│       ├── RateLimiterTests.cs
│       └── UserRepositoryTests.cs
│
└── taskapp-frontend\
    └── src\
        ├── components\
        │   ├── ui\
        │   │   ├── alert.tsx
        │   │   ├── badge.tsx            (variants: default, outline, success, warning, sky, todo)
        │   │   ├── button.tsx           (cursor-pointer in base class)
        │   │   ├── dialog.tsx
        │   │   ├── input.tsx
        │   │   ├── label.tsx
        │   │   ├── select.tsx
        │   │   ├── spinner.tsx
        │   │   └── table.tsx
        │   ├── AdminRoute.tsx           (admin-only guard: redirects non-admin to /dashboard)
        │   ├── AppNav.tsx               (sticky header: logo left, UserMenu right — no nav links)
        │   ├── BottomNav.tsx            (mobile only: fixed bottom tab bar — Tasks + Dashboard)
        │   ├── DeleteConfirmDialog.tsx
        │   ├── EditProfileDialog.tsx    (first/last name, username, email, optional password)
        │   ├── GlobalSnackbar.tsx
        │   ├── LabelCombobox.tsx        (Jira-style tag input with create-on-type)
        │   ├── Paginator.tsx            (rows-per-page select 10/20/50 + first/prev/next/last)
        │   ├── ProtectedRoute.tsx
        │   ├── Sidebar.tsx              (desktop only: w-56 left sidebar — Dashboard, Tasks, Users (admin) links; active highlight)
        │   ├── TaskFormDialog.tsx       (modal for create+edit, includes labels)
        │   └── UserMenu.tsx             (person icon dropdown: Edit Profile, Generate Demo Data, Logout)
        ├── context\
        │   └── AuthContext.tsx          (sessionStorage persistence, updateUser method)
        ├── hooks\
        │   └── useNotification.ts
        ├── pages\
        │   ├── DashboardPage.tsx        (stat cards, PieChart, dumbbell timing, weekly velocity BarChart, estimation accuracy BarChart)
        │   ├── LoginPage.tsx            (card layout, logo, redirects to /dashboard on success)
        │   ├── RegisterPage.tsx         (first/last name fields, card layout)
        │   ├── TaskFormPage.tsx         (kept as file, no longer routed — safe to delete)
        │   ├── TasksPage.tsx            (inline status+date editing, label+status+owner filter, paginator, SP badges, mobile cards)
        │   └── UsersPage.tsx            (admin only: list users with role/status badges, lock/unlock actions)
        ├── services\
        │   └── api.ts                   (baseURL: https://localhost:7020)
        ├── types\
        │   └── index.ts
        ├── App.tsx
        └── main.tsx
```

---

## API Endpoints

### Auth API (`/api/auth`) — Rate limited: 10 req/min/IP

| Method | Route | Auth | Returns | Notes |
|---|---|---|---|---|
| GET | /api/auth/ping | Public | `{status:"ok"}` | Health check |
| POST | /api/auth/register | Public | `UserResponseDto` 201 | Requires FirstName, LastName |
| POST | /api/auth/login | Public | `LoginResponseDto` 200 | 403 if locked |
| GET | /api/auth/me | JWT | `UserResponseDto` | |
| PUT | /api/auth/profile | JWT | `UserResponseDto` | Optional password change |
| GET | /api/auth/users | Admin JWT | `UserResponseDto[]` | List all registered users |
| POST | /api/auth/lock/{userId} | Admin JWT | `UserResponseDto` | Lock an active account |
| POST | /api/auth/unlock/{userId} | Admin JWT | `UserResponseDto` | Unlock a locked account; resets failed attempts |

### Tasks API (`/api/tasks`) — Rate limited: 60 req/min/IP

| Method | Route | Auth | Returns | Notes |
|---|---|---|---|---|
| GET | /api/tasks | JWT | `TaskResponseDto[]` | Own tasks (regular user); all tasks with `OwnerUsername` (admin) |
| GET | /api/tasks/labels | JWT | `string[]` | Must be declared before `{id}` route; admin receives all labels |
| GET | /api/tasks/dashboard | JWT | `DashboardStatsDto` | Own stats (regular user); system-wide stats (admin) |
| GET | /api/tasks/{id} | JWT | `TaskResponseDto` | |
| POST | /api/tasks | JWT | `TaskResponseDto` 201 | Default status: ToDo |
| PUT | /api/tasks/{id} | JWT | `TaskResponseDto` | |
| DELETE | /api/tasks/{id} | JWT | 204 | |

### Seed API (`/api/seed`) — JWT required

| Method | Route | Auth | Returns | Notes |
|---|---|---|---|---|
| POST | /api/seed/me | JWT | `DemoSeedResultDto` | Deletes all user tasks, creates 13 demo tasks covering all statuses/SPs |

---

## MongoDB Collections

### `tasks`
| Field | Type | Notes |
|---|---|---|
| `_id` | ObjectId | → `Id` string |
| `title` | string | Required, max 200 |
| `description` | string | |
| `status` | int enum | Pending=0, InProgress=1, Completed=2, ToDo=3 |
| `due_date` | DateTime | Cannot be past on creation |
| `labels` | string[] | Trimmed, deduplicated in service layer |
| `userId` | string | References users._id |
| `createdAt` | DateTime | Set once |
| `updatedAt` | DateTime | Updated on every update |
| `updatedBy` | string | UserId of last modifier (set in UpdateAsync) |
| `storyPoints` | int? | Backend validates 1–100; UI exposes Fibonacci picker (1, 2, 3, 5, 8) only |
| `completedAt` | DateTime? | Set when status → Completed; cleared when status leaves Completed |

### `users`
| Field | Type | Notes |
|---|---|---|
| `_id` | ObjectId | → `Id` string |
| `firstName` | string | Required on register |
| `lastName` | string | Required on register |
| `username` | string | Unique |
| `email` | string | Unique |
| `passwordHash` | string | BCrypt |
| `role` | int enum | User=0, Admin=1 |
| `createdAt` | DateTime | |
| `failedLoginAttempts` | int | Default 0 |
| `isLocked` | bool | Default false |
| `lockedAt` | DateTime? | Null when unlocked |

---

## Service Method Signatures

### `ITaskService`
```csharp
Task<IEnumerable<TaskResponseDto>> GetAllAsync(string userId, bool isAdmin = false);
Task<IEnumerable<string>> GetAllLabelsAsync(string userId, bool isAdmin = false);
Task<DashboardStatsDto> GetDashboardStatsAsync(string userId, bool isAdmin = false);
Task<TaskResponseDto> GetByIdAsync(string id, string userId, bool isAdmin = false);
Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, string userId);
Task<TaskResponseDto> UpdateAsync(string id, UpdateTaskDto dto, string userId, bool isAdmin = false);
Task DeleteAsync(string id, string userId, bool isAdmin = false);
```

### `IDemoSeedService`
```csharp
Task<DemoSeedResultDto> SeedForUserAsync(string userId);
```

### `IUserService`
```csharp
Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
Task<UserResponseDto> RegisterAsync(RegisterRequestDto dto);
Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
Task<UserResponseDto> GetByIdAsync(string id);
Task<UserResponseDto> LockAsync(string userId);
Task<UserResponseDto> UnlockAsync(string userId);
Task<UserResponseDto> UpdateProfileAsync(string userId, UpdateProfileDto dto);
```

---

## Key Implementation Notes

- **Enum serialization**: `JsonStringEnumConverter` registered globally in `Program.cs` — enums serialize as strings in JSON (`"InProgress"` not `1`). MongoDB stores them as integers internally.
- **TaskStatus default**: new tasks born as `ToDo` (value 3). Existing data (`Pending=0`) is unaffected.
- **JWT storage**: `sessionStorage` (not localStorage). Survives F5 reload, cleared when tab closes.
- **CurrentUserService**: uses `FindFirst()?.Value` — `FindFirstValue()` extension not available in class libraries.
- **Labels route ordering**: `GET /api/tasks/labels` must be declared before `GET /api/tasks/{id}` in the controller. Same for `GET /api/tasks/dashboard`.
- **Inline editing**: status and due date are editable directly in the table row; saves optimistically via `taskService.update`.
- **Story points**: nullable int on `TaskItem`; validated 1–100 in `TaskService.ValidateStoryPoints`; UI shows Fibonacci picker (1/2/3/5/8) in the form dialog and a badge in the task list.
- **CompletedAt tracking**: set automatically when `status → Completed` in `UpdateAsync`; cleared when status moves away from Completed.
- **401 auto-logout**: axios response interceptor dispatches `app:session-expired` custom event on 401 from any non-public endpoint. `AppRoutes` listens and calls `logout()`. A 5-second debounce prevents duplicate events from parallel requests.
- **Dashboard dumbbell chart**: `ComposedChart layout="vertical"` with a floating `Bar dataKey="range"` (array of two timestamps) as the connector, plus two `Line strokeWidth={0}` with custom dots — one for due date (blue), one for completed date (green = early, red = late).
- **Weekly velocity**: groups completed tasks by `GetWeekStart(completedAt)` (Monday of that week), sums story points and task count per bucket.
- **Estimation accuracy**: groups completed tasks by SP value, computes average days from `CreatedAt` to `CompletedAt` per group.
- **Navigation layout**: sticky `AppNav` header (logo + UserMenu only) + `Sidebar` (desktop only, `w-56`, `hidden md:flex`) + `BottomNav` (mobile only, `fixed bottom-0 md:hidden`). Layout in `App.tsx` uses a `flex` row below the header.
- **Admin data access**: `ITaskService` methods accept `bool isAdmin = false`. When `true`, repository calls use `GetAllAsync()` / `GetAllLabelsAsync()` (no userId filter) and ownership checks are skipped. `TasksController` reads `ICurrentUserService.Role` and passes `isAdmin` to every service call. Default `false` means unit tests require no changes.

---

## NuGet Packages

| Package | Project | Version |
|---|---|---|
| `MongoDB.Driver` | Domain, Infrastructure, Tests | 3.x |
| `BCrypt.Net-Next` | Infrastructure | 4.x |
| `System.IdentityModel.Tokens.Jwt` | Infrastructure | 8.x |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | API | 8.0.0 (pinned) |
| `Swashbuckle.AspNetCore` | API | latest |
| `Microsoft.AspNetCore.Http.Abstractions` | Infrastructure | 2.x |
| `Microsoft.AspNetCore.OpenApi` | API | 8.x |
| `Moq` | Tests | 4.x |
| `xunit` | Tests | 2.x |
| `Microsoft.AspNetCore.Mvc.Testing` | Tests | 8.x |

## npm Packages (frontend)

| Package | Purpose |
|---|---|
| `react` + `react-dom` | UI framework (v19) |
| `react-router-dom` | Routing (v7) |
| `axios` | HTTP client |
| `@radix-ui/react-dialog` | Modal primitive |
| `@radix-ui/react-select` | Select primitive |
| `@radix-ui/react-label` | Label primitive |
| `@radix-ui/react-slot` | Slot primitive (Button asChild) |
| `@radix-ui/react-alert-dialog` | Alert dialog |
| `class-variance-authority` | Component variants (cva) |
| `clsx` + `tailwind-merge` | Class merging (cn utility) |
| `lucide-react` | Icons |
| `sonner` | Toast notifications |
| `recharts` | Charts: PieChart (status distribution), ComposedChart (dumbbell timing), BarChart (velocity + estimation accuracy) |
| `tailwindcss` v4 + `@tailwindcss/vite` | Styling |

---

## Seed Credentials

| Name | Username | Email | Password | Role |
|---|---|---|---|---|
| Demo User | `demo` | `demo@taskapp.com` | `Demo1234!` | User |
| Admin User | `admin` | `admin@taskapp.com` | `Admin1234!` | Admin |

5 sample tasks assigned to `demo` on first startup (Completed ×2, InProgress ×1, Pending ×2). These static tasks do **not** have `completedAt` or `storyPoints` set, so the dashboard charts will be sparse until demo data is generated.

Use `POST /api/seed/me` (or the "Generate Demo Data" button) to replace tasks with 13 fully populated demo tasks that include all statuses, SP values, labels, and realistic dates for all charts.

---

## Test Status

- **Unit tests**: 24 passing (TaskServiceTests: 14, UserServiceTests: 10)
- **Integration tests**: require live MongoDB on `localhost:27017`, use `taskapp_test` DB
- **TypeScript**: strict mode, 0 errors

---

## Known Issues / Pending Decisions

- `TaskFormPage.tsx` still exists as a file but is no longer wired into the router (task editing moved to `TaskFormDialog` modal). Can be deleted if desired.
- Integration tests have not been updated to cover: labels, profile update, `GET /api/tasks/labels`, `GET /api/tasks/dashboard`, `POST /api/seed/me`, story points, `CompletedAt` tracking.
- `DatabaseSeeder.cs` static tasks don't have `completedAt` or `storyPoints` set. Not worth fixing — the dynamic seed endpoint (`POST /api/seed/me`) supersedes it for demo purposes.

