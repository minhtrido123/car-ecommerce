"use client";

import { useCallback, useEffect, useState } from "react";
import api from "../../../(main)/lib/api";
import SortableList from "../../components/SortableList";
import EntityFormModal from "../../components/EntityFormModal";
import { adminEntities } from "../../lib/adminEntities";

type MenuRow = {
  id: string;
  label: string;
  url: string;
  icon?: string | null;
  order: number;
  isActive: boolean;
};

export default function AdminMenuPage() {
  const [items, setItems] = useState<MenuRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState<{
    mode: "create" | "edit";
    row: MenuRow | null;
  } | null>(null);

  const load = useCallback(async () => {
    try {
      const { data } = await api.get<MenuRow[]>("/menu/all", {
        showLoading: false,
      });
      setError(null);
      setItems(data);
    } catch {
      setError("Failed to load menu");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    api
      .get<MenuRow[]>("/menu/all", { showLoading: false })
      .then(({ data }) => {
        if (cancelled) return;
        setError(null);
        setItems(data);
      })
      .catch(() => {
        if (!cancelled) setError("Failed to load menu");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const handleReorder = async (ids: string[]) => {
    const byId = new Map(items.map((i) => [i.id, i]));
    const reordered = ids
      .map((id, order) => {
        const item = byId.get(id);
        return item ? { ...item, order } : null;
      })
      .filter((i): i is MenuRow => i !== null);
    setItems(reordered);
    try {
      for (const item of reordered) {
        await api.put(
          `/menu/${item.id}`,
          {
            id: item.id,
            data: {
              label: item.label,
              url: item.url,
              icon: item.icon ?? null,
              isActive: item.isActive,
              order: item.order,
            },
          },
          { showLoading: false }
        );
      }
    } catch {
      setError("Failed to save order");
      setLoading(true);
      load();
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/menu/${id}`, { showLoading: false });
      setLoading(true);
      await load();
    } catch {
      setError("Failed to delete menu item");
    }
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-2xl font-bold">Website Menu</h2>
        <button
          className="button primary"
          onClick={() => setModal({ mode: "create", row: null })}
        >
          Add item
        </button>
      </div>
      {error && <div className="alert alert-error mb-4">{error}</div>}
      {loading ? (
        <p className="text-muted">Loading...</p>
      ) : items.length === 0 ? (
        <p className="text-muted">No menu items.</p>
      ) : (
        <SortableList
          items={items}
          onReorder={handleReorder}
          className="space-y-2"
          renderItem={(item) => (
            <div className="card p-3 flex items-center justify-between">
              <button
                type="button"
                className="flex items-center gap-3 text-left flex-grow"
                onClick={() => setModal({ mode: "edit", row: item })}
              >
                {item.icon && <span className={`mif-${item.icon}`}></span>}
                <span className="font-semibold">{item.label}</span>
                <span className="text-sm">{item.url}</span>
                {!item.isActive && (
                  <span className="bg-gray-200 text-gray-600 text-xs px-2 py-0.5 rounded">
                    inactive
                  </span>
                )}
              </button>
              <button
                type="button"
                className="button danger small"
                onClick={() => handleDelete(item.id)}
              >
                Delete
              </button>
            </div>
          )}
        />
      )}
      {modal && (
        <EntityFormModal
          config={adminEntities.menu}
          mode={modal.mode}
          editing={modal.row as Record<string, unknown> | null}
          onClose={() => setModal(null)}
          onSaved={() => {
            setModal(null);
            load();
          }}
        />
      )}
      <p className="text-small text-muted mt-2">
        Drag items to reorder. Click an item to edit.
      </p>
    </div>
  );
}
