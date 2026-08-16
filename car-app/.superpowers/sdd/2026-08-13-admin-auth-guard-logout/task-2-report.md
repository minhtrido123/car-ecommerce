# Task 2 Report: Login + register persist user and role-redirect

**Status:** DONE
**Date:** 2026-08-13
**Verify:** `npx tsc --noEmit` — passed with no errors (no output, exit 0).

## Files changed

- `app/(main)/login/page.tsx`
- `app/(main)/register/page.tsx`

No other files touched. Not a git repo — no commit made.

---

## Step 1: `app/(main)/login/page.tsx`

### Selectors (lines 20-23)

Before:
```ts
  const router = useRouter();
  const setTokens = useAuthStore((state) => state.setTokens);
  const accessToken = useAuthStore((state) => state.accessToken);
```

After:
```ts
  const router = useRouter();
  const setTokens = useAuthStore((state) => state.setTokens);
  const accessToken = useAuthStore((state) => state.accessToken);
  const setUser = useAuthStore((state) => state.setUser);
  const user = useAuthStore((state) => state.user);
```

### `onSubmit` (lines 25-39)

Before:
```ts
  const onSubmit = async (input: LoginForm) => {
    try {
      const { data } = await api.post(`/auth/login`, input);
      setTokens(data.accessToken, data.refreshToken)
      router.push("/")
      // console.log(data)
    }
    catch (err) {
      // console.log(err);
    }
  };
```

After:
```ts
  const onSubmit = async (input: LoginForm) => {
    try {
      const { data } = await api.post(`/auth/login`, input);
      setTokens(data.accessToken, data.refreshToken)
      setUser(data.user);
      if (data.user?.role === "Admin") {
        router.push("/admin/dashboard");
      } else {
        router.push("/");
      }
    }
    catch (err) {
      // console.log(err);
    }
  };
```

### `useEffect` (lines 40-44)

Before:
```ts
  useEffect(() => {
    if (accessToken) {
      router.replace("/");
    }
  }, [accessToken, router]);
```

After:
```ts
  useEffect(() => {
    if (accessToken) {
      router.replace(user?.role === "Admin" ? "/admin/dashboard" : "/");
    }
  }, [accessToken, user, router]);
```

---

## Step 2: `app/(main)/register/page.tsx`

### Selector (line 23)

Before:
```ts
  const router = useRouter();
  const setTokens = useAuthStore((state) => state.setTokens);
  const accessToken = useAuthStore((state) => state.accessToken);
```

After:
```ts
  const router = useRouter();
  const setTokens = useAuthStore((state) => state.setTokens);
  const accessToken = useAuthStore((state) => state.accessToken);
  const setUser = useAuthStore((state) => state.setUser);
```

### `onSubmit` (lines 25-36)

Before:
```ts
  const onSubmit = async (input: LoginForm) => {
    try {
      const { data } = await api.post(`/auth/register`, input);
      setTokens(data.accessToken, data.refreshToken)
      router.push("/")
      // console.log(data)
    }
    catch (err) {
      // console.log(err);
    }
  };
```

After:
```ts
  const onSubmit = async (input: LoginForm) => {
    try {
      const { data } = await api.post(`/auth/register`, input);
      setTokens(data.accessToken, data.refreshToken)
      setUser(data.user);
      router.push("/")
      // console.log(data)
    }
    catch (err) {
      // console.log(err);
    }
  };
```

`useEffect` (redirect to "/") intentionally unchanged per brief.

---

## Step 3: Typecheck

Command (run in `/home/trido/car-ecommerce/car-app`):

```
npx tsc --noEmit
```

Output:

```
(no output)
```

Exit code: 0. No errors — no NEW errors introduced.

---

## Step 4: Self-review

| Check | Result |
| --- | --- |
| Login persists user via `setUser(data.user)` | PASS |
| Register persists user via `setUser(data.user)` | PASS |
| Admin login lands on `/admin/dashboard` (both `onSubmit` push and `useEffect` replace) | PASS |
| Non-admin login still lands on `/` | PASS |
| Register redirect stays `/` | PASS |
| No dead code — `setUser` used in both pages; `user` selector used in login `useEffect`; `accessToken` still used in both | PASS |
| Existing structure preserved — imports, JSX, `useEffect` shape, role comparison `"Admin"` exact per backend | PASS |
| Next 16 breaking-change compliance — client event handlers use `useRouter().push()` from `next/navigation`, `redirect()` server-only NOT used | PASS (confirmed against `node_modules/next/dist/docs/01-app/02-guides/redirecting.md`) |
| No comments added | PASS |

## Concerns

None.
