"use client";

import { useState } from "react";
import type { EntityConfig } from "../lib/adminEntities";
import AdminTable from "./AdminTable";
import EntityFormModal from "./EntityFormModal";

type Row = Record<string, unknown>;

export default function AdminCrudPage({ config }: { config: EntityConfig }) {
  const [modal, setModal] = useState<{ mode: "create" | "edit"; row: Row | null } | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  const refresh = () => setRefreshKey((k) => k + 1);

  return (
    <div>
      <AdminTable
        key={refreshKey}
        config={config}
        onAdd={() => setModal({ mode: "create", row: null })}
        onEdit={(row) => setModal({ mode: "edit", row })}
      />
      {modal && (
        <EntityFormModal
          config={config}
          mode={modal.mode}
          editing={modal.row}
          onClose={() => setModal(null)}
          onSaved={refresh}
        />
      )}
    </div>
  );
}
