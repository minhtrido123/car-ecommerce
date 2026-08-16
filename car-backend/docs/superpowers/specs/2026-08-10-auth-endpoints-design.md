# Auth Endpoints Design

Date: 2026-08-10

## Goal

Add register, login, and refresh-token endpoints to the car-backend minimal API. JWT bearer auth is already wired (`AddJwtAuthentication` in `Sorcha.ServiceDefaults`), and the `User` model already has `Email`, `PasswordHash`, `Role`, `Name` — but nothing hashes passwords or issues tokens.

## Endpoints

| Method | Route | Auth | Behavior |
|--------|-------|------|----------|
| POST | `/api/auth/register` | none | 201 + token pair + user info. Rejects duplicate email with 409. Always assigns role `Customer`. |
| POST | `/api/auth/login` | none | 200 + token pair. 401 on bad credentials. |
| POST | `/api/auth/refresh` | none | 200 + new token pair. 401 on invalid/expired/revoked/rotated token. |

All three are anonymous. No `[Authorize]` anywhere on auth routes.

## Data Model

New entity `RefreshToken : EntityBase` in Models:

- `Token` — SHA-256 hash of the opaque refresh token string. Unique index.
- `UserId` (Guid) — FK to `User`.
- `ExpiresAt` (DateTime).
- `IsRevoked` (bool).

Changes:
- `DbSet<RefreshToken>` on `SorchaDbContext`.
- EF Core migration for the new table.
- Refresh tokens stored hashed so a DB leak does not expose usable tokens.

## AuthService

New `IAuthService` + `AuthService` in the Services project.

Dependencies: `IRepository<User>`, `IRepository<RefreshToken>`, `IConfiguration`.

### RegisterAsync(email, password, name)

1. If a user with `Email` already exists → throw conflict (409).
2. Hash password with BCrypt.
3. Create `User` with role `Customer`.
4. Issue token pair.

### LoginAsync(email, password)

1. Find user by email; if missing → 401.
2. BCrypt.Verify; on failure → 401.
3. Issue token pair.

### RefreshAsync(refreshToken)

1. Hash incoming token, look up matching `RefreshToken`.
2. Fail (401) if: not found, revoked, or expired.
3. Revoke the old token (rotation).
4. Issue new token pair.

### Token issuance

- Access token: JWT, HS256, signed with `Jwt:Secret`, issuer/audience from config, 15 min expiry. Claims: `sub` (user id), `email`, `name`, `role`.
- Refresh token: 64 random bytes, base64url encoded, 7 days expiry, stored as SHA-256 hash.

### DI registration

`AuthService` registered scoped in `Program.cs`. `IRepository<>` already registered scoped.

## DTOs (API/DTOs/AuthDtos.cs)

- `RegisterRequest(string Email, string Password, string Name)`
- `LoginRequest(string Email, string Password)`
- `RefreshRequest(string RefreshToken)`
- `AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserResponse User)`

`UserResponse` already exists.

## Endpoint wiring

New `MapAuthEndpoints` static method in `Program.cs` (mirrors `MapEntityEndpoints` style). Creates group `/api/auth`. Error mapping: conflict → 409, bad creds/invalid refresh → 401, validation → 400.

## Configuration

Add to `API/appsettings.json` (already present in `appsettings.Development.json`):

```json
"Jwt": {
  "Secret": "...",
  "Issuer": "CarEcommerce",
  "Audience": "CarFrontEnd",
  "AccessTokenExpiryMinutes": 15,
  "RefreshTokenExpiryDays": 7
}
```

## Packages

- `BCrypt.Net-Next` added to `Services.csproj`.
- `System.IdentityModel.Tokens.Jwt` available transitively via the JwtBearer auth already referenced; add explicit reference to `API.csproj` only if the build requires it.

## Testing

Follow existing test pattern (xUnit, EF InMemory, FluentAssertions, `Tests` project referencing API/Services):

- Register hashes password, assigns Customer role, rejects duplicate email.
- Login succeeds with correct password, fails with wrong password/missing user.
- Refresh rotates token (old one rejected on reuse), rejects revoked/expired tokens.

Run: `dotnet test` in `car-backend/Tests`.

## Out of Scope

- Email verification, password reset.
- Rate limiting / lockout.
- Admin user management (existing generic `/api/users` endpoints handle that).
- Frontend work.
