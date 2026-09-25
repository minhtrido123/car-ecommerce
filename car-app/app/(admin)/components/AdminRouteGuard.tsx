"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/app/(main)/stores/auth-store";

type Role = "admin" | "user";

export function RoleGuard({
  roles,
  children,
}: {
  roles: Role[];
  children: React.ReactNode;
}) {
  const router = useRouter();
  const { user, accessToken } = useAuthStore();

  useEffect(() => {
    if (!accessToken) {
      // router.replace("/login");
      return;
    }

    if (!user || !roles.includes(user.role.toLowerCase() as Role)) {
      router.replace("/not-found");
    }
  }, [accessToken, user, roles, router]);

  if (
    !accessToken ||
    !user ||
    !roles.includes(user.role.toLowerCase() as Role)
  ) {
    return null;
  }

  return children;
}
