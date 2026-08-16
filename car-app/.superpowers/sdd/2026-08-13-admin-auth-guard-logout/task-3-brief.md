### Task 3: Admin route guard

**Files:**
- Create: `app/(admin)/components/RequireAdmin.tsx`
- Modify: `app/(admin)/layout.tsx`

**Interfaces:**
- Consumes: `useAuthStore.accessToken`, `useAuthStore.user` from Task 1.
- Produces: `RequireAdmin` client component rendering `children` only for authenticated admins; redirects logged-out (or user-less) → `/login`, non-admin → `/`.

- [ ] **Step 1: Create `app/(admin)/components/RequireAdmin.tsx`**

```tsx
"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuthStore } from "../../(main)/stores/auth-store";

export default function RequireAdmin({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const accessToken = useAuthStore((state) => state.accessToken);
  const user = useAuthStore((state) => state.user);

  useEffect(() => {
    if (!accessToken || !user) {
      router.replace("/login");
    } else if (user.role !== "Admin") {
      router.replace("/");
    }
  }, [accessToken, user, router]);

  if (!accessToken || !user || user.role !== "Admin") {
    return null;
  }

  return <>{children}</>;
}
```

Note: `!user` (tokens without user) also goes to `/login` — guards against stale pre-feature persisted sessions.

- [ ] **Step 2: Mount guard in `app/(admin)/layout.tsx`**

Import and wrap: `import RequireAdmin from "./components/RequireAdmin";` then change lines 28-34 so the SideNav/content block is wrapped:

```tsx
          <RequireAdmin>
            <div className="min-h-screen flex">
              <SideNav />
              <div className="w-full p-5">

                {children}

              </div>



            </div>
          </RequireAdmin>
```

- [ ] **Step 3: Verify typecheck**

Run: `npx tsc --noEmit`
Expected: no errors.

---

