# Task 3 Report: Admin route guard

**Date:** 2026-08-13
**Status:** DONE

## Summary

- Created `app/(admin)/components/RequireAdmin.tsx` — client component guard.
- Mounted `<RequireAdmin>` in `app/(admin)/layout.tsx` wrapping SideNav/content block.
- `npx tsc --noEmit` passes with no new errors.

## Changes

### 1. New file: `app/(admin)/components/RequireAdmin.tsx` (full content)

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

Verbatim match to brief Step 1 code block. Import path `../../(main)/stores/auth-store` resolves: from `app/(admin)/components/`, `../..` → `app/`, then `(main)/stores/auth-store`. Parentheses in path confirmed valid by tsc.

### 2. Modified: `app/(admin)/layout.tsx`

**Before:**

```tsx
// app/(admin)/layout.tsx
import { Metadata } from "next";
import MetroProvider from "../(main)/components/MetroProvider";
import GlobalSpinner from "../(main)/components/Spinner";
import "../globals.css";
import Link from "next/link";
import SideNav from "./components/SideNav";

export const metadata: Metadata = {
  title: "Supra",
  description: "Car website",
};

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html
      lang="en"
      // className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
      className={`h-full antialiased`}
    >
      <body>
        <MetroProvider>
          <GlobalSpinner />
          <div className="min-h-screen flex">
            <SideNav />
            <div className="w-full p-5">

              {children}

            </div>



          </div>
        </MetroProvider>
      </body>
    </html>

  );
}
```

**After:**

```tsx
// app/(admin)/layout.tsx
import { Metadata } from "next";
import MetroProvider from "../(main)/components/MetroProvider";
import GlobalSpinner from "../(main)/components/Spinner";
import "../globals.css";
import Link from "next/link";
import SideNav from "./components/SideNav";
import RequireAdmin from "./components/RequireAdmin";

export const metadata: Metadata = {
  title: "Supra",
  description: "Car website",
};

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html
      lang="en"
      // className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
      className={`h-full antialiased`}
    >
      <body>
        <MetroProvider>
          <GlobalSpinner />
          <RequireAdmin>
            <div className="min-h-screen flex">
              <SideNav />
              <div className="w-full p-5">

                {children}

              </div>



            </div>
          </RequireAdmin>
        </MetroProvider>
      </body>
    </html>

  );
}
```

Diff: added import line 8; wrapped the `<div className="min-h-screen flex">...</div>` block in `<RequireAdmin>...</RequireAdmin>`. All other JSX, attributes, blank lines preserved verbatim.

## Verification

Command (run in `/home/trido/car-ecommerce/car-app`):

```
npx tsc --noEmit
```

Output: none (exit 0). No new errors. No errors at all.

## Self-review

| Check | Result |
|-------|--------|
| Logged-out or user-less (`!accessToken \|\| !user`) → `/login` via `router.replace` in `useEffect` | PASS |
| Non-admin (`user.role !== "Admin"`) → `/` | PASS |
| Admins → `children` rendered | PASS |
| `useRouter().replace()` used in client component; `redirect()` (server-only) NOT used | PASS — per `node_modules/next/dist/docs/01-app/02-guides/redirecting.md` (client components redirect via `useRouter`) |
| Layout structure intact (html/body/MetroProvider/GlobalSpinner/RequireAdmin nesting) | PASS |
| Guard receives `children` from server layout (server→client children passing, no server import into client) | PASS |
| SSR safety: guard returns `null` during server render (no accessToken yet), no `localStorage` access on server; client re-hydrates and redirects | PASS |
| Role comparison is exact string `"Admin"` (case-sensitive), matching `AuthUser.role: string` from Task 1 | PASS |
| No comments added | PASS |
| car-app is not a git repo; nothing committed | PASS |

## Concerns

None. Brief followed verbatim.
