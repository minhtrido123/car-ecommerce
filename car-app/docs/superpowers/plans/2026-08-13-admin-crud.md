# Admin CRUD Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a config-driven CRUD admin panel covering all backend entities, plus a small backend addition for admin user creation with server-side password hashing.

**Architecture:** One entity config registry (`adminEntities.ts`) drives three generic client components — `AdminTable`, `EntityFormModal`, `AdminCrudPage` — reachable from a dynamic route `/admin/[entity]`. Writes go through axios helpers in `adminApi.ts` (existing auth interceptor handles tokens). Backend gains `AdminService` (bcrypt hashing) exposed at `POST/PUT /api/admins`.

**Tech Stack:** Next.js 16.2.10 (App Router), React 19, TypeScript, zustand, axios, react-hook-form + zod, Metro4 CSS, Tailwind v4. Backend: .NET 10, xUnit + EF InMemory.

## Global Constraints

- **Next.js 16 params:** dynamic route `params` is a `Promise`; client pages must read via `use(params)` from `react`. Do NOT use the old sync signature.
- **Auth:** all admin write calls go through `api` axios instance from `app/(main)/lib/api.ts` (attaches bearer token, auto-refreshes on 401). Never use `fetch` for these.
- **Users entity:** list from `GET /api/users`, but create/edit via `POST/PUT /api/admins`. Never send raw passwords anywhere except the `password` field of `/api/admins`.
- **Generic body shapes:** create = `POST /api/{route}` with `{ data: {...} }`; update = `PUT /api/{route}/{id}` with `{ id, data: {...} }`; delete = `DELETE /api/{route}/{id}`. Exception: `/api/admins` takes flat `{ email, password, name, role }`.
- **PagedResult JSON:** `{ items, totalCount, pageNumber, pageSize }` (camelCase).
- **Styling:** Metro4 CSS classes for tables/buttons/forms (`.table .table-border .cell-border`, `.button`, `.button primary/success/danger`, `.form-group`, `.card`, `.alert`), Tailwind for layout/modal overlay. Do not depend on Metro4 JS `data-role` init.
- **No new dependencies.** No test framework for frontend — verify via `npm run lint`, `npx tsc --noEmit`, `npm run build`.
- **No toast system:** show inline error strings only.
- `car-app` is NOT a git repo; `car-backend` is. Frontend commits are skipped; backend commits per task run in `car-backend`.

---

### Task 1: Backend — AdminService (create/update admin users with bcrypt)

**Files:**
- Create: `car-backend/Services/AdminService.cs`
- Test: `car-backend/Tests/AdminServiceTests.cs`

**Interfaces:**
- Consumes: `IRepository<User>` (`GetFilteredAsync(Expression<Func<User,bool>>, ct)`, `GetByIdAsync(Guid, ct)`, `AddAsync(User, ct)`, `UpdateAsync(User, ct)`), `EmailAlreadyExistsException`, `BCrypt.Net.BCrypt` (all already in the solution).
- Produces: `Services.IAdminService` with `CreateAdminAsync(string email, string password, string name, string role, CancellationToken) : Task<User>` and `UpdateAdminAsync(Guid id, string email, string name, string role, string? password, CancellationToken) : Task<User>`.

- [ ] **Step 1: Write the failing tests**

Create `car-backend/Tests/AdminServiceTests.cs`:

```csharp
using Infrastructure;
using Models;
using Services;

namespace Tests;

public class AdminServiceTests
{
    private readonly SorchaDbContext _db;
    private readonly AdminService _admin;

    public AdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new SorchaDbContext(options);
        _admin = new AdminService(new EfRepository<User>(_db));
    }

    [Fact]
    public async Task CreateAdminAsync_CreatesUserWithHashedPasswordAndGivenRole()
    {
        var user = await _admin.CreateAdminAsync("boss@test.com", "secret123", "Boss", "Admin");

        Assert.Equal("boss@test.com", user.Email);
        Assert.Equal("Admin", user.Role);
        Assert.Equal("Boss", user.Name);
        Assert.True(BCrypt.Net.BCrypt.Verify("secret123", user.PasswordHash));
    }

    [Fact]
    public async Task CreateAdminAsync_Throws_WhenEmailTaken()
    {
        await _admin.CreateAdminAsync("dup@test.com", "secret123", "A", "Admin");

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            _admin.CreateAdminAsync("dup@test.com", "other", "B", "Admin"));
    }

    [Fact]
    public async Task UpdateAdminAsync_UpdatesFieldsAndRehashesPassword()
    {
        var created = await _admin.CreateAdminAsync("a@test.com", "old-pass", "A", "Admin");

        var updated = await _admin.UpdateAdminAsync(created.Id, "b@test.com", "B", "Staff", "new-pass");

        Assert.Equal("b@test.com", updated.Email);
        Assert.Equal("B", updated.Name);
        Assert.Equal("Staff", updated.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("new-pass", updated.PasswordHash));
    }

    [Fact]
    public async Task UpdateAdminAsync_KeepsPassword_WhenBlankOrNull()
    {
        var created = await _admin.CreateAdminAsync("c@test.com", "keep-pass", "C", "Admin");

        var updated = await _admin.UpdateAdminAsync(created.Id, "c@test.com", "C", "Admin", null);

        Assert.True(BCrypt.Net.BCrypt.Verify("keep-pass", updated.PasswordHash));
    }

    [Fact]
    public async Task UpdateAdminAsync_Throws_WhenUserMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _admin.UpdateAdminAsync(Guid.NewGuid(), "x@test.com", "X", "Admin", null));
    }

    [Fact]
    public async Task UpdateAdminAsync_Throws_WhenEmailTakenByAnotherUser()
    {
        var first = await _admin.CreateAdminAsync("one@test.com", "p", "One", "Admin");
        await _admin.CreateAdminAsync("two@test.com", "p", "Two", "Admin");

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            _admin.UpdateAdminAsync(first.Id, "two@test.com", "One", "Admin", null));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run (from `car-backend`): `dotnet test`
Expected: FAIL — `AdminService` type not found.

- [ ] **Step 3: Write minimal implementation**

Create `car-backend/Services/AdminService.cs`:

```csharp
namespace Services;

public interface IAdminService
{
    Task<User> CreateAdminAsync(string email, string password, string name, string role, CancellationToken cancellationToken = default);
    Task<User> UpdateAdminAsync(Guid id, string email, string name, string role, string? password, CancellationToken cancellationToken = default);
}

public class AdminService : IAdminService
{
    private readonly IRepository<User> _userRepository;

    public AdminService(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> CreateAdminAsync(string email, string password, string name, string role, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetFilteredAsync(u => u.Email == email, cancellationToken);
        if (existing.Count > 0) throw new EmailAlreadyExistsException();

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Name = name
        };
        return await _userRepository.AddAsync(user, cancellationToken);
    }

    public async Task<User> UpdateAdminAsync(Guid id, string email, string name, string role, string? password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User {id} not found");

        var taken = await _userRepository.GetFilteredAsync(u => u.Email == email && u.Id != id, cancellationToken);
        if (taken.Count > 0) throw new EmailAlreadyExistsException();

        user.Email = email;
        user.Name = name;
        user.Role = role;
        if (!string.IsNullOrWhiteSpace(password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }
        return await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run (from `car-backend`): `dotnet test`
Expected: PASS — all 6 AdminServiceTests green.

- [ ] **Step 5: Commit (backend repo only)**

```bash
cd car-backend
git add Services/AdminService.cs Tests/AdminServiceTests.cs
git commit -m "feat: add AdminService with bcrypt-hashed admin user management"
```

---

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

### Task 3: Frontend — `adminApi.ts` CRUD helpers + lookup cache

**Files:**
- Create: `car-app/app/(admin)/lib/adminApi.ts`

**Interfaces:**
- Consumes: `api` from `../../(main)/lib/api`; `EntityConfig`, `EntityField` types from `./adminEntities` (Task 4 — type-only imports, defined in same task batch but consumed here).
- Produces:
  - `PagedResult<T> { items: T[]; totalCount: number; pageNumber: number; pageSize: number }`
  - `list<T>(config, pageNumber, pageSize = 20) : Promise<PagedResult<T>>`
  - `create(config, values: Record<string, unknown>) : Promise<unknown>`
  - `update(config, id: string, values: Record<string, unknown>) : Promise<unknown>`
  - `remove(config, id: string) : Promise<void>`
  - `getLookupOptions(field: EntityField) : Promise<Map<string, string>>` (module-level cache)

- [ ] **Step 1: Write the module**

Create `car-app/app/(admin)/lib/adminApi.ts`:

```ts
import api from "../../(main)/lib/api";
import type { EntityConfig, EntityField } from "./adminEntities";

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export async function list<T>(
  config: EntityConfig,
  pageNumber: number,
  pageSize = 20
): Promise<PagedResult<T>> {
  const { data } = await api.get<PagedResult<T>>(`/${config.route}`, {
    params: { pageNumber, pageSize },
  });
  return data;
}

export async function create(
  config: EntityConfig,
  values: Record<string, unknown>
): Promise<unknown> {
  const { data } = config.userAuth
    ? await api.post("/admins", values)
    : await api.post(`/${config.route}`, { data: values });
  return data;
}

export async function update(
  config: EntityConfig,
  id: string,
  values: Record<string, unknown>
): Promise<unknown> {
  const { data } = config.userAuth
    ? await api.put(`/admins/${id}`, values)
    : await api.put(`/${config.route}/${id}`, { id, data: values });
  return data;
}

export async function remove(config: EntityConfig, id: string): Promise<void> {
  await api.delete(`/${config.route}/${id}`);
}

const lookupCache = new Map<string, Map<string, string>>();

export async function getLookupOptions(
  field: EntityField
): Promise<Map<string, string>> {
  const { route, valueKey, labelKey } = field.lookup!;
  const cacheKey = `${route}:${valueKey}:${labelKey}`;
  const cached = lookupCache.get(cacheKey);
  if (cached) return cached;

  const map = new Map<string, string>();
  let page = 1;
  let totalCount = Infinity;
  const pageSize = 200;
  while ((page - 1) * pageSize < totalCount) {
    const { data } = await api.get<PagedResult<Record<string, unknown>>>(
      `/${route}`,
      { params: { pageNumber: page, pageSize } }
    );
    for (const item of data.items) {
      map.set(String(item[valueKey]), String(item[labelKey]));
    }
    totalCount = data.totalCount;
    page += 1;
  }
  lookupCache.set(cacheKey, map);
  return map;
}
```

Note: `adminEntities.ts` is created in Task 4; both files compile together. To verify this task's file alone, temporarily stub `adminEntities.ts` with the type declarations (Task 4 Step 1) or run the full-verify step in Task 4.

- [ ] **Step 2: Typecheck**

Run (from `car-app`): `npx tsc --noEmit`
Expected: may error on missing `./adminEntities` until Task 4 — acceptable; rerun after Task 4.

---

### Task 4: Frontend — entity config registry (`adminEntities.ts`)

**Files:**
- Create: `car-app/app/(admin)/lib/adminEntities.ts`

**Interfaces:**
- Consumes: nothing.
- Produces: `FieldType`, `EntityField`, `EntityConfig` types and `adminEntities: Record<string, EntityConfig>` keyed by URL slug. Consumed by `adminApi.ts`, `AdminTable`, `EntityFormModal`, `AdminCrudPage`, dynamic route.

- [ ] **Step 1: Write types + all 12 entity configs**

Create `car-app/app/(admin)/lib/adminEntities.ts`:

```ts
export type FieldType =
  | "text"
  | "email"
  | "password"
  | "number"
  | "decimal"
  | "checkbox"
  | "textarea"
  | "select";

export interface EntityField {
  key: string;
  label: string;
  type: FieldType;
  required?: boolean;
  showInTable?: boolean;
  options?: { value: string; label: string }[];
  lookup?: { route: string; valueKey: string; labelKey: string };
  hiddenOnCreate?: boolean;
  hiddenOnEdit?: boolean;
}

export interface EntityConfig {
  slug: string;
  route: string;
  label: string;
  singular?: string;
  userAuth?: boolean;
  canCreate?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  fields: EntityField[];
}

const statusOptions = [
  { value: "Active", label: "Active" },
  { value: "Inactive", label: "Inactive" },
  { value: "Sold", label: "Sold" },
];

export const adminEntities: Record<string, EntityConfig> = {
  users: {
    slug: "users",
    route: "users",
    label: "Users",
    singular: "User",
    userAuth: true,
    fields: [
      { key: "email", label: "Email", type: "email", required: true },
      { key: "password", label: "Password", type: "password", showInTable: false },
      { key: "name", label: "Name", type: "text", required: true },
      {
        key: "role",
        label: "Role",
        type: "select",
        required: true,
        options: [
          { value: "Admin", label: "Admin" },
          { value: "Staff", label: "Staff" },
          { value: "Customer", label: "Customer" },
        ],
      },
      { key: "createdAt", label: "Created", type: "text", hiddenOnCreate: true, hiddenOnEdit: true },
    ],
  },
  brands: {
    slug: "brands",
    route: "brands",
    label: "Brands",
    singular: "Brand",
    fields: [
      { key: "name", label: "Name", type: "text", required: true },
      { key: "country", label: "Country", type: "text" },
    ],
  },
  "car-models": {
    slug: "car-models",
    route: "car-models",
    label: "Car Models",
    singular: "Car Model",
    fields: [
      { key: "brandId", label: "Brand", type: "select", required: true, lookup: { route: "brands", valueKey: "id", labelKey: "name" } },
      { key: "name", label: "Name", type: "text", required: true },
      { key: "yearStart", label: "Year Start", type: "number" },
      { key: "yearEnd", label: "Year End", type: "number" },
    ],
  },
  categories: {
    slug: "categories",
    route: "categories",
    label: "Categories",
    singular: "Category",
    fields: [
      { key: "name", label: "Name", type: "text", required: true },
      { key: "slug", label: "Slug", type: "text", required: true },
      {
        key: "type",
        label: "Type",
        type: "select",
        required: true,
        options: [
          { value: "Car", label: "Car" },
          { value: "Part", label: "Part" },
        ],
      },
    ],
  },
  cars: {
    slug: "cars",
    route: "cars",
    label: "Cars",
    singular: "Car",
    fields: [
      { key: "brandId", label: "Brand", type: "select", lookup: { route: "brands", valueKey: "id", labelKey: "name" } },
      { key: "modelId", label: "Model", type: "select", lookup: { route: "car-models", valueKey: "id", labelKey: "name" } },
      { key: "year", label: "Year", type: "number", required: true },
      { key: "price", label: "Price", type: "decimal", required: true },
      { key: "mileage", label: "Mileage", type: "number" },
      { key: "color", label: "Color", type: "text" },
      { key: "categoryId", label: "Category", type: "select", lookup: { route: "categories", valueKey: "id", labelKey: "name" } },
      { key: "status", label: "Status", type: "select", required: true, options: statusOptions },
      { key: "quantity", label: "Quantity", type: "number" },
      { key: "description", label: "Description", type: "textarea" },
      { key: "specs", label: "Specs", type: "textarea" },
    ],
  },
  parts: {
    slug: "parts",
    route: "parts",
    label: "Parts",
    singular: "Part",
    fields: [
      { key: "name", label: "Name", type: "text", required: true },
      { key: "brand", label: "Brand", type: "text" },
      { key: "sku", label: "SKU", type: "text" },
      { key: "price", label: "Price", type: "decimal", required: true },
      { key: "quantity", label: "Quantity", type: "number" },
      { key: "status", label: "Status", type: "select", required: true, options: statusOptions },
      { key: "categoryId", label: "Category", type: "select", lookup: { route: "categories", valueKey: "id", labelKey: "name" } },
      { key: "description", label: "Description", type: "textarea" },
      { key: "specs", label: "Specs", type: "textarea" },
    ],
  },
  orders: {
    slug: "orders",
    route: "orders",
    label: "Orders",
    singular: "Order",
    canCreate: false,
    fields: [
      { key: "buyerId", label: "Buyer", type: "select", hiddenOnEdit: true, lookup: { route: "users", valueKey: "id", labelKey: "email" } },
      {
        key: "status",
        label: "Status",
        type: "select",
        required: true,
        options: [
          { value: "Pending", label: "Pending" },
          { value: "Processing", label: "Processing" },
          { value: "Shipped", label: "Shipped" },
          { value: "Delivered", label: "Delivered" },
          { value: "Completed", label: "Completed" },
          { value: "Cancelled", label: "Cancelled" },
        ],
      },
      { key: "totalAmount", label: "Total", type: "decimal", hiddenOnEdit: true },
      { key: "createdAt", label: "Created", type: "text", hiddenOnCreate: true, hiddenOnEdit: true },
    ],
  },
  "order-items": {
    slug: "order-items",
    route: "order-items",
    label: "Order Items",
    singular: "Order Item",
    canCreate: false,
    canEdit: false,
    canDelete: false,
    fields: [
      { key: "orderId", label: "Order", type: "text" },
      { key: "productId", label: "Product", type: "text" },
      { key: "quantity", label: "Quantity", type: "number" },
      { key: "unitPrice", label: "Unit Price", type: "decimal" },
    ],
  },
  reviews: {
    slug: "reviews",
    route: "reviews",
    label: "Reviews",
    singular: "Review",
    canCreate: false,
    canEdit: false,
    fields: [
      { key: "productId", label: "Product", type: "text" },
      { key: "userId", label: "User", type: "text" },
      { key: "rating", label: "Rating", type: "number" },
      { key: "comment", label: "Comment", type: "text" },
      { key: "createdAt", label: "Created", type: "text", hiddenOnCreate: true, hiddenOnEdit: true },
    ],
  },
  "product-images": {
    slug: "product-images",
    route: "product-images",
    label: "Product Images",
    singular: "Product Image",
    fields: [
      { key: "productId", label: "Product", type: "text", required: true },
      { key: "url", label: "URL", type: "text", required: true },
      { key: "isPrimary", label: "Primary", type: "checkbox" },
    ],
  },
  "cart-items": {
    slug: "cart-items",
    route: "cart-items",
    label: "Cart Items",
    singular: "Cart Item",
    fields: [
      { key: "userId", label: "User", type: "text", required: true },
      { key: "productId", label: "Product", type: "text", required: true },
      { key: "quantity", label: "Quantity", type: "number", required: true },
    ],
  },
  favorites: {
    slug: "favorites",
    route: "favorites",
    label: "Favorites",
    singular: "Favorite",
    canCreate: false,
    canEdit: false,
    fields: [
      { key: "userId", label: "User", type: "text" },
      { key: "productId", label: "Product", type: "text" },
      { key: "createdAt", label: "Created", type: "text", hiddenOnCreate: true, hiddenOnEdit: true },
    ],
  },
};
```

- [ ] **Step 2: Typecheck**

Run (from `car-app`): `npx tsc --noEmit`
Expected: PASS (both `adminEntities.ts` and `adminApi.ts` now compile).

---

### Task 5: Frontend — `EntityFormModal` (generated form + create/update)

**Files:**
- Create: `car-app/app/(admin)/components/EntityFormModal.tsx`

**Interfaces:**
- Consumes: `EntityConfig`, `EntityField` (Task 4); `create`, `update`, `getLookupOptions` (Task 3).
- Produces: default-export `EntityFormModal({ config, mode, editing, onClose, onSaved })` where `mode: "create" | "edit"`, `editing: Record<string, unknown> | null`, `onClose: () => void`, `onSaved: () => void`. Consumed by `AdminCrudPage` (Task 7).

- [ ] **Step 1: Write the component**

Create `car-app/app/(admin)/components/EntityFormModal.tsx`:

```tsx
"use client";

import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { EntityConfig, EntityField } from "../lib/adminEntities";
import { create, getLookupOptions, update } from "../lib/adminApi";

type Row = Record<string, unknown>;
type Mode = "create" | "edit";

function editableFieldsFor(config: EntityConfig, mode: Mode) {
  return config.fields.filter((f) => (mode === "create" ? !f.hiddenOnCreate : !f.hiddenOnEdit));
}

function fieldSchema(field: EntityField, required: boolean): z.ZodType {
  switch (field.type) {
    case "email":
      return required
        ? z.string().min(1, `${field.label} is required`).email("Invalid email")
        : z.string().email("Invalid email").optional();
    case "number":
      return required
        ? z.coerce.number({ message: `${field.label} must be a number` }).int(`${field.label} must be an integer`)
        : z.preprocess(
            (v) => (v === "" || v == null ? undefined : Number(v)),
            z.number().int().optional()
          );
    case "decimal":
      return required
        ? z.coerce.number({ message: `${field.label} must be a number` })
        : z.preprocess(
            (v) => (v === "" || v == null ? undefined : Number(v)),
            z.number().optional()
          );
    case "checkbox":
      return z.boolean();
    case "password":
      return z.string();
    default:
      return required ? z.string().min(1, `${field.label} is required`) : z.string();
  }
}

function buildSchema(config: EntityConfig, mode: Mode) {
  const shape: Record<string, z.ZodType> = {};
  for (const field of config.fields) {
    if (mode === "create" ? field.hiddenOnCreate : field.hiddenOnEdit) continue;
    const required = field.type === "password" ? false : (field.required ?? false);
    shape[field.key] = fieldSchema(field, required);
  }
  return z.object(shape);
}

function toDefaults(config: EntityConfig, mode: Mode, editing: Row | null) {
  const defaults: Record<string, unknown> = {};
  for (const field of config.fields) {
    if (mode === "create" ? field.hiddenOnCreate : field.hiddenOnEdit) continue;
    if (editing && editing[field.key] != null) defaults[field.key] = editing[field.key];
    else if (field.type === "checkbox") defaults[field.key] = false;
    else defaults[field.key] = "";
  }
  return defaults;
}

export default function EntityFormModal({
  config,
  mode,
  editing,
  onClose,
  onSaved,
}: {
  config: EntityConfig;
  mode: Mode;
  editing: Row | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const editableFields = useMemo(() => editableFieldsFor(config, mode), [config, mode]);
  const [lookups, setLookups] = useState<Record<string, Map<string, string>>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const schema = useMemo(() => buildSchema(config, mode), [config, mode]);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<Record<string, unknown>>({
    resolver: zodResolver(schema),
    defaultValues: toDefaults(config, mode, editing),
  });

  useEffect(() => {
    reset(toDefaults(config, mode, editing));
  }, [config, mode, editing, reset]);

  useEffect(() => {
    let cancelled = false;
    for (const field of editableFields) {
      if (field.type === "select" && field.lookup) {
        getLookupOptions(field).then((options) => {
          if (!cancelled) setLookups((prev) => ({ ...prev, [field.key]: options }));
        }).catch(() => {});
      }
    }
    return () => {
      cancelled = true;
    };
  }, [editableFields]);

  const onSubmit = async (values: Record<string, unknown>) => {
    setSaving(true);
    setSubmitError(null);
    try {
      if (mode === "create") {
        await create(config, values);
      } else {
        await update(config, String(editing!.id), values);
      }
      onSaved();
      onClose();
    } catch (err) {
      const response = (err as { response?: { status?: number; data?: { error?: string } } })?.response;
      setSubmitError(response?.data?.error ?? `Save failed (${response?.status ?? "network error"})`);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
      onClick={onClose}
    >
      <form
        onClick={(e) => e.stopPropagation()}
        onSubmit={handleSubmit(onSubmit)}
        className="card p-6 w-full max-w-2xl max-h-[90vh] overflow-y-auto"
      >
        <h3 className="text-xl font-bold mb-4">
          {mode === "create" ? "Add" : "Edit"} {config.singular ?? config.label}
        </h3>
        {submitError && <div className="alert alert-error mb-4">{submitError}</div>}
        {editableFields.map((field) => (
          <div className="form-group" key={field.key}>
            <label>
              {field.label}
              {field.required ? " *" : ""}
            </label>
            {field.type === "select" ? (
              <select className="input w-full" {...register(field.key)}>
                <option value="">Select...</option>
                {(field.options ??
                  Array.from(lookups[field.key] ?? []).map(([value, label]) => ({ value, label }))).map(
                  (opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  )
                )}
              </select>
            ) : field.type === "textarea" ? (
              <textarea className="input w-full" rows={4} {...register(field.key)} />
            ) : field.type === "checkbox" ? (
              <input type="checkbox" {...register(field.key)} />
            ) : (
              <input
                className="input w-full"
                type={
                  field.type === "password"
                    ? "password"
                    : field.type === "number" || field.type === "decimal"
                      ? "number"
                      : "text"
                }
                step={field.type === "decimal" ? "0.01" : undefined}
                {...register(field.key)}
              />
            )}
            {errors[field.key] && (
              <p className="text-red-500">{String(errors[field.key]?.message ?? "")}</p>
            )}
            {field.type === "password" && mode === "edit" && (
              <small className="text-muted">Leave blank to keep current password.</small>
            )}
          </div>
        ))}
        <div className="flex gap-2 justify-end mt-4">
          <button type="button" className="button" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="button success" disabled={saving}>
            {saving ? "Saving..." : "Save"}
          </button>
        </div>
      </form>
    </div>
  );
}
```

- [ ] **Step 2: Typecheck**

Run (from `car-app`): `npx tsc --noEmit`
Expected: PASS.

---

### Task 6: Frontend — `AdminTable` (paged list, row actions, delete)

**Files:**
- Create: `car-app/app/(admin)/components/AdminTable.tsx`

**Interfaces:**
- Consumes: `list`, `remove`, `getLookupOptions`, `PagedResult` (Task 3); `EntityConfig`, `EntityField` (Task 4); existing `Pagination` from `../../(main)/components/Pagination`.
- Produces: default-export `AdminTable({ config, onEdit, onAdd })` where `onEdit: (row: Record<string, unknown>) => void`, `onAdd: () => void`. Consumed by `AdminCrudPage` (Task 7).

- [ ] **Step 1: Write the component**

Create `car-app/app/(admin)/components/AdminTable.tsx`:

```tsx
"use client";

import { useEffect, useState } from "react";
import Pagination from "../../(main)/components/Pagination";
import type { EntityConfig, EntityField } from "../lib/adminEntities";
import type { PagedResult } from "../lib/adminApi";
import { getLookupOptions, list, remove } from "../lib/adminApi";

type Row = Record<string, unknown>;

function cellValue(
  field: EntityField,
  row: Row,
  lookups: Record<string, Map<string, string>>
) {
  const value = row[field.key];
  if (value == null) return "—";
  if (field.type === "checkbox") return value ? "Yes" : "No";
  if (field.type === "select" && field.lookup) {
    return lookups[field.key]?.get(String(value)) ?? String(value);
  }
  return String(value);
}

export default function AdminTable({
  config,
  onEdit,
  onAdd,
}: {
  config: EntityConfig;
  onEdit: (row: Row) => void;
  onAdd: () => void;
}) {
  const columns = config.fields.filter((f) => f.showInTable !== false);
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PagedResult<Row> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [lookups, setLookups] = useState<Record<string, Map<string, string>>>({});

  const load = async (targetPage: number) => {
    setError(null);
    try {
      setData(await list<Row>(config, targetPage));
    } catch {
      setError("Failed to load data");
    }
  };

  useEffect(() => {
    load(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, config]);

  useEffect(() => {
    let cancelled = false;
    for (const field of config.fields) {
      if (field.type === "select" && field.lookup) {
        getLookupOptions(field).then((options) => {
          if (!cancelled) setLookups((prev) => ({ ...prev, [field.key]: options }));
        }).catch(() => {});
      }
    }
    return () => {
      cancelled = true;
    };
  }, [config]);

  const handleDelete = async (row: Row) => {
    if (!window.confirm(`Delete ${config.singular ?? config.label}?`)) return;
    try {
      await remove(config, String(row.id));
      if (data && data.items.length === 1 && page > 1) {
        setPage(page - 1);
      } else {
        load(page);
      }
    } catch {
      setError("Delete failed");
    }
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-2xl font-bold">{config.label}</h2>
        {config.canCreate !== false && (
          <button className="button primary" onClick={onAdd}>
            Add {config.singular ?? config.label}
          </button>
        )}
      </div>
      {error && <div className="alert alert-error mb-4">{error}</div>}
      <table className="table table-border cell-border">
        <thead>
          <tr>
            {columns.map((field) => (
              <th key={field.key}>{field.label}</th>
            ))}
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {(data?.items ?? []).map((row) => (
            <tr key={String(row.id)}>
              {columns.map((field) => (
                <td key={field.key}>{cellValue(field, row, lookups)}</td>
              ))}
              <td>
                {config.canEdit !== false && (
                  <button className="button small" onClick={() => onEdit(row)}>
                    Edit
                  </button>
                )}
                {config.canDelete !== false && (
                  <button
                    className="button small danger ml-2"
                    onClick={() => handleDelete(row)}
                  >
                    Delete
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {data && (
        <Pagination
          currentPage={data.pageNumber}
          totalCount={data.totalCount}
          pageSize={data.pageSize}
          onPageChange={setPage}
        />
      )}
    </div>
  );
}
```

- [ ] **Step 2: Typecheck**

Run (from `car-app`): `npx tsc --noEmit`
Expected: PASS.

---

### Task 7: Frontend — `AdminCrudPage` + dynamic route `/admin/[entity]`

**Files:**
- Create: `car-app/app/(admin)/components/AdminCrudPage.tsx`
- Create: `car-app/app/(admin)/admin/[entity]/page.tsx`

**Interfaces:**
- Consumes: `AdminTable` (Task 6), `EntityFormModal` (Task 5), `adminEntities` (Task 4).
- Produces: route `/admin/{slug}` for every key in `adminEntities`; 404-style fallback for unknown slugs.

- [ ] **Step 1: Write `AdminCrudPage`**

Create `car-app/app/(admin)/components/AdminCrudPage.tsx`:

```tsx
"use client";

import { useState } from "react";
import type { EntityConfig } from "../lib/adminEntities";
import AdminTable from "./AdminTable";
import EntityFormModal from "./EntityFormModal";

type Row = Record<string, unknown>;

export default function AdminCrudPage({ config }: { config: EntityConfig }) {
  const [modal, setModal] = useState<{ mode: "create" | "edit"; row: Row | null } | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  const refresh = () => setRefreshKey((k) => k + 1);

  return (
    <div>
      <AdminTable
        key={refreshKey}
        config={config}
        onAdd={() => setModal({ mode: "create", row: null })}
        onEdit={(row) => setModal({ mode: "edit", row })}
      />
      {modal && (
        <EntityFormModal
          config={config}
          mode={modal.mode}
          editing={modal.row}
          onClose={() => setModal(null)}
          onSaved={refresh}
        />
      )}
    </div>
  );
}
```

- [ ] **Step 2: Write the dynamic route page**

Create `car-app/app/(admin)/admin/[entity]/page.tsx`:

```tsx
"use client";

import { use } from "react";
import Link from "next/link";
import { adminEntities } from "../../lib/adminEntities";
import AdminCrudPage from "../../components/AdminCrudPage";

export default function AdminEntityPage({
  params,
}: {
  params: Promise<{ entity: string }>;
}) {
  const { entity } = use(params);
  const config = adminEntities[entity];

  if (!config) {
    return (
      <div>
        <h2 className="text-2xl font-bold mb-4">Unknown section</h2>
        <p>No entity configured for &quot;{entity}&quot;.</p>
        <Link className="button mt-4" href="/admin/dashboard">
          Back to dashboard
        </Link>
      </div>
    );
  }

  return <AdminCrudPage config={config} />;
}
```

- [ ] **Step 3: Typecheck + lint**

Run (from `car-app`): `npx tsc --noEmit && npm run lint`
Expected: PASS.

---

### Task 8: Frontend — real sidebar links

**Files:**
- Modify: `car-app/app/(admin)/components/SideNav.tsx`

**Interfaces:**
- Consumes: routes produced by Task 7 (`/admin/{slug}`) plus existing `/admin/dashboard`.
- Produces: `Link` targets for all managed entities.

- [ ] **Step 1: Rewrite `SideNav` links**

Replace the placeholder `href="#"` links in `car-app/app/(admin)/components/SideNav.tsx`:

- Dashboard → `/admin/dashboard`
- vehicles → `/admin/cars`
- parts → `/admin/parts`
- inventory → `/admin/product-images`
- Orders → `/admin/orders`
- customers → `/admin/users`
- brands → `/admin/brands`
- car models → `/admin/car-models`
- categories → `/admin/categories`

Keep `data sync`, `promotions`, `analytics`, `settings`, `logout` at `href="#"`. Full replacement for the three sections:

```tsx
    <div className="p-[20px_0px_5px_10px] leading-none font-semibold h-auto title normal-case">Home</div>
    <li><Link href="/admin/dashboard">
      <span className="mif-apps icon"></span>
      <span className="title">Dashboard</span>
    </Link></li>
    <li><Link href="/admin/cars">
      <span className="mif-drive-eta icon"></span>
      <span className="title">vehicles</span>
    </Link></li>
    <li><Link href="/admin/parts">
      <span className="icon"><div className="w-full h-full" style={{
        maskImage: "url('/turbo-charger.png')",
        WebkitMaskImage: "url('/turbo-charger.png')",
        maskSize: "contain",
        WebkitMaskSize: "contain",
        maskRepeat: "no-repeat",
        WebkitMaskRepeat: "no-repeat",
        maskPosition: "center",
        WebkitMaskPosition: "center",
        background: "black"
      }} /></span>
      <span className="title">parts</span>
    </Link></li>
    <hr className="border" />
    <div className="p-[20px_0px_5px_10px] leading-none font-semibold h-auto title normal-case">Inventory</div>
    <li><Link href="/admin/product-images">
      <span className="mif-inbox icon"></span>
      <span className="title">inventory</span>
    </Link></li>
    <li><Link href="/admin/orders">
      <span className="mif-cart icon"></span>
      <span className="title">Orders</span>
    </Link></li>
    <li><Link href="/admin/users">
      <span className="mif-cogs icon"></span>
      <span className="title">customers</span>
    </Link></li>
    <li><Link href="/admin/brands">
      <span className="mif-cogs icon"></span>
      <span className="title">brands</span>
    </Link></li>
    <li><Link href="/admin/car-models">
      <span className="mif-cogs icon"></span>
      <span className="title">car models</span>
    </Link></li>
    <li><Link href="/admin/categories">
      <span className="mif-cogs icon"></span>
      <span className="title">categories</span>
    </Link></li>
    <li><Link href="#">
      <span className="mif-cogs icon"></span>
      <span className="title">data sync</span>
    </Link></li>
    <li><Link href="#">
      <span className="mif-cogs icon"></span>
      <span className="title">promotions</span>
    </Link></li>
```

(The Settings section: `analytics`, `settings`, `logout` stay at `href="#"` unchanged.)

- [ ] **Step 2: Verify**

Run (from `car-app`): `npx tsc --noEmit && npm run lint && npm run build`
Expected: PASS.

---

### Task 9: End-to-end verification

**Files:** none.

- [ ] **Step 1: Backend full check**

Run (from `car-backend`): `dotnet build CarEcommerce.slnx && dotnet test`
Expected: build success, all tests pass.

- [ ] **Step 2: Frontend full check**

Run (from `car-app`): `npx tsc --noEmit && npm run lint && npm run build`
Expected: all pass with no errors.

- [ ] **Step 3: Manual smoke test (with backend running)**

Start backend and `npm run dev` from `car-app`, log in as an Admin user, then verify:
- `/admin/users` lists users; Add creates a new admin (verify the created account can log in with the entered password); Edit renames + optionally resets password; Delete removes a user.
- `/admin/brands` create/edit/delete works and brand names appear in the car-models Brand dropdown.
- `/admin/cars` list shows cars; create and edit persist.
- `/admin/orders` read-only create button absent; edit changes status only.
- Pagination navigates on a large list.

Report any failure with exact steps to reproduce.
