# Admin CRUD Panel — Design

Date: 2026-08-13
Status: Approved

## Goal

Give the admin panel (`app/(admin)`) working CRUD pages for all backend entities. Currently the admin area has only a static dashboard and placeholder sidebar links.

## Scope

- Config-driven CRUD framework so each entity is one config object, not bespoke code.
- Entities wired: users, brands, car-models, categories, cars, parts, orders, order-items, reviews, product-images, cart-items, favorites.
- Small backend addition: admin user create/update endpoint that bcrypt-hashes passwords server-side.

## Architecture

```
AdminCrudPage (client)
  ├── AdminTable          — paged table + row actions + Add button
  │     └── Pagination    — existing app/(main)/components/Pagination
  └── EntityFormModal     — react-hook-form + zod, generated from config
        └── adminApi      — axios CRUD helpers (auth handled by existing interceptor)
```

Registry: `app/(admin)/lib/adminEntities.ts` holds one config per entity. Dynamic route `app/(admin)/admin/[entity]/page.tsx` resolves a config by slug and renders `AdminCrudPage`.

### Entity config shape

```ts
interface EntityField {
  key: string;              // JSON property name
  label: string;
  type: "text" | "email" | "password" | "number" | "decimal" | "checkbox" | "textarea" | "select";
  required?: boolean;
  showInTable?: boolean;    // default true
  options?: { value: string; label: string }[];        // static select options
  lookup?: { route: string; valueKey: string; labelKey: string; params?: Record<string, string> };
  hiddenOnCreate?: boolean; // e.g. id
  hiddenOnEdit?: boolean;
}

interface EntityConfig {
  slug: string;             // URL segment, e.g. "users"
  route: string;            // API path, e.g. "users"
  label: string;            // plural title, e.g. "Users"
  fields: EntityField[];
  canCreate?: boolean;      // default true
  canEdit?: boolean;        // default true
  canDelete?: boolean;      // default true
  userAuth?: boolean;       // use /api/admins for writes (users only)
}
```

### Generic endpoint contract (backend)

- `GET /api/{route}?pageNumber&pageSize` → `PagedResult<T>` with `{ items, totalCount, pageNumber, pageSize }`
- `POST /api/{route}` body `{ data: T }`
- `PUT /api/{route}/{id}` body `{ id, data: T }`
- `DELETE /api/{route}/{id}`
- Cars: `GET /api/cars` (returns raw `Car` in `PagedResult`), `POST/PUT/DELETE /api/cars`
- Parts: `GET /api/parts`, `POST/PUT/DELETE /api/parts`

### Admin user endpoints (new backend)

- `POST /api/admins` — Admin role. Body `{ email, password, name, role }`. Bcrypt-hashes password, creates user. 409 if email exists. Returns `UserResponse`.
- `PUT /api/admins/{id}` — Admin role. Body `{ email, name, role, password? }`. Updates fields; re-hashes only if `password` present.
- List/delete: reuse generic `/api/users`.

## Component behavior

### AdminTable

- Client component. Props: `config`.
- On mount and on page change: `GET /api/{route}?pageNumber=N&pageSize=20`.
- Renders one column per field with `showInTable !== false`. Values: checkbox → yes/no, select/lookup → label resolution (lookup values loaded once), else raw value.
- Row actions: Edit (opens modal), Delete (confirm dialog, then DELETE, refresh list).
- Header: title + "Add" button (opens modal, hidden if `canCreate === false`).
- Uses existing `Pagination` component.

### EntityFormModal

- Client component. Props: `config`, `editing` (row or null), `open`, `onClose`, `onSaved`.
- Builds zod schema from config (required check, email format, number/decimal coercion).
- react-hook-form + `zodResolver`. Field rendering by type:
  - `select`: static `options` or fetched from `lookup.route` (fetched once on open, cached per session).
  - `password`: shown only for users create/edit; blank on edit = unchanged.
  - `checkbox` → boolean, `number`/`decimal` → coerced numbers.
- Submit: `canCreate`/`canEdit` path → POST/PUT. Users use `/api/admins` for writes.
- On save: call `onSaved()`, parent refreshes table and closes modal.
- On failure: inline error message (Metro UI alert). No toast system exists; keep minimal.

### adminApi helpers

```ts
list(route, page, pageSize)          → GET
create(route, data)                  → POST { data }
update(route, id, data)              → PUT { id, data }
remove(route, id)                    → DELETE
listAdmins(page, pageSize)           → GET /api/users (for users table)
createAdmin(data)                    → POST /api/admins
updateAdmin(id, data)                → PUT /api/admins/{id}
```

## Field configs per entity

- **users**: email, password (create only, `showInTable: false`), role (select: Admin/Customer/Staff), name. PasswordHash never rendered.
- **brands**: name (required), country.
- **car-models**: brandId (select lookup `/api/brands`, valueKey id, labelKey name), name (required), yearStart, yearEnd.
- **categories**: name (required), slug (required), type (select Car/Part).
- **cars**: brandId (lookup brands), modelId (lookup car-models), year, price, mileage, color, categoryId (lookup categories), status (select Active/Inactive), quantity, description, specs. Create/edit through `/api/cars`.
- **parts**: name, brand, sku, price, quantity, status, categoryId (lookup categories), description, specs. Create/edit through `/api/parts`.
- **orders**: buyerId, status, totalAmount. Edit only status.
- **order-items**: orderId, productId, quantity, unitPrice. (Read-mostly.)
- **reviews**: productId, userId, rating, comment. (Read-mostly.)
- **product-images**: productId, url, isPrimary.
- **cart-items**: userId, productId, quantity.
- **favorites**: userId, productId. (Read-mostly.)

## Sidebar

`SideNav.tsx`: replace `href="#"` placeholders with real links:
- Dashboard → `/admin/dashboard`
- vehicles → `/admin/cars`
- parts → `/admin/parts`
- inventory → `/admin/product-images`
- Orders → `/admin/orders`
- customers → `/admin/users`
- brands, car models, categories → `/admin/brands`, `/admin/car-models`, `/admin/categories`
- analytics/settings/data sync/promotions: leave `#` placeholders.

## Data flow

1. User navigates `/admin/{slug}` → page resolves config.
2. Table lists page 1, 20 rows.
3. Add/Edit → modal → POST/PUT → close → refetch.
4. Delete → confirm → DELETE → refetch.

## Error handling

- Existing axios interceptor: attaches bearer token, auto-refreshes on 401.
- Non-401 failures: modal shows inline message.
- Delete confirmation: window-style confirm (Metro UI) or native confirm — native `confirm()` kept minimal.

## Security notes

- `GET /api/users` returns `passwordHash` in JSON (pre-existing backend issue). Frontend never renders it; config excludes it from table and form. Fixing is out of scope.

## Testing

- No frontend test framework present. Verification:
  - `npm run lint`
  - `npx tsc --noEmit`
  - `npm run build`
- Backend: build + existing tests (`dotnet test`) unaffected; new admin endpoints covered by build success.

## Out of scope

- Toast/notification system.
- Client-side search/filter across list (server has none for generic entities).
- Editing navigation/related collections (e.g. product images inside car form).
- Fixing `passwordHash` exposure in `/api/users`.
