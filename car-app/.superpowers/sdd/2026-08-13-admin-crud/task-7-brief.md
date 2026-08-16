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

