# Task 7 Report: Frontend — `AdminCrudPage` + dynamic route `/admin/[entity]`

## What I implemented

Created two files, transcribed verbatim from the task brief:

1. `car-app/app/(admin)/components/AdminCrudPage.tsx`
   - Client component wrapper.
   - Owns `modal` state (`{ mode: "create" | "edit"; row: Row | null } | null`) and `refreshKey` counter.
   - Renders `AdminTable` with `key={refreshKey}`, wiring `onAdd` → open create modal, `onEdit` → open edit modal with the row.
   - Conditionally renders `EntityFormModal` only while `modal` is non-null (`{modal && ...}`) — satisfies the required contract that the modal unmounts on close so its internal form state resets between opens.
   - `onSaved={refresh}` bumps `refreshKey`, forcing `AdminTable` remount + reload after create/update.

2. `car-app/app/(admin)/admin/[entity]/page.tsx`
   - Client page component.
   - Next.js 16 dynamic route: `params: Promise<{ entity: string }>`, resolved via `use(params)` from `react` (verified against `node_modules/next/dist/docs/01-app/03-api-reference/03-file-conventions/dynamic-routes.md` — documented Client Component pattern).
   - Resolves config via `adminEntities[entity]`; renders 404-style fallback ("Unknown section" + "Back to dashboard" link to `/admin/dashboard`) for unknown slugs; otherwise renders `<AdminCrudPage config={config} />`.

## Verification evidence

### `npx tsc --noEmit` (from `car-app`)
```
TSC_EXIT=0
```
PASS — no type errors.

### `npm run lint` (from `car-app`)
```
✖ 77 problems (0 errors, 77 warnings)
LINT_EXIT=0
```
PASS — 0 errors. All 77 warnings are pre-existing and located in other files (`SideNav.tsx`, `(main)/*`, `layout.tsx`, etc.); neither of my two new files appears in the warning output.

## Files changed

- Created: `car-app/app/(admin)/components/AdminCrudPage.tsx`
- Created: `car-app/app/(admin)/admin/[entity]/page.tsx`

No existing files modified.

## Self-review findings

- Code matches brief byte-for-byte.
- Import paths verified:
  - `(admin)/admin/[entity]/page.tsx` → `../../lib/adminEntities` and `../../components/AdminCrudPage` both resolve to `(admin)/` scope. Correct.
  - `(admin)/components/AdminCrudPage.tsx` → `../lib/adminEntities`, `./AdminTable`, `./EntityFormModal`. Correct.
- Props match consumed interfaces:
  - `AdminTable` requires `{ config, onEdit: (row: Row) => void, onAdd: () => void }` — `AdminCrudPage` supplies all three. ✓
  - `EntityFormModal` requires `{ config, mode, editing, onClose, onSaved }` — all supplied; `editing={modal.row}` typed `Row | null`. ✓
- `AdminTable` accepts a `key` prop (remount trick) — valid on any React component. ✓
- `use(params)` used for async `params` (Next 16 contract). No deprecated synchronous `params` usage. ✓
- No `any`/unsafe types introduced. No unused imports in my files.

## Concerns

- None. No runtime smoke test was run (would require dev server + backend); verification is static (tsc + lint). All consumed components (Tasks 3–6) existed and typecheck together.
