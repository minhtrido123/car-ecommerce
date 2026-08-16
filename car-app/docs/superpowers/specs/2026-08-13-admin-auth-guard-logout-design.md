# Admin Route Guard + Logout — Design

Date: 2026-08-13

## Problem

- `/admin/*` routes are not client-guarded. Any logged-in user (or guest) can open them; only the API's `Admin` role requirement stops writes.
- Admin SideNav has a dead "logout" link (`href="#"`), while the main Navbar already wires `logout()`.
- The auth store keeps tokens only — the user object (and `role`) from the login response is discarded.

## Decisions (user-confirmed)

- Guard redirects: logged-out → `/login`; logged-in non-admin → `/`.
- Post-login: `role === "Admin"` → `/admin/dashboard`; customers → `/`.

## Backend contract (no backend changes)

`POST /api/auth/login` returns `{ accessToken, refreshToken, expiresAt, user: { id, email, role, name, createdAt } }`. Role values: `Admin`, `Seller`, `Customer`. Register returns the same shape.

## Architecture

### 1. `app/(main)/stores/auth-store.ts`

- Add `user: { id, email, role, name } | null` and `setUser(user)`.
- `logout()` clears tokens and user.
- `user` included in zustand `persist` (localStorage) so role survives refresh and the guard has no redirect flash (hydrates synchronously).

### 2. `app/(main)/login/page.tsx`

- Persist `data.user` from login response.
- Redirect: `user.role === "Admin"` → `/admin/dashboard`, else `/`.

### 3. `app/(main)/register/page.tsx`

- Persist `data.user` (response is AuthResponse; new accounts are always `Customer`).
- Redirect `/`.

### 4. `app/(admin)/components/RequireAdmin.tsx` (client component)

- Reads auth store.
- No `accessToken` → `router.replace("/login")`.
- `user?.role !== "Admin"` → `router.replace("/")`.
- Else renders children.
- Mounted in `app/(admin)/layout.tsx` wrapping SideNav + content.

### 5. `app/(admin)/components/SideNav.tsx`

- Replace dead logout link (lines 88-91) with a real handler: `logout()` then `router.push("/login")`.

## Error handling

- Stale role after 401 refresh failure: `authService.ts` already clears tokens and redirects `/login` — unchanged.
- No other failure modes (guard is synchronous store read).

## Testing / verification

- Frontend has no test framework. Verify with `npx tsc --noEmit`, `npm run lint`, `npm run build`.
- Manual: login as customer → `/admin/dashboard` redirects to `/`; logout → login page. Login as admin → `/admin/dashboard`; logout → login page.

## Out of scope

- Backend changes.
- Admin link on the main Navbar.
- Session-timeout UX beyond existing refresh-failure redirect.
