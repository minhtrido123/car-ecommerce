"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuthStore } from "../../(main)/stores/auth-store";

export default function RequireAdmin({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const [mounted, setMounted] = useState(false);
  const accessToken = useAuthStore((state) => state.accessToken);
  const user = useAuthStore((state) => state.user);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- hydration guard
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!mounted) return;
    if (!accessToken || !user) {
      router.replace("/login");
    } else if (user.role !== "Admin") {
      router.replace("/");
    }
  }, [mounted, accessToken, user, router]);

  if (!mounted || !accessToken || !user || user.role !== "Admin") {
    return null;
  }

  return <>{children}</>;
}
