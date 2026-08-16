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

