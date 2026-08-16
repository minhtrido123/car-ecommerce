### Task 8: Frontend — real sidebar links

**Files:**
- Modify: `car-app/app/(admin)/components/SideNav.tsx`

**Interfaces:**
- Consumes: routes produced by Task 7 (`/admin/{slug}`) plus existing `/admin/dashboard`.
- Produces: `Link` targets for all managed entities.

- [ ] **Step 1: Rewrite `SideNav` links**

Replace the placeholder `href="#"` links in `car-app/app/(admin)/components/SideNav.tsx`:

- Dashboard → `/admin/dashboard`
- vehicles → `/admin/cars`
- parts → `/admin/parts`
- inventory → `/admin/product-images`
- Orders → `/admin/orders`
- customers → `/admin/users`
- brands → `/admin/brands`
- car models → `/admin/car-models`
- categories → `/admin/categories`

Keep `data sync`, `promotions`, `analytics`, `settings`, `logout` at `href="#"`. Full replacement for the three sections:

```tsx
    <div className="p-[20px_0px_5px_10px] leading-none font-semibold h-auto title normal-case">Home</div>
    <li><Link href="/admin/dashboard">
      <span className="mif-apps icon"></span>
      <span className="title">Dashboard</span>
    </Link></li>
    <li><Link href="/admin/cars">
      <span className="mif-drive-eta icon"></span>
      <span className="title">vehicles</span>
    </Link></li>
    <li><Link href="/admin/parts">
      <span className="icon"><div className="w-full h-full" style={{
        maskImage: "url('/turbo-charger.png')",
        WebkitMaskImage: "url('/turbo-charger.png')",
        maskSize: "contain",
        WebkitMaskSize: "contain",
        maskRepeat: "no-repeat",
        WebkitMaskRepeat: "no-repeat",
        maskPosition: "center",
        WebkitMaskPosition: "center",
        background: "black"
      }} /></span>
      <span className="title">parts</span>
    </Link></li>
    <hr className="border" />
    <div className="p-[20px_0px_5px_10px] leading-none font-semibold h-auto title normal-case">Inventory</div>
    <li><Link href="/admin/product-images">
      <span className="mif-inbox icon"></span>
      <span className="title">inventory</span>
    </Link></li>
    <li><Link href="/admin/orders">
      <span className="mif-cart icon"></span>
      <span className="title">Orders</span>
    </Link></li>
    <li><Link href="/admin/users">
      <span className="mif-cogs icon"></span>
      <span className="title">customers</span>
    </Link></li>
    <li><Link href="/admin/brands">
      <span className="mif-cogs icon"></span>
      <span className="title">brands</span>
    </Link></li>
    <li><Link href="/admin/car-models">
      <span className="mif-cogs icon"></span>
      <span className="title">car models</span>
    </Link></li>
    <li><Link href="/admin/categories">
      <span className="mif-cogs icon"></span>
      <span className="title">categories</span>
    </Link></li>
    <li><Link href="#">
      <span className="mif-cogs icon"></span>
      <span className="title">data sync</span>
    </Link></li>
    <li><Link href="#">
      <span className="mif-cogs icon"></span>
      <span className="title">promotions</span>
    </Link></li>
```

(The Settings section: `analytics`, `settings`, `logout` stay at `href="#"` unchanged.)

- [ ] **Step 2: Verify**

Run (from `car-app`): `npx tsc --noEmit && npm run lint && npm run build`
Expected: PASS.

---

