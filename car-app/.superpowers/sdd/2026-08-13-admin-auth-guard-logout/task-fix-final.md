# Final Review Fix Report: RequireAdmin hydration + stale persist dead-end

**Status:** DONE

## Fix 1 — Hydration mismatch on hard reload of /admin/*

**File modified:** `app/(admin)/components/RequireAdmin.tsx`

BEFORE:

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

AFTER:

```tsx
"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuthStore } from "../../(main)/stores/auth-store";

export default function RequireAdmin({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const [mounted, setMounted] = useState(false);
  const accessToken = useAuthStore((state) => state.accessToken);
  const user = useAuthStore((state) => state.user);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!mounted) return;
    if (!accessToken || !user) {
      router.replace("/login");
    } else if (user.role !== "Admin") {
      router.replace("/");
    }
  }, [mounted, accessToken, user, router]);

  if (!mounted || !accessToken || !user || user.role !== "Admin") {
    return null;
  }

  return <>{children}</>;
}
```

Both server render and first client render emit `null` before `useEffect` fires; client only mounts children after `mounted` flips true, so server HTML === client output — hydration mismatch resolved.

## Fix 2 — Stale persisted storage dead-end

**File modified:** `app/(main)/stores/auth-store.ts`

BEFORE:

```ts
    {
      name: 'auth-storage',
      storage: createJSONStorage(() => localStorage),
    }
```

AFTER:

```ts
    {
      name: 'auth-storage',
      storage: createJSONStorage(() => localStorage),
      version: 1,
      migrate: () => ({ accessToken: null, refreshToken: null, user: null }),
    }
```

Persist config now declares `version: 1`; any stored state without that version triggers `migrate`, which resets to a clean logged-out state (no accessToken, no user). Old `{ accessToken, refreshToken }` payloads are wiped once, forcing a one-time re-login and eliminating the /admin → /login → (accessToken present) bounce dead-end. `setTokens`/`setUser`/`logout` logic untouched.

## Verification

### Typecheck

Command: `npx tsc --noEmit`

Output tail: empty (no errors), exit code 0.

### Build

Command: `npm run build`

Output tail:

```
▲ Next.js 16.2.10 (Turbopack)
- Environments: .env.local

  Creating an optimized production build ...
✓ Compiled successfully in 20.9s
  Running TypeScript ...
  Finished TypeScript in 14.8s ...
  Collecting page data using 3 workers ...
  Generating static pages using 3 workers (0/9) ...
  Generating static pages using 3 workers (2/9)
  Generating static pages using 3 workers (4/9)
  Generating static pages using 3 workers (6/9)
✓ Generating static pages using 3 workers (9/9) in 825ms
  Finalizing page optimization ...

Route (app)
┌ ○ /
├ ○ /_not-found
├ ƒ /admin/[entity]
├ ○ /admin/dashboard
├ ○ /car-parts
├ ○ /cars
├ ƒ /cars/[id]
├ ○ /login
├ ○ /register
└ ○ /windows


○  (Static)   prerendered as static content
ƒ  (Dynamic)  server-rendered on demand

BUILD_EXIT=0
```

Clean build. `npm run lint` intentionally not run (exits 1 on pre-existing RangeFilter.tsx error, out of scope).

## Self-review

- [x] `RequireAdmin.tsx` matches the specified fix byte-for-byte: `useState` imported, `mounted` state + mount effect added, redirect effect early-returns when `!mounted`, render guard returns null when `!mounted`.
- [x] `auth-store.ts` persist config gains `version: 1` + `migrate` resetting to `{ accessToken: null, refreshToken: null, user: null }`; `setTokens`/`setUser`/`logout` untouched.
- [x] Both findings from final review addressed (hydration mismatch; stale persisted storage dead-end).
- [x] `npx tsc --noEmit` exit 0, `npm run build` exit 0 — no new errors.
- [x] No comments added. Not a git repo; no commit attempted.
