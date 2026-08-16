# Task 4 Report: Wire SideNav logout + full verification

**Status:** DONE_WITH_CONCERNS

## Changes

**File modified:** `app/(admin)/components/SideNav.tsx`

### Block 1 — imports (BEFORE / AFTER)

BEFORE:

```tsx
import Link from "next/link";
import { useState } from "react";

export default function SideNav() {
  const [expandSide, setExpandSide] = useState(true);
```

AFTER:

```tsx
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useAuthStore } from "../../(main)/stores/auth-store";

export default function SideNav() {
  const [expandSide, setExpandSide] = useState(true);
  const router = useRouter();
  const logout = useAuthStore((state) => state.logout);

  const handleLogout = () => {
    logout();
    router.push("/login");
  };
```

### Block 2 — logout link (BEFORE / AFTER)

BEFORE:

```tsx
    <li><Link href="#">
      <span className="mif-exit icon"></span>
      <span className="title">logout</span>
    </Link></li>
```

AFTER:

```tsx
    <li><Link href="/login" onClick={handleLogout}>
      <span className="mif-exit icon"></span>
      <span className="title">logout</span>
    </Link></li>
```

All other nav items preserved byte-for-byte. No comments added. Client event handler uses `useRouter().push()` per `node_modules/next/dist/docs/01-app/02-guides/redirecting.md` (server-only `redirect()` avoided).

## Verification

### Typecheck

Command: `npx tsc --noEmit`

Output tail: empty (no errors), exit code 0.

### Lint

Command: `npm run lint`

Output tail:

```
/home/trido/car-ecommerce/car-app/app/(main)/components/RangeFilter.tsx
  36:41  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any
...
✖ 78 problems (1 error, 77 warnings)
LINT_EXIT=1
```

1 error + 77 warnings. The error is at `app/(main)/components/RangeFilter.tsx:36` (`onValueChange={([newLo, newHi]: any) =>`), pre-existing and unrelated to this task (SideNav.tsx contributes only 2 pre-existing `<img>` warnings at line 23). Exit code 1 is driven by this pre-existing error, not by Task 4 changes. This diverges from the brief's expectation of "0 errors, 77 warnings".

### Build

Command: `npm run build`

Output tail:

```
▲ Next.js 16.2.10 (Turbopack)
- Environments: .env.local

  Creating an optimized production build ...
✓ Compiled successfully in 16.9s
  Running TypeScript ...
  Finished TypeScript in 12.8s ...
  Collecting page data using 3 workers ...
  Generating static pages using 3 workers (9/9) in 725ms
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

Clean build; routes include `/admin/dashboard` and `/admin/[entity]`.

### Manual smoke (user)

Not run — requires interactive browser session (login flows). Pending per brief Step 5.

## Self-review

- [x] `handleLogout` calls `logout()` (clears `accessToken`, `refreshToken`, `user` in `auth-storage` via zustand persist) then `router.push("/login")`.
- [x] Dead logout link now points to `/login` with `onClick={handleLogout}`; Metro `<li><Link>` structure and `mif-exit` icon preserved.
- [x] All other nav items untouched (verified against original file).
- [x] No comments added.
- [x] `useRouter().push()` from `next/navigation` used in event handler per Next 16 docs; no server `redirect()`.
- [x] Import path `../../(main)/stores/auth-store` resolves (tsc/build pass).
- [x] Not a git repo; no commit attempted.

## Concerns

1. `npm run lint` exits 1 due to a pre-existing `no-explicit-any` error in `app/(main)/components/RangeFilter.tsx:36`, not introduced by this task. 77 warnings are pre-existing as expected. Flagging for awareness; fixing RangeFilter is out of scope for Task 4.
2. Manual smoke (brief Step 5) not performed — requires running app + browser.
