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

