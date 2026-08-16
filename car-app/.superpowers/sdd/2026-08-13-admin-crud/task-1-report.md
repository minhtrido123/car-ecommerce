# Task 1 Report: Backend — AdminService

## Status: DONE_WITH_CONCERNS

## What I implemented

- `car-backend/Services/AdminService.cs` — `IAdminService` interface + `AdminService` class (bcrypt-hashed admin create/update). Transcribed verbatim from brief.
  - `CreateAdminAsync(email, password, name, role, ct)`: throws `EmailAlreadyExistsException` if email taken, hashes password via `BCrypt.Net.BCrypt.HashPassword`, adds via `IRepository<User>.AddAsync`.
  - `UpdateAdminAsync(id, email, name, role, password, ct)`: throws `KeyNotFoundException` if user missing, throws `EmailAlreadyExistsException` if email taken by another user (`u.Email == email && u.Id != id`), rehashes password only when non-blank, updates via `IRepository<User>.UpdateAsync`.
- `car-backend/Tests/AdminServiceTests.cs` — 6 xUnit tests (InMemory via `SorchaDbContext` + `EfRepository<User>`), same pattern as `AuthServiceTests`.

## TDD Evidence

### RED
Command: `dotnet test --filter "FullyQualifiedName~AdminServiceTests"`
Failure:
```
Tests/AdminServiceTests.cs(10,22): error CS0246: The type or namespace name 'AdminService' could not be found (are you missing a using directive or an assembly reference?)
```
Why expected: `Services/AdminService.cs` did not exist yet; the interface/class from brief hadn't been written.

### GREEN
Command: `dotnet test --filter "FullyQualifiedName~AdminServiceTests"`
Output:
```
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6
```

Full suite (`dotnet test`):
```
Passed!  - Failed: 0, Passed: 57, Skipped: 0, Total: 57
```
No regressions.

## Files changed

- `Services/AdminService.cs` (new, 52 lines)
- `Tests/AdminServiceTests.cs` (new, 79 lines)

## Self-review findings

1. **Brief error in test file** — brief's test file as written would not compile. Brief claims "Global usings already provide Models, Infrastructure, Services in both projects," but `Tests/GlobalUsings.cs` only contains `Xunit` and `Microsoft.EntityFrameworkCore`. `EfRepository<T>` lives in the `Repositories` namespace (`Repositories/EfRepository.cs:7`), which is not a global using in Tests. `AuthServiceTests.cs` explicitly adds `using Repositories;` (line 6).
   Fix: added `using Repositories;` to `AdminServiceTests.cs`. This matches the established test pattern exactly.
2. **Brief error in context** — `Services/GlobalUsings.cs` does provide `Infrastructure` and `Models`, so `AdminService.cs` compiles clean with only `namespace Services;` as transcribed. No change needed there.
3. **Verified** `UpdateAdminAsync` signature uses `string? password` and null-conditional `GetByIdAsync(...) ?? throw`, matching brief. `GetByIdAsync` nullability: `RefreshAsync` in AuthService uses same pattern, consistent.
4. **Vulnerability warnings pre-existing** — NU1903/NU1904 (Microsoft.OpenApi 2.0.0, System.Drawing.Common 4.7.0) appear across all projects; not introduced by this change. Not addressed (out of scope).

## Concerns

- `using Repositories;` added to test file deviates from the brief's verbatim test code. Necessary for compilation; consistent with `AuthServiceTests`. If the brief is treated as immutable, flag to plan owner — otherwise harmless.
- NuGet vulnerability warnings pre-existing in solution; unrelated to this task.
