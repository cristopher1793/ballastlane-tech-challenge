# CHANGELOG

---

## [2026-06-08] — Users page with lock and unlock (admin)

### What was built
- `GET /api/auth/users` endpoint (Admin JWT) — returns all registered users ordered by username
- `POST /api/auth/lock/{userId}` endpoint (Admin JWT) — locks an active account (sets `IsLocked=true`, `LockedAt=DateTime.UtcNow`)
- Users page (`/users` route, admin-only via `AdminRoute`) — table showing all users with full name, username, email, role badge (purple=Admin, sky=User), status badge (green=Active, red=Locked), failed-attempt count, and inline Lock/Unlock actions
- Active accounts show a red-tinted Lock button; locked accounts show an Unlock button; own account shows "You" (non-actionable, prevents self-lock)
- Locked count shown under the page title when > 0
- Admin-only "Users" link added to the left sidebar

### Files created
- `taskapp-frontend/src/pages/UsersPage.tsx`
- `taskapp-frontend/src/components/AdminRoute.tsx`

### Files modified
- `TaskApp.Application/Interfaces/IUserService.cs` — added `GetAllUsersAsync`, `LockAsync`
- `TaskApp.Application/Services/UserService.cs` — implemented both methods
- `TaskApp.API/Controllers/AuthController.cs` — added `GET /api/auth/users` and `POST /api/auth/lock/{userId}` (both `[Authorize(Roles = "Admin")]`)
- `taskapp-frontend/src/services/api.ts` — added `authService.getAllUsers()`, `lockUser()`, `unlockUser()`
- `taskapp-frontend/src/components/Sidebar.tsx` — added admin-only "Users" link (shown only when `user?.role === 'Admin'`)
- `taskapp-frontend/src/App.tsx` — added `/users` route wrapped in `AdminRoute`

### Tests added
None — admin-only page; covered by manual verification.

### Deviations
- `LockAsync` intentionally does not reset `FailedLoginAttempts` — admin-imposed lock is distinct from the automatic lockout. `UnlockAsync` (existing) does reset it to 0.

---

## [2026-06-08] — Owner column and user filter in admin task list

### What was built
- `TaskResponseDto` now includes `OwnerUsername` (null on all regular user responses; populated when admin requests tasks)
- `TaskService.GetAllAsync` (admin path) batch-loads all users via `IUserRepository.GetAllAsync()`, builds a `userId → username` dictionary, and enriches each DTO
- Admin task list shows an "Owner" column in the desktop table and "Owner: username" on mobile cards
- Owner filter dropdown above the table lets admins narrow to a specific user's tasks
- Admin view is detected automatically on the frontend: Owner column and filter appear only when `ownerUsername` is present on any returned task

### Files modified
- `TaskApp.Application/DTOs/Tasks/TaskResponseDto.cs` — added `OwnerUsername: string?`
- `TaskApp.Application/Services/TaskService.cs` — admin path enriches DTOs with owner username
- `TaskApp.Domain/Interfaces/Repositories/IUserRepository.cs` — added `GetAllAsync()` (no filter)
- `TaskApp.Infrastructure/Persistence/Repositories/UserRepository.cs` — implemented `GetAllAsync()` with `Filter.Empty`
- `TaskApp.Tests/Unit/Services/TaskServiceTests.cs` — updated constructor to pass `Mock<IUserRepository>` (required by updated `TaskService` constructor)
- `taskapp-frontend/src/types/index.ts` — added `ownerUsername: string | null` to `TaskResponseDto`
- `taskapp-frontend/src/pages/TasksPage.tsx` — owner filter dropdown + owner column (desktop table + mobile cards)

### Tests added
None — owner enrichment logic covered by integration testing; existing unit tests updated (constructor fix only).

### Deviations
None.

---

## [2026-06-08] — Admin sees all tasks

### What was built
- Admin users now see **all** tasks in the system (tasks list, labels, dashboard) rather than only their own.
- Admin can also view, update, and delete any task regardless of ownership.
- Regular users are unchanged — they see and can only modify their own tasks.

### Files modified
- `TaskApp.Domain/Interfaces/Repositories/ITaskRepository.cs` — added `GetAllAsync()` and `GetAllLabelsAsync()` (no userId filter)
- `TaskApp.Infrastructure/Persistence/Repositories/TaskRepository.cs` — implemented both with `Filter.Empty`
- `TaskApp.Application/Interfaces/ITaskService.cs` — added `bool isAdmin = false` default parameter to all read/write methods
- `TaskApp.Application/Services/TaskService.cs` — branches on `isAdmin` to call the correct repository method; ownership checks skipped when `isAdmin = true`
- `TaskApp.API/Controllers/TasksController.cs` — private `IsAdmin` property reads `_currentUserService.Role`; passed to every service call

### Tests added
None — existing unit tests unaffected (default `isAdmin = false` keeps all call sites valid).

### Deviations
None.

---

## [2026-06-08] — Left sidebar navigation (desktop)

### What was built
- Replaced the nav buttons in the top header with a persistent left sidebar on desktop (≥ md breakpoint). Header is now clean — just logo on the left and UserMenu on the right.
- `Sidebar.tsx` — `w-56` aside with Dashboard + Tasks links; active item gets a muted background highlight. Hidden on mobile (bottom nav still handles mobile).
- Layout in `App.tsx` updated: authenticated view uses a `flex` row with `<Sidebar />` + `<main className="flex-1 min-w-0">`.
- AppNav made `sticky top-0 z-40` so it stays visible when content scrolls.

### Files created
- `taskapp-frontend/src/components/Sidebar.tsx`

### Files modified
- `taskapp-frontend/src/components/AppNav.tsx` — removed Tasks/Dashboard buttons, removed unused `cn`/`useLocation` imports
- `taskapp-frontend/src/App.tsx` — new flex layout wrapping Sidebar + main

### Tests added
None.

### Deviations
None.

---

## [2026-06-08] — Demo data seed endpoint

### What was built
- `POST /api/seed/me` — authenticated endpoint that deletes all tasks for the current user and seeds 13 realistic demo tasks (Completed×5, InProgress×2, Pending×3, ToDo×3). Tasks use all Fibonacci SP values (1,2,3,5,8), various labels (backend, frontend, bug, feature, testing, devops), and have completed dates spread across 4 weeks so the velocity and timing charts populate immediately.
- "Generate Demo Data" button added to the UserMenu dropdown (between Edit Profile and Logout). Shows a browser confirm dialog before calling the API, then reloads the page to refresh all data.

### Files created
- `TaskApp.Application/DTOs/Seed/DemoSeedResultDto.cs`
- `TaskApp.Application/Interfaces/IDemoSeedService.cs`
- `TaskApp.Application/Services/DemoSeedService.cs`
- `TaskApp.API/Controllers/SeedController.cs`

### Files modified
- `TaskApp.API/Program.cs` — registered `IDemoSeedService → DemoSeedService`
- `taskapp-frontend/src/services/api.ts` — added `seedService.seedMe()`
- `taskapp-frontend/src/components/UserMenu.tsx` — added "Generate Demo Data" button with Wand2 icon
- `PROJECT_MEMORY.md` — updated structure, endpoints, service signatures

### Tests added
None — this is a demo/testing utility; unit tests would add limited value.

### Deviations
- `DemoSeedService` subsequently updated to use `Random` instead of hardcoded dates: task titles are shuffled from a pool of 25, SP values and labels are picked randomly per task, completed-at dates are jittered ±1 day within their week bucket, and due-date variance is randomised −4 to +4 days. Every call produces a distinct dataset.

---

## [2026-06-07] — Initial fullstack implementation

### What was built
Complete TaskApp fullstack application for technical interview exercise:
- Clean Architecture .NET 8 backend with MongoDB, JWT auth, and rate limiting
- React + TypeScript + Vite + Tailwind CSS + Radix UI frontend
- 24 unit tests (TDD: failing tests written before implementations)
- Repository integration tests (real MongoDB `taskapp_test` database)
- API integration tests (WebApplicationFactory)

### Files created

**Solution scaffold:**
- `TaskApp.sln`
- `TaskApp.Domain/TaskApp.Domain.csproj`
- `TaskApp.Application/TaskApp.Application.csproj`
- `TaskApp.Infrastructure/TaskApp.Infrastructure.csproj`
- `TaskApp.API/TaskApp.API.csproj`
- `TaskApp.Tests/TaskApp.Tests.csproj`

**Domain layer:**
- `TaskApp.Domain/Entities/TaskItem.cs`
- `TaskApp.Domain/Entities/User.cs`
- `TaskApp.Domain/Enums/TaskStatus.cs`
- `TaskApp.Domain/Enums/UserRole.cs`
- `TaskApp.Domain/Exceptions/DomainException.cs`
- `TaskApp.Domain/Interfaces/Repositories/ITaskRepository.cs`
- `TaskApp.Domain/Interfaces/Repositories/IUserRepository.cs`

**Application layer:**
- `TaskApp.Application/DTOs/Auth/LoginRequestDto.cs`
- `TaskApp.Application/DTOs/Auth/LoginResponseDto.cs`
- `TaskApp.Application/DTOs/Auth/RegisterRequestDto.cs`
- `TaskApp.Application/DTOs/Auth/UserResponseDto.cs`
- `TaskApp.Application/DTOs/Tasks/CreateTaskDto.cs`
- `TaskApp.Application/DTOs/Tasks/UpdateTaskDto.cs`
- `TaskApp.Application/DTOs/Tasks/TaskResponseDto.cs`
- `TaskApp.Application/Interfaces/ITaskService.cs`
- `TaskApp.Application/Interfaces/IUserService.cs`
- `TaskApp.Application/Interfaces/IJwtTokenGenerator.cs`
- `TaskApp.Application/Interfaces/IPasswordHasher.cs`
- `TaskApp.Application/Interfaces/ICurrentUserService.cs`
- `TaskApp.Application/Services/TaskService.cs`
- `TaskApp.Application/Services/UserService.cs`

**Infrastructure layer:**
- `TaskApp.Infrastructure/Persistence/MongoDbContext.cs`
- `TaskApp.Infrastructure/Persistence/Repositories/TaskRepository.cs`
- `TaskApp.Infrastructure/Persistence/Repositories/UserRepository.cs`
- `TaskApp.Infrastructure/Auth/JwtTokenGenerator.cs`
- `TaskApp.Infrastructure/Auth/PasswordHasher.cs`
- `TaskApp.Infrastructure/Auth/CurrentUserService.cs`
- `TaskApp.Infrastructure/Seeder/DatabaseSeeder.cs`

**API layer:**
- `TaskApp.API/Controllers/AuthController.cs`
- `TaskApp.API/Controllers/TasksController.cs`
- `TaskApp.API/Middleware/RateLimitingSetup.cs`
- `TaskApp.API/Program.cs`
- `TaskApp.API/appsettings.json`

**Tests:**
- `TaskApp.Tests/Unit/Services/TaskServiceTests.cs` (14 tests)
- `TaskApp.Tests/Unit/Services/UserServiceTests.cs` (10 tests)
- `TaskApp.Tests/Integration/ApiTestFactory.cs`
- `TaskApp.Tests/Integration/AuthControllerTests.cs`
- `TaskApp.Tests/Integration/TasksControllerTests.cs`
- `TaskApp.Tests/Integration/RateLimiterTests.cs`
- `TaskApp.Tests/Integration/TaskRepositoryTests.cs`
- `TaskApp.Tests/Integration/UserRepositoryTests.cs`

**Frontend:**
- `taskapp-frontend/` (Vite + React + TypeScript scaffold)
- `taskapp-frontend/src/types/index.ts`
- `taskapp-frontend/src/services/api.ts`
- `taskapp-frontend/src/context/AuthContext.tsx`
- `taskapp-frontend/src/hooks/useNotification.ts`
- `taskapp-frontend/src/components/AppNav.tsx`
- `taskapp-frontend/src/components/DeleteConfirmDialog.tsx`
- `taskapp-frontend/src/components/GlobalSnackbar.tsx`
- `taskapp-frontend/src/components/ProtectedRoute.tsx`
- `taskapp-frontend/src/pages/LoginPage.tsx`
- `taskapp-frontend/src/pages/RegisterPage.tsx`
- `taskapp-frontend/src/pages/TasksPage.tsx`
- `taskapp-frontend/src/pages/TaskFormPage.tsx`
- `taskapp-frontend/src/App.tsx`

**Docs:**
- `README.md`
- `PROJECT_MEMORY.md`
- `CHANGELOG.md`

### Tests added
- 14 `TaskService` unit tests
- 10 `UserService` unit tests
- 6 `AuthController` integration tests
- 5 `TasksController` integration tests
- 3 `RateLimiter` integration tests
- 6 `TaskRepository` integration tests
- 8 `UserRepository` integration tests

### Deviations from plan
- `MongoDB.Driver` added to `TaskApp.Domain` project (required for BSON attributes on entities). This is a pragmatic trade-off — the spec explicitly requires typed entity mapping with BSON attributes, which necessitates the driver in Domain.
- `TaskStatus` enum renamed references use fully-qualified `TaskApp.Domain.Enums.TaskStatus` throughout to avoid ambiguity with `System.Threading.Tasks.TaskStatus` from global usings.
- `Microsoft.AspNetCore.Authentication.JwtBearer` pinned to v8.0.0 (latest v10 is net10 only).
- `CurrentUserService` uses `FindFirst()?.Value` (native `ClaimsPrincipal` method) instead of `FindFirstValue()` extension which requires `Microsoft.AspNetCore.Authentication` not available in class libraries.

---

## [2026-06-08] — Frontend UX polish: smart nav, cursor pointer, secondary button

### What was changed
- AppNav hides the Login button when on `/login` route, hides Register when on `/register` route (uses `useLocation`)
- Nav secondary action styled as white-on-navy pill (`bg-white text-primary`) to contrast with ghost primary links
- All `Button` components globally have `cursor-pointer` via `buttonVariants` base class
- `RouterLink` elements in LoginPage and RegisterPage have `cursor-pointer` class added

### Files modified
- `taskapp-frontend/src/components/AppNav.tsx`
- `taskapp-frontend/src/components/ui/button.tsx`
- `taskapp-frontend/src/pages/LoginPage.tsx`
- `taskapp-frontend/src/pages/RegisterPage.tsx`

---

## [2026-06-08] — User profile: first/last name, edit profile, session persistence

### What was built
- `User` entity: added `FirstName`, `LastName` BSON fields
- `RegisterRequestDto`: added `FirstName`, `LastName`
- `UpdateProfileDto`: new DTO for profile update (name, username, email, optional password change)
- `UserResponseDto`: added `FirstName`, `LastName`, computed `FullName`
- `IUserService` / `UserService`: added `UpdateProfileAsync` with uniqueness checks and optional password change
- `AuthController`: new `PUT /api/auth/profile` endpoint (Authorize)
- `DatabaseSeeder`: seed users now have first/last names
- Frontend types, api service, AuthContext (`updateUser`), RegisterPage (first/last name fields)
- `EditProfileDialog` component: full profile edit with optional password change section
- `UserMenu`: shows full name, wires up EditProfileDialog
- Session persistence via `sessionStorage` (survives reload, clears on tab close)
- Unit tests updated for new `RegisterRequestDto` fields

### Files modified
- `TaskApp.Domain/Entities/User.cs`
- `TaskApp.Application/DTOs/Auth/RegisterRequestDto.cs`
- `TaskApp.Application/DTOs/Auth/UserResponseDto.cs`
- `TaskApp.Application/DTOs/Auth/UpdateProfileDto.cs` (new)
- `TaskApp.Application/Interfaces/IUserService.cs`
- `TaskApp.Application/Services/UserService.cs`
- `TaskApp.API/Controllers/AuthController.cs`
- `TaskApp.Infrastructure/Seeder/DatabaseSeeder.cs`
- `TaskApp.Tests/Unit/Services/UserServiceTests.cs`
- `taskapp-frontend/src/types/index.ts`
- `taskapp-frontend/src/services/api.ts`
- `taskapp-frontend/src/context/AuthContext.tsx`
- `taskapp-frontend/src/pages/RegisterPage.tsx`
- `taskapp-frontend/src/components/AppNav.tsx`
- `taskapp-frontend/src/components/UserMenu.tsx`
- `taskapp-frontend/src/components/EditProfileDialog.tsx` (new)

---

## [2026-06-08] — Inline task editing (status + due date)

### What was built
- Status cell in task table is now a styled `SelectTrigger` that looks like a badge — click to open dropdown, pick new status → auto-saves optimistically (no reload)
- Due date cell click → inline `<input type="date">` pre-filled with current date; Enter/blur saves if changed, Escape cancels
- `savingCell` state disables actions on the row while a save is in-flight
- Optimistic update: local state updated from API response, avoiding full list reload

### Files modified
- `taskapp-frontend/src/pages/TasksPage.tsx`

---

## [2026-06-08] — Labels feature (full stack)

### What was built
- `TaskItem` entity: `Labels: List<string>` BSON array field
- `ITaskRepository` / `TaskRepository`: `GetAllLabelsByUserIdAsync` (distinct labels for user)
- `CreateTaskDto` / `UpdateTaskDto` / `TaskResponseDto`: `Labels` field; service trims/deduplicates on create/update
- `ITaskService` / `TaskService`: `GetAllLabelsAsync`
- `GET /api/tasks/labels` endpoint (must be declared before `{id}` route to avoid conflict)
- Frontend `LabelCombobox`: Jira-style tag input — type to filter, "Create 'xxx'" option for new labels, Enter to confirm, Backspace removes last, label colors derived from name hash
- Labels column in task table with colored chips
- Label filter dropdown (derives options from loaded tasks, no extra API call)
- `TaskFormDialog` updated with labels field and `availableLabels` prop

### Files modified/created
- `TaskApp.Domain/Entities/TaskItem.cs`
- `TaskApp.Domain/Interfaces/Repositories/ITaskRepository.cs`
- `TaskApp.Infrastructure/Persistence/Repositories/TaskRepository.cs`
- `TaskApp.Application/DTOs/Tasks/CreateTaskDto.cs`
- `TaskApp.Application/DTOs/Tasks/UpdateTaskDto.cs`
- `TaskApp.Application/DTOs/Tasks/TaskResponseDto.cs`
- `TaskApp.Application/Interfaces/ITaskService.cs`
- `TaskApp.Application/Services/TaskService.cs`
- `TaskApp.API/Controllers/TasksController.cs`
- `taskapp-frontend/src/types/index.ts`
- `taskapp-frontend/src/services/api.ts`
- `taskapp-frontend/src/components/LabelCombobox.tsx` (new)
- `taskapp-frontend/src/components/TaskFormDialog.tsx`
- `taskapp-frontend/src/pages/TasksPage.tsx`

---

## [2026-06-08] — TaskStatus: added ToDo, colored badges, readable labels

### What was changed
- `TaskStatus` enum: added `ToDo = 3` (existing integer values preserved to avoid breaking MongoDB data)
- `TaskService.CreateAsync`: default status changed from `Pending` → `ToDo`
- Frontend badge variants: ToDo=gray, Pending=yellow, InProgress=sky-blue, Completed=green
- Human-readable status labels in table ("In Progress" not "InProgress")
- Status and label filter dropdowns include all 4 statuses

### Files modified
- `TaskApp.Domain/Enums/TaskStatus.cs`
- `TaskApp.Application/Services/TaskService.cs`
- `taskapp-frontend/src/types/index.ts`
- `taskapp-frontend/src/components/ui/badge.tsx`
- `taskapp-frontend/src/pages/TasksPage.tsx`
- `taskapp-frontend/src/components/TaskFormDialog.tsx`

---

## [2026-06-08] — Fix: JsonStringEnumConverter for API enum serialization

### What was fixed
- `TaskStatus` and `UserRole` enums were serializing as integers (e.g. `0`) instead of strings (`"Pending"`)
- Root cause: `AddControllers()` uses System.Text.Json with integer enum default
- Fix: registered `JsonStringEnumConverter` globally in `Program.cs`

### Files modified
- `TaskApp.API/Program.cs`

---

## [2026-06-08] — New Task as modal, Login/Register page polish

### What was built
- `TaskFormPage.tsx` removed from router; create + edit now opens `TaskFormDialog` modal from the Tasks page
- "Tasks" nav button removed (redundant — user is already on the tasks page)
- Login and Register pages: card layout (`rounded-xl border bg-background shadow-sm p-6` on `bg-muted/40`), logo section above card (`ClipboardList` icon in navy rounded square), all inputs have `placeholder` props
- Register page: two-column grid for first/last name fields
- Both auth buttons use `bg-white text-primary hover:bg-white/90` for consistent style

### Files modified
- `taskapp-frontend/src/App.tsx` — removed `/tasks/new` and `/tasks/:id/edit` routes
- `taskapp-frontend/src/pages/LoginPage.tsx` — card layout, logo
- `taskapp-frontend/src/pages/RegisterPage.tsx` — first/last name grid, card layout

---

## [2026-06-08] — Mobile responsive + Paginator

### What was built
- **Dialog mobile fix**: `dialog.tsx` `w-full` → `w-[calc(100%-2rem)]` so modals have side spacing on small screens
- **Mobile card layout**: tasks list renders as stacked cards on `< md` breakpoint (`md:hidden`), desktop table unchanged (`hidden md:block`)
- **Paginator component**: `Paginator.tsx` — rows-per-page select (10/20/50), first/prev/next/last buttons, `X–Y of total` label; resets to page 1 when status or label filter changes

### Files modified/created
- `taskapp-frontend/src/components/ui/dialog.tsx` — width fix
- `taskapp-frontend/src/components/Paginator.tsx` (new)
- `taskapp-frontend/src/pages/TasksPage.tsx` — mobile card layout + pagination state

---

## [2026-06-08] — Audit fields + Dashboard

### What was built
- **Audit fields on TaskItem**: `UpdatedBy` (userId of last modifier) and `CompletedAt` (DateTime?) — both stored in MongoDB, returned in `TaskResponseDto`
- `CompletedAt` is set automatically when a task's status changes to `Completed`; cleared when status moves away from Completed
- `UpdatedBy` is set on every `UpdateAsync` call to the current user's id
- `GET /api/tasks/dashboard` endpoint returns per-status counts + completion timing data (days early/late vs due date)
- Dashboard page (`/dashboard` route): 5 stat cards (Total, To Do, Pending, In Progress, Completed) + recharts Pie chart (status distribution) + dumbbell chart (completion timing — due date vs actual completion, green = on time, red = late) + weekly velocity BarChart (SP and task count per week) + estimation accuracy BarChart (avg days to complete by SP value)
- Navigation: authenticated users now see Tasks + Dashboard nav links in AppNav (active link highlighted with `bg-white/20`)

### Files modified/created
- `TaskApp.Domain/Entities/TaskItem.cs` — added `UpdatedBy`, `CompletedAt` fields
- `TaskApp.Application/DTOs/Tasks/TaskResponseDto.cs` — added `UpdatedBy`, `CompletedAt`
- `TaskApp.Application/DTOs/Tasks/DashboardStatsDto.cs` (new) — `DashboardStatsDto` + `CompletionTimingDto`
- `TaskApp.Application/Interfaces/ITaskService.cs` — added `GetDashboardStatsAsync`
- `TaskApp.Application/Services/TaskService.cs` — `UpdateAsync` sets audit fields; `GetDashboardStatsAsync` computes stats
- `TaskApp.API/Controllers/TasksController.cs` — `GET /api/tasks/dashboard` (before `{id}`)
- `taskapp-frontend/src/types/index.ts` — `DashboardStatsDto`, `CompletionTimingEntry`, updated `TaskResponseDto`
- `taskapp-frontend/src/services/api.ts` — `getDashboardStats`
- `taskapp-frontend/src/pages/DashboardPage.tsx` (new)
- `taskapp-frontend/src/App.tsx` — `/dashboard` route
- `taskapp-frontend/src/components/AppNav.tsx` — Tasks + Dashboard nav links

### npm packages added
- `recharts` (charts library for Dashboard)
