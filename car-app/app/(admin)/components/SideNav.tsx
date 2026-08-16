"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useAuthStore } from "../../(main)/stores/auth-store";

export default function SideNav() {
  const [expandSide, setExpandSide] = useState(true);
  const router = useRouter();
  const logout = useAuthStore((state) => state.logout);

  const handleLogout = () => {
    logout();
    router.push("/login");
  };

  return <ul className={`sidenav-simple ${expandSide ? "sidenav-simple-expand-fs" : ""} h-auto`}>
    <li><Link onClick={() => setExpandSide(!expandSide)} href="#">
      <span className={`mif-chevron-left icon transition-transform duration-300 ${expandSide ? "" : "rotate-180"}`}></span>
    </Link></li>
    <li className={`${expandSide ? "h-auto" : ""}`}><Link className="h-auto" href="#">
      <span className="icon"><img src="/logo.jpg" /></span>
      <span className="title text-2xl font-bold text-wrap">Supra Admin</span>
    </Link></li>
    <hr className="border" />
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
    <li><Link href="/admin/menu">
      <span className="mif-menu icon"></span>
      <span className="title">Menu</span>
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

    <hr className="border" />
    <div className="p-[20px_0px_5px_10px] leading-none font-semibold h-auto title normal-case">Settings</div>


    <li><Link href="#">
      <span className="mif-cogs icon"></span>
      <span className="title">analytics</span>
    </Link></li>
    <li><Link href="#">
      <span className="mif-cogs icon"></span>
      <span className="title">settings</span>
    </Link></li>
    <li><Link href="/login" onClick={handleLogout}>
      <span className="mif-exit icon"></span>
      <span className="title">logout</span>
    </Link></li>
  </ul>
}
