### Task 5: Frontend — `EntityFormModal` (generated form + create/update)

**Files:**
- Create: `car-app/app/(admin)/components/EntityFormModal.tsx`

**Interfaces:**
- Consumes: `EntityConfig`, `EntityField` (Task 4); `create`, `update`, `getLookupOptions` (Task 3).
- Produces: default-export `EntityFormModal({ config, mode, editing, onClose, onSaved })` where `mode: "create" | "edit"`, `editing: Record<string, unknown> | null`, `onClose: () => void`, `onSaved: () => void`. Consumed by `AdminCrudPage` (Task 7).

- [ ] **Step 1: Write the component**

Create `car-app/app/(admin)/components/EntityFormModal.tsx`:

```tsx
"use client";

import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { EntityConfig, EntityField } from "../lib/adminEntities";
import { create, getLookupOptions, update } from "../lib/adminApi";

type Row = Record<string, unknown>;
type Mode = "create" | "edit";

function fieldSchema(field: EntityField, required: boolean): z.ZodType {
  switch (field.type) {
    case "email":
      return required
        ? z.string().min(1, `${field.label} is required`).email("Invalid email")
        : z.string().email("Invalid email").optional();
    case "number":
      return required
        ? z.coerce.number({ message: `${field.label} must be a number` }).int(`${field.label} must be an integer`)
        : z.preprocess(
            (v) => (v === "" || v == null ? undefined : Number(v)),
            z.number().int().optional()
          );
    case "decimal":
      return required
        ? z.coerce.number({ message: `${field.label} must be a number` })
        : z.preprocess(
            (v) => (v === "" || v == null ? undefined : Number(v)),
            z.number().optional()
          );
    case "checkbox":
      return z.boolean();
    case "password":
      return z.string();
    default:
      return required ? z.string().min(1, `${field.label} is required`) : z.string();
  }
}

function buildSchema(config: EntityConfig, mode: Mode) {
  const shape: Record<string, z.ZodType> = {};
  for (const field of config.fields) {
    if (mode === "create" ? field.hiddenOnCreate : field.hiddenOnEdit) continue;
    const required = field.type === "password" ? false : (field.required ?? false);
    shape[field.key] = fieldSchema(field, required);
  }
  return z.object(shape);
}

function toDefaults(config: EntityConfig, mode: Mode, editing: Row | null) {
  const defaults: Record<string, unknown> = {};
  for (const field of config.fields) {
    if (mode === "create" ? field.hiddenOnCreate : field.hiddenOnEdit) continue;
    if (editing && editing[field.key] != null) defaults[field.key] = editing[field.key];
    else if (field.type === "checkbox") defaults[field.key] = false;
    else defaults[field.key] = "";
  }
  return defaults;
}

export default function EntityFormModal({
  config,
  mode,
  editing,
  onClose,
  onSaved,
}: {
  config: EntityConfig;
  mode: Mode;
  editing: Row | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const editableFields = config.fields.filter((f) =>
    mode === "create" ? !f.hiddenOnCreate : !f.hiddenOnEdit
  );
  const [lookups, setLookups] = useState<Record<string, Map<string, string>>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const schema = useMemo(() => buildSchema(config, mode), [config, mode]);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<Record<string, unknown>>({
    resolver: zodResolver(schema),
    defaultValues: toDefaults(config, mode, editing),
  });

  useEffect(() => {
    reset(toDefaults(config, mode, editing));
  }, [config, mode, editing, reset]);

  useEffect(() => {
    let cancelled = false;
    for (const field of editableFields) {
      if (field.type === "select" && field.lookup) {
        getLookupOptions(field).then((options) => {
          if (!cancelled) setLookups((prev) => ({ ...prev, [field.key]: options }));
        });
      }
    }
    return () => {
      cancelled = true;
    };
  }, [editableFields]);

  const onSubmit = async (values: Record<string, unknown>) => {
    setSaving(true);
    setSubmitError(null);
    try {
      if (mode === "create") {
        await create(config, values);
      } else {
        await update(config, String(editing!.id), values);
      }
      onSaved();
      onClose();
    } catch (err) {
      const response = (err as { response?: { status?: number; data?: { error?: string } } })?.response;
      setSubmitError(response?.data?.error ?? `Save failed (${response?.status ?? "network error"})`);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
      onClick={onClose}
    >
      <form
        onClick={(e) => e.stopPropagation()}
        onSubmit={handleSubmit(onSubmit)}
        className="card p-6 w-full max-w-2xl max-h-[90vh] overflow-y-auto"
      >
        <h3 className="text-xl font-bold mb-4">
          {mode === "create" ? "Add" : "Edit"} {config.singular ?? config.label}
        </h3>
        {submitError && <div className="alert alert-error mb-4">{submitError}</div>}
        {editableFields.map((field) => (
          <div className="form-group" key={field.key}>
            <label>
              {field.label}
              {field.required ? " *" : ""}
            </label>
            {field.type === "select" ? (
              <select className="input w-full" {...register(field.key)}>
                <option value="">Select...</option>
                {(field.options ??
                  Array.from(lookups[field.key] ?? []).map(([value, label]) => ({ value, label }))).map(
                  (opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  )
                )}
              </select>
            ) : field.type === "textarea" ? (
              <textarea className="input w-full" rows={4} {...register(field.key)} />
            ) : field.type === "checkbox" ? (
              <input type="checkbox" {...register(field.key)} />
            ) : (
              <input
                className="input w-full"
                type={
                  field.type === "password"
                    ? "password"
                    : field.type === "number" || field.type === "decimal"
                      ? "number"
                      : "text"
                }
                step={field.type === "decimal" ? "0.01" : undefined}
                {...register(field.key)}
              />
            )}
            {errors[field.key] && (
              <p className="text-red-500">{String(errors[field.key]?.message ?? "")}</p>
            )}
            {field.type === "password" && mode === "edit" && (
              <small className="text-muted">Leave blank to keep current password.</small>
            )}
          </div>
        ))}
        <div className="flex gap-2 justify-end mt-4">
          <button type="button" className="button" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="button success" disabled={saving}>
            {saving ? "Saving..." : "Save"}
          </button>
        </div>
      </form>
    </div>
  );
}
```

- [ ] **Step 2: Typecheck**

Run (from `car-app`): `npx tsc --noEmit`
Expected: PASS.

---

