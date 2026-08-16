### Task 9: End-to-end verification

**Files:** none.

- [ ] **Step 1: Backend full check**

Run (from `car-backend`): `dotnet build CarEcommerce.slnx && dotnet test`
Expected: build success, all tests pass.

- [ ] **Step 2: Frontend full check**

Run (from `car-app`): `npx tsc --noEmit && npm run lint && npm run build`
Expected: all pass with no errors.

- [ ] **Step 3: Manual smoke test (with backend running)**

Start backend and `npm run dev` from `car-app`, log in as an Admin user, then verify:
- `/admin/users` lists users; Add creates a new admin (verify the created account can log in with the entered password); Edit renames + optionally resets password; Delete removes a user.
- `/admin/brands` create/edit/delete works and brand names appear in the car-models Brand dropdown.
- `/admin/cars` list shows cars; create and edit persist.
- `/admin/orders` read-only create button absent; edit changes status only.
- Pagination navigates on a large list.

Report any failure with exact steps to reproduce.
