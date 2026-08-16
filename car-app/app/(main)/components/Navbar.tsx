"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import style from "../css/Navbar.module.css"
import { useAuthStore } from "../stores/auth-store";
import api from "../lib/api";

type MenuItem = {
  id: string;
  label: string;
  url: string;
  icon?: string | null;
  order: number;
  isActive: boolean;
};

const FALLBACK_MENU = [
  { label: "Cars", url: "/cars" },
  { label: "Car Parts", url: "/windows" },
  { label: "Blogs", url: "/windows" },
];

export default function Navbar() {
  const accessToken = useAuthStore((state) => state.accessToken);
  const logout = useAuthStore((state) => state.logout);
  const [menu, setMenu] = useState<{ label: string; url: string }[]>([]);

  useEffect(() => {
    let cancelled = false;
    api
      .get<MenuItem[]>("/menu", { showLoading: false })
      .then(({ data }) => {
        if (cancelled) return;
        setMenu(data.map((item) => ({ label: item.label, url: item.url })));
      })
      .catch(() => {
        if (cancelled) return;
        setMenu([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const items = menu.length > 0 ? menu : FALLBACK_MENU;

  return (
    <div
      data-role="appbar"
      className={`justify-between sticky! p-[5px] large ${style.appBar}`}
      data-expand-point="md"
    >
      <Link href="/" className="brand no-hover md:flex-1 justify-start">
        <img src={"/logo.jpg"} className="w-[40px]!" />
      </Link>

      <ul className={`app-bar-menu relative gap-5 large ${style.appBarMenu}`}>
        {items.map((item) => (
          <li key={item.label + item.url}>
            <Link href={item.url} className="">{item.label}</Link>
          </li>
        ))}
      </ul>
      {!accessToken && <ul className={`app-bar-menu justify-end flex md:flex-1`}>
        <li className="fresh-primary-action border-r-black!">
          <Link href="/login" className="fresh-primary-action-content font-bold text-md!">Login</Link>
          <Link href="/login" className="fresh-primary-action-content fresh-primary-action-content--hover font-bold text-md!">Login</Link>
        </li>
        <li className="fresh-primary-action">
          <Link href="/register" className="fresh-primary-action-content font-bold text-md!">Register</Link>
          <Link href="/register" className="fresh-primary-action-content fresh-primary-action-content--hover font-bold text-md!">Register</Link>
        </li>
      </ul>}

      {accessToken && <ul className={`app-bar-menu justify-end flex md:flex-1`}>
        <li className="fresh-primary-action">
          <Link href="#" onClick={logout} className="fresh-primary-action-content font-bold text-md!">Logout</Link>
          <Link href="#" onClick={logout} className="fresh-primary-action-content fresh-primary-action-content--hover font-bold text-md!">Logout</Link>
        </li>

      </ul>}


    </div>
  );
}
