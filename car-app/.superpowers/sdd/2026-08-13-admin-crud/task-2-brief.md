### Task 2: Backend — DTOs, DI registration, `/api/admins` endpoints

**Files:**
- Modify: `car-backend/API/DTOs/UserDto.cs`
- Modify: `car-backend/API/Program.cs`

**Interfaces:**
- Consumes: `Services.IAdminService` (Task 1).
- Produces: `POST /api/admins` (Admin role) — body `CreateAdminRequest(string Email, string Password, string Name, string Role)`, 201 + `UserResponse`, 409 on duplicate email. `PUT /api/admins/{id:guid}` (Admin role) — body `UpdateAdminRequest(string Email, string Name, string Role, string? Password)`, 200 + `UserResponse`, 404 if missing, 409 on duplicate email.

- [ ] **Step 1: Add DTO records**

Append to `car-backend/API/DTOs/UserDto.cs`:

```csharp
public record CreateAdminRequest(string Email, string Password, string Name, string Role);

public record UpdateAdminRequest(string Email, string Name, string Role, string? Password);
```

- [ ] **Step 2: Register AdminService in DI**

In `car-backend/API/Program.cs`, after the line `builder.Services.AddScoped<IAuthService, AuthService>();` (line 19) add:

```csharp
builder.Services.AddScoped<IAdminService, AdminService>();
```

- [ ] **Step 3: Add endpoint mapping + response mapper**

In `car-backend/API/Program.cs`, after the `MapAuthEndpoints(app);` call (line 60) add:

```csharp
MapAdminEndpoints(app);
```

Add the `ToUserResponse` helper next to the existing `ToAuthResponse` helper (after line 159):

```csharp
static UserResponse ToUserResponse(User user) =>
    new(user.Id, user.Email, user.Role, user.Name, user.CreatedAt);
```

Add the endpoint mapper at the end of the file (after `MapAuthEndpoints`):

```csharp
static void MapAdminEndpoints(WebApplication app)
{
    var admin = app.MapGroup("/api/admins")
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

    admin.MapPost("/", async ([FromServices] IAdminService adminService, CreateAdminRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var user = await adminService.CreateAdminAsync(request.Email, request.Password, request.Name, request.Role, cancellationToken);
            return Results.Json(ToUserResponse(user), statusCode: StatusCodes.Status201Created);
        }
        catch (EmailAlreadyExistsException)
        {
            return Results.Conflict(new { error = "Email already registered" });
        }
    }).WithName("Create_Admin");

    admin.MapPut("/{id:guid}", async ([FromServices] IAdminService adminService, Guid id, UpdateAdminRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var user = await adminService.UpdateAdminAsync(id, request.Email, request.Name, request.Role, request.Password, cancellationToken);
            return Results.Ok(ToUserResponse(user));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (EmailAlreadyExistsException)
        {
            return Results.Conflict(new { error = "Email already registered" });
        }
    }).WithName("Update_Admin");
}
```

- [ ] **Step 4: Verify build + full test suite**

Run (from `car-backend`): `dotnet build CarEcommerce.slnx && dotnet test`
Expected: build succeeds, all tests pass.

- [ ] **Step 5: Commit (backend repo only)**

```bash
cd car-backend
git add API/DTOs/UserDto.cs API/Program.cs
git commit -m "feat: add /api/admins create and update endpoints"
```

---

