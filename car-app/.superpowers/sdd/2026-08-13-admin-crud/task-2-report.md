# Task 2 Report: Backend — DTOs, DI registration, `/api/admins` endpoints

**Status:** DONE

## What I implemented

All four brief steps applied verbatim to `car-backend`:

1. **DTOs** (`API/DTOs/UserDto.cs`) — appended `CreateAdminRequest(string Email, string Password, string Name, string Role)` and `UpdateAdminRequest(string Email, string Name, string Role, string? Password)` records.
2. **DI registration** (`API/Program.cs`) — added `builder.Services.AddScoped<IAdminService, AdminService>();` directly after the `IAuthService` registration.
3. **Endpoint mapping + mapper** (`API/Program.cs`):
   - `MapAdminEndpoints(app);` call added immediately after `MapAuthEndpoints(app);`
   - `ToUserResponse(User user)` helper added right after `ToAuthResponse`
   - `MapAdminEndpoints` static method added at end of file.
4. **Verify** — build + full test suite run (see evidence).

Endpoints exposed:
- `POST /api/admins` (Admin role) → 201 + `UserResponse`, 409 on duplicate email
- `PUT /api/admins/{id:guid}` (Admin role) → 200 + `UserResponse`, 404 if missing, 409 on duplicate email

## Build + test evidence

Command (from `/home/trido/car-ecommerce/car-backend`):
```
dotnet build CarEcommerce.slnx && dotnet test
```

Key output:
```
Build succeeded.
    19 Warning(s)
    0 Error(s)

Test run for /home/trido/car-ecommerce/car-backend/Tests/bin/Debug/net10.0/Tests.dll
Passed!  - Failed: 0, Passed: 57, Skipped: 0, Total: 57, Duration: 6 s - Tests.dll (net10.0)
```

57/57 tests pass, including Task 1's `AdminServiceTests`. All warnings are pre-existing (package vulnerabilities NU1903/NU1904, nullable CS8604/CS8620, duplicate `using` CS0105 at Program.cs:4) — none introduced by this task.

## Files changed

- `car-backend/API/DTOs/UserDto.cs` (+4 lines)
- `car-backend/API/Program.cs` (+42 lines, -1 line)

Commit: `26228a2` — `feat: add /api/admins create and update endpoints` (2 files, 46 insertions, 1 deletion)

## Self-review findings

- All code matches brief verbatim (verified against diff).
- `IAdminService` method signatures match `AdminService` exactly (`CreateAdminAsync(email, password, name, role, ct)`, `UpdateAdminAsync(id, email, name, role, password, ct)`).
- `EmailAlreadyExistsException` and `User`/`CreateAdminRequest` all reachable via `GlobalUsings.cs` (`Services`, `Models`, `API.DTOs`); no new usings required. `AuthorizeAttribute` already in scope.
- Exception→status mapping correct: duplicate email → 409; `KeyNotFoundException` → 404.
- No existing Program.cs content reformatted or relocated.

**Brief deviation (benign):** the brief said to place `MapAdminEndpoints` "at the end of the file (after `MapAuthEndpoints`)". `MapAuthEndpoints` is not the last method in the file (it sits mid-file), so I placed the method at the literal end of the file, matching the primary instruction "at the end of the file". The parenthetical appears to be a stale reference from the plan author.

## Concerns

- None blocking. Two minor observations:
  1. The duplicate `using Microsoft.AspNetCore.Authorization;` at Program.cs lines 1/4 predates this task (CS0105 warning) — could be cleaned up separately.
  2. Endpoint methods use explicit status `Results.Json(..., 201)` rather than `Results.Created`/`CreatedAtRoute`, so no `Location` header is set on create. Consistent with brief; noted for follow-up if frontend needs a Location header.
