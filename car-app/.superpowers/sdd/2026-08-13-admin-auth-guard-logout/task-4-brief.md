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
