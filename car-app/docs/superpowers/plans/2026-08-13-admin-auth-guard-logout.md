# Admin Route Guard + Logout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gate `/admin/*` routes to authenticated users with role `Admin`, persist the user from the login response, and wire the admin SideNav logout button.

**Architecture:** Store `user` (incl. `role`) alongside tokens in the existing zustand auth store (persisted to localStorage). Add a client `RequireAdmin` guard mounted in the admin layout that redirects logged-out users to `/login` and non-admins to `/`. Login/register persist `data.user`; login redirects admins to `/admin/dashboard`. SideNav logout clears the store and sends the user to `/login`.

**Tech Stack:** Next.js 16 app router, TypeScript, zustand, `useRouter` from `next/navigation`.

## Global Constraints

- **Read `node_modules/next/dist/docs/` before writing code** (Next 16 breaking changes; AGENTS.md mandate).
- Client-side redirects must use `useRouter().push()/replace()` from `next/navigation` — NOT `redirect()` (that's server-only). `redirect()` cannot be called in client event handlers or effects.
- Auth lives in `app/(main)/stores/auth-store.ts`, persisted via zustand `persist` + `createJSONStorage(() => localStorage)` (synchronous rehydration — guard has no redirect flash).
- Backend role values are exactly `"Admin"`, `"Seller"`, `"Customer"`. Login/register response shape: `{ accessToken, refreshToken, expiresAt, user: { id, email, role, name, createdAt } }`.
- `car-app` is NOT a git repo — no commit steps. Verify each task with `npx tsc --noEmit`; final task runs lint + build.
- No test framework in `car-app` — verification is typecheck/build, not unit tests.
- Store must be imported from `app/(main)/stores/auth-store` (relative `../../(main)/stores/auth-store` from `app/(admin)/components`).
- Do NOT add comments to code (existing files have them; don't add new ones).

---

### Task 1: Persist user in auth store

**Files:**
- Modify: `app/(main)/stores/auth-store.ts` (entire file)

**Interfaces:**
- Consumes: nothing new.
- Produces: `AuthUser { id, email, role, name }` interface, `user: AuthUser | null`, `setUser(user: AuthUser): void` in `useAuthStore`. `logout()` now clears `user` too.

- [ ] **Step 1: Rewrite `app/(main)/stores/auth-store.ts`**

Replace the whole file with:

```ts
import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

export interface AuthUser {
  id: string;
  email: string;
  role: string;
  name: string;
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: AuthUser | null;
  setTokens: (accessToken: string, refreshToken?: string) => void;
  setUser: (user: AuthUser) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,

      setTokens: (accessToken: string, refreshToken?: string) =>
        set((state) => ({
          accessToken,
          refreshToken: refreshToken ?? state.refreshToken,
        })),

      setUser: (user: AuthUser) => set({ user }),

      logout: () => set({ accessToken: null, refreshToken: null, user: null }),
    }),
    {
      name: 'auth-storage',
      storage: createJSONStorage(() => localStorage),
    }
  )
);
```

- [ ] **Step 2: Verify typecheck**

Run: `npx tsc --noEmit`
Expected: no errors related to auth-store (pre-existing warnings in `tsc` output are unrelated — use the `--noEmit` exit, and note lint warnings are separate).

---

### Task 2: Login + register persist user and role-redirect

**Files:**
- Modify: `app/(main)/login/page.tsx`
- Modify: `app/(main)/register/page.tsx`

**Interfaces:**
- Consumes: `useAuthStore.setUser`, `useAuthStore.user`, `AuthUser` from Task 1.
- Produces: login redirects role `Admin` → `/admin/dashboard`, others → `/`.

- [ ] **Step 1: Update `app/(main)/login/page.tsx`**

Changes:
1. Add `const setUser = useAuthStore((state) => state.setUser);` and `const user = useAuthStore((state) => state.user);` next to the existing `setTokens`/`accessToken` selectors (lines 20-21).
2. In `onSubmit` (line 25), after `setTokens(data.accessToken, data.refreshToken)` add:

```ts
      setUser(data.user);
      if (data.user?.role === "Admin") {
        router.push("/admin/dashboard");
      } else {
        router.push("/");
      }
```

3. Replace the existing `useEffect` (lines 34-38) with:

```ts
  useEffect(() => {
    if (accessToken) {
      router.replace(user?.role === "Admin" ? "/admin/dashboard" : "/");
    }
  }, [accessToken, user, router]);
```

- [ ] **Step 2: Update `app/(main)/register/page.tsx`**

1. Add `const setUser = useAuthStore((state) => state.setUser);` next to the `setTokens` selector (line 21).
2. In `onSubmit` (line 27), after `setTokens(data.accessToken, data.refreshToken)` add:

```ts
      setUser(data.user);
      router.push("/");
```

(New registrations are always role `Customer`; the existing `router.push("/")` stays.)

- [ ] **Step 3: Verify typecheck**

Run: `npx tsc --noEmit`
Expected: no errors.

---

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

### Task 4: Wire SideNav logout + full verification

**Files:**
- Modify: `app/(admin)/components/SideNav.tsx`

**Interfaces:**
- Consumes: `useAuthStore.logout` from Task 1.
- Produces: logout link navigates to `/login` and clears auth state.

- [ ] **Step 1: Update `app/(admin)/components/SideNav.tsx`**

1. Add imports (top of file, after the `useState` import):

```tsx
import { useRouter } from "next/navigation";
import { useAuthStore } from "../../(main)/stores/auth-store";
```

2. Inside the component (after `const [expandSide, setExpandSide] = useState(true);`):

```tsx
  const router = useRouter();
  const logout = useAuthStore((state) => state.logout);

  const handleLogout = () => {
    logout();
    router.push("/login");
  };
```

3. Replace the dead logout link (lines 88-91) — keep the Metro `<li><Link>` structure and `mif-exit` icon, change `href="#"` to `href="/login"` and add the handler:

```tsx
    <li><Link href="/login" onClick={handleLogout}>
      <span className="mif-exit icon"></span>
      <span className="title">logout</span>
    </Link></li>
```

- [ ] **Step 2: Typecheck**

Run: `npx tsc --noEmit`
Expected: no errors.

- [ ] **Step 3: Lint**

Run: `npm run lint`
Expected: 0 errors (77 pre-existing warnings are acceptable).

- [ ] **Step 4: Build**

Run: `npm run build`
Expected: builds clean; routes include `/admin/dashboard` and `/admin/[entity]`.

- [ ] **Step 5: Manual smoke (user)**

- Login as customer → visit `/admin/dashboard` → bounced to `/`.
- Login as admin → lands on `/admin/dashboard`; logout link in sidebar → `/login`; tokens + user cleared in localStorage (`auth-storage`).
- Logged-out visit to `/admin/dashboard` → `/login`.
