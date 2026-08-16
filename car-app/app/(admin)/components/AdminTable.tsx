"use client";

import { useEffect, useState } from "react";
import Pagination from "../../(main)/components/Pagination";
import type { EntityConfig, EntityField } from "../lib/adminEntities";
import type { PagedResult } from "../lib/adminApi";
import { getLookupOptions, list, remove } from "../lib/adminApi";

type Row = Record<string, unknown>;

function cellValue(
  field: EntityField,
  row: Row,
  lookups: Record<string, Map<string, string>>
) {
  const value = row[field.key];
  if (value == null) return "—";
  if (field.type === "checkbox") return value ? "Yes" : "No";
  if (field.type === "select" && field.lookup) {
    return lookups[field.key]?.get(String(value)) ?? String(value);
  }
  return String(value);
}

export default function AdminTable({
  config,
  onEdit,
  onAdd,
}: {
  config: EntityConfig;
  onEdit: (row: Row) => void;
  onAdd: () => void;
}) {
  const columns = config.fields.filter((f) => f.showInTable !== false);
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PagedResult<Row> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [lookups, setLookups] = useState<Record<string, Map<string, string>>>({});

  const load = async (targetPage: number) => {
    setError(null);
    try {
      setData(await list<Row>(config, targetPage));
    } catch {
      setError("Failed to load data");
    }
  };

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    load(page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, config]);

  useEffect(() => {
    let cancelled = false;
    for (const field of config.fields) {
      if (field.type === "select" && field.lookup) {
        getLookupOptions(field).then((options) => {
          if (!cancelled) setLookups((prev) => ({ ...prev, [field.key]: options }));
        }).catch(() => {});
      }
    }
    return () => {
      cancelled = true;
    };
  }, [config]);

  const handleDelete = async (row: Row) => {
    if (!window.confirm(`Delete ${config.singular ?? config.label}?`)) return;
    try {
      await remove(config, String(row.id));
      if (data && data.items.length === 1 && page > 1) {
        setPage(page - 1);
      } else {
        load(page);
      }
    } catch {
      setError("Delete failed");
    }
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-2xl font-bold">{config.label}</h2>
        {config.canCreate !== false && (
          <button className="button primary" onClick={onAdd}>
            Add {config.singular ?? config.label}
          </button>
        )}
      </div>
      {error && <div className="alert alert-error mb-4">{error}</div>}
      <table className="table table-border cell-border">
        <thead>
          <tr>
            {columns.map((field) => (
              <th key={field.key}>{field.label}</th>
            ))}
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {(data?.items ?? []).map((row) => (
            <tr key={String(row.id)}>
              {columns.map((field) => (
                <td key={field.key}>{cellValue(field, row, lookups)}</td>
              ))}
              <td>
                {config.canEdit !== false && (
                  <button className="button small" onClick={() => onEdit(row)}>
                    Edit
                  </button>
                )}
                {config.canDelete !== false && (
                  <button
                    className="button small danger ml-2"
                    onClick={() => handleDelete(row)}
                  >
                    Delete
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {data && (
        <Pagination
          currentPage={data.pageNumber}
          totalCount={data.totalCount}
          pageSize={data.pageSize}
          onPageChange={setPage}
        />
      )}
    </div>
  );
}
