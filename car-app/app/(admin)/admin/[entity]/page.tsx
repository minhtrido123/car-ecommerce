"use client";

import { use } from "react";
import Link from "next/link";
import { adminEntities } from "../../lib/adminEntities";
import AdminCrudPage from "../../components/AdminCrudPage";

export default function AdminEntityPage({
  params,
}: {
  params: Promise<{ entity: string }>;
}) {
  const { entity } = use(params);
  const config = adminEntities[entity];

  if (!config) {
    return (
      <div>
        <h2 className="text-2xl font-bold mb-4">Unknown section</h2>
        <p>No entity configured for &quot;{entity}&quot;.</p>
        <Link className="button mt-4" href="/admin/dashboard">
          Back to dashboard
        </Link>
      </div>
    );
  }

  return <AdminCrudPage config={config} />;
}
