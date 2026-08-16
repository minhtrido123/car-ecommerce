# Admin Image Drag-Drop Editing + Dynamic Website Menu Design

**Date:** 2026-08-14

## Problem

1. Admin edit forms for cars and parts have no image management. Product images
   are edited as raw URL text in a separate "product-images" section, with no
   thumbnails, no upload, no ordering, no primary control beyond a checkbox.
2. The website navbar hardcodes its menu links (Cars, Car Parts, Blogs). There is
   no way to add, edit, or reorder menu items.

## Goal

- Car and parts admin edit forms manage images: file upload, thumbnail grid,
  drag-drop reorder, set primary, delete.
- Website menu is DB-driven: admin can add/edit/delete menu items and reorder
  them with drag-drop. Navbar renders from API.

## Shared Component

`SortableList` (frontend, `app/(admin)/components/SortableList.tsx`): native
HTML5 drag-and-drop (no new dependencies). Renders a list/grid of children with
drag handles; on drop emits `onReorder(orderedIds: string[])`. Used by the image
grid and the menu editor.

## Backend — Images

- `ProductImage` gains `Position` (int). `CarService` includes and orders
  `ProductImages` by `Position` in `SearchCarsAsync`, `GetDetailAsync`,
  `GetSimilarAsync` so `productImages[0]` stays deterministic.
- New admin endpoints in `API/Program.cs`:
  - `POST /api/images/upload` — multipart `IFormFile`, saved to
    `API/wwwroot/uploads/{guid}{ext}`, returns `{ url: "/uploads/{name}" }`.
    `UseStaticFiles()` serves the folder.
  - `PUT /api/product-images/reorder` — body `{ ids: string[] }`; sets
    `Position = index` for each id, ordered.
  - `GET /api/product-images/by-product/{productId}` — ordered by `Position`.
- Delete and set-primary reuse the existing generic `DELETE /product-images/{id}`
  and `PUT /product-images/{id}` endpoints.

## Backend — Menu

- New `MenuItem` entity: `Label`, `Url`, `Order`, `IsActive` (default true),
  `Icon` (optional).
- `SorchaDbContext` gains `DbSet<MenuItem>`.
- `MapMenuEndpoints` in `API/Program.cs`:
  - `GET /api/menu` — public, active items ordered by `Order`.
  - Admin: `GET /api/menu/all`, `POST /`, `PUT /{id}`, `DELETE /{id}`.
- Startup seeding: if no menu items exist, insert Cars → `/cars`,
  Car Parts → `/windows`, Blogs → `/windows`.

## Frontend — Images

- `ImagesManager.tsx` (`app/(admin)/components/`): thumbnail grid via
  `SortableList`, star toggles `isPrimary` (PUT), delete button (DELETE), file
  picker uploads via FormData to `/images/upload` then refetches
  `/product-images/by-product/{id}`.
- `adminEntities.ts`: `images: true` flag on `cars` and `parts` configs; remove
  the standalone `product-images` section.
- `EntityFormModal.tsx`: when `config.images && mode === "edit"`, render
  `ImagesManager` under the fields. Hidden on create (no id yet).

## Frontend — Menu

- `Navbar.tsx`: fetch `GET /api/menu` on mount, render active links in order.
  Keep Login/Register/Logout hardcoded. Fall back to the current three links
  when the API fails or the list is empty.
- New admin page `/admin/menu`: `SortableList` of menu items (drag reorder →
  `PUT` reorder), Add/Edit via existing `EntityFormModal` with a `menu` entity
  config, delete button. New items default to `Order = max + 1`.
- Add "menu" link to `SideNav.tsx`.

## Out of Scope

- Nested dropdown menus (flat links only).
- Image cropping/editing.
- Public listing changes beyond deterministic image order.

## Testing

Backend (`Tests/`): reorder sets Position correctly; by-product returns ordered
images; upload saves file and returns URL; MenuItem ordered list + seed runs once.
Frontend: `npx tsc --noEmit`, `npx eslint`, `next build`, manual smoke against
live API (upload, reorder, menu CRUD, navbar render).
