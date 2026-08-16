# Task 1: Persist user in auth store

Goal context: Admin route guard feature. Store `user` (incl. `role`) alongside tokens so the client can gate `/admin/*`.

## Global Constraints
- Read `node_modules/next/dist/docs/` before writing code (Next 16 breaking changes; AGENTS.md mandate).
- car-app is NOT a git repo — no commit steps. Verify with `npx tsc --noEmit`.
- No test framework — verification is typecheck, not unit tests.
- Do NOT add comments to code.

## Files
- Modify: `app/(main)/stores/auth-store.ts` (entire file)

## Interfaces
- Produces: `AuthUser { id, email, role, name }` interface, `user: AuthUser | null`, `setUser(user: AuthUser): void` in `useAuthStore`. `logout()` now clears `user` too.

## Steps

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
Expected: no errors related to auth-store (pre-existing warnings in tsc output are unrelated).
