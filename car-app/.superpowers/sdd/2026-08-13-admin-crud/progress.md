# SDD ledger — plan: car-app/docs/superpowers/plans/2026-08-13-admin-crud.md
Task 1: complete (commits 7ccd6de..4ba9bd9, review clean)
Task 1: minor (deferred): KeepsPassword test covers null but not blank/whitespace — plan-mandated, plan-identical.
Task 1: minor (deferred): email duplicate check case-sensitive, no trim — consistent with AuthService, pre-existing.
Task 2: complete (commits 4ba9bd9..26228a2, review clean)
Task 2: minor (deferred): stale brief note + pre-existing no-trailing-newline — non-issues.
Task 3: complete (adminApi.ts created, review clean — file-only, no commits, no git)
Task 3: minor (deferred): lookupCache key delimiter ':' unescaped (per-brief); cache never invalidated (per-brief).
Task 4: complete (adminEntities.ts created, review clean — file-only)
Task 4: minor (deferred): password key vs backend PasswordHash is non-issue (users writes go to /api/admins which accepts `password`); FK text fields w/o lookups cosmetic; buyerId hiddenOnCreate moot.
Task 5: fix round 1/5 (2 addressed, 0 open — useMemo dep + .catch; no git)
Task 5: complete (EntityFormModal.tsx, review clean; plan amended to match)
Task 6: complete (AdminTable.tsx, review clean — file-only)
Task 6: minor (deferred): stale-fetch race in list effect (per-brief, low impact, fast reload corrects).
Task 7: complete (AdminCrudPage.tsx + admin/[entity]/page.tsx, review clean — file-only)
Task 8: complete (SideNav.tsx, review clean — file-only)
Task 9: VERIFICATION COMPLETE. Backend: build clean, 59/59 tests. Frontend: tsc/lint/build clean, /admin/[entity] generated.
FINAL REVIEW (whole-branch): 1 Critical + 1 Important, both fixed.
  - Critical: GET /api/parts returned PagedResult<ProductResponse> (no name/brand/sku/desc/specs) -> admin parts edit wiped fields. Fixed: GetPartsAsync -> PagedResult<PartResponse>; endpoint rewired.
  - Important: UpdateCarAsync dropped Specs. Fixed: existing.Specs = car.Specs.
  - +2 regression tests (GetPartsAsync_ReturnsFullPartFields, UpdateCarAsync_PreservesSpecs). Suite 59/59.
  - Backend commit 09c2a12.
  - Deferred minors (safe): blank-pw test gap, case-sensitive email, cache-key ':'; cache invalidation, FK text fields, stale-fetch race, blank number->0, password-not-required-on-create.
MANUAL SMOKE (user): not run - needs running backend+frontend+admin login.
