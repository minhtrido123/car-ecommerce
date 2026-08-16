"use client";

import { useCallback, useEffect, useState } from "react";
import api from "../../(main)/lib/api";
import SortableList from "./SortableList";

type ImageRow = {
  id: string;
  productId: string;
  url: string;
  isPrimary: boolean;
  position: number;
};

export default function ImagesManager({ productId }: { productId: string }) {
  const [images, setImages] = useState<ImageRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const { data } = await api.get<ImageRow[]>(
        `/product-images/by-product/${productId}`,
        { showLoading: false }
      );
      setError(null);
      setImages(data);
    } catch {
      setError("Failed to load images");
    } finally {
      setLoading(false);
    }
  }, [productId]);

  useEffect(() => {
    let cancelled = false;
    api
      .get<ImageRow[]>(`/product-images/by-product/${productId}`, {
        showLoading: false,
      })
      .then(({ data }) => {
        if (cancelled) return;
        setError(null);
        setImages(data);
      })
      .catch(() => {
        if (!cancelled) setError("Failed to load images");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [productId]);

  const handleReorder = async (ids: string[]) => {
    const byId = new Map(images.map((im) => [im.id, im]));
    setImages(
      ids
        .map((id, position) => {
          const image = byId.get(id);
          return image ? { ...image, position } : null;
        })
        .filter((im): im is ImageRow => im !== null)
    );
    try {
      await api.put("/product-images/reorder", { ids }, { showLoading: false });
    } catch {
      setError("Failed to save order");
      setLoading(true);
      load();
    }
  };

  const handleSetPrimary = async (image: ImageRow) => {
    try {
      for (const im of images) {
        if (im.isPrimary !== (im.id === image.id)) {
          await api.put(
            `/product-images/${im.id}`,
            { id: im.id, data: { ...im, isPrimary: im.id === image.id } },
            { showLoading: false }
          );
        }
      }
      setLoading(true);
      await load();
    } catch {
      setError("Failed to update primary image");
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/product-images/${id}`, { showLoading: false });
      setLoading(true);
      await load();
    } catch {
      setError("Failed to delete image");
    }
  };

  const handleUpload = async (file: File) => {
    setUploading(true);
    setError(null);
    try {
      const form = new FormData();
      form.append("file", file);
      const { data } = await api.post<{ url: string }>("/images/upload", form, {
        showLoading: false,
      });
      await api.post(
        "/product-images",
        {
          data: {
            productId,
            url: data.url,
            isPrimary: images.length === 0,
            position: images.length,
          },
        },
        { showLoading: false }
      );
      setLoading(true);
      await load();
    } catch {
      setError("Upload failed");
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="border bd-default p-3 mt-4">
      <div className="flex justify-between items-center mb-2">
        <h4 className="font-bold">Images</h4>
        <label className="button primary cursor-pointer">
          {uploading ? "Uploading..." : "Add image"}
          <input
            type="file"
            accept="image/*"
            className="hidden"
            disabled={uploading}
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) handleUpload(file);
              e.target.value = "";
            }}
          />
        </label>
      </div>
      {error && <div className="alert alert-error mb-2">{error}</div>}
      {loading ? (
        <p className="text-muted">Loading images...</p>
      ) : images.length === 0 ? (
        <p className="text-muted">No images yet.</p>
      ) : (
        <SortableList
          items={images}
          onReorder={handleReorder}
          className="grid grid-cols-3 gap-2 sm:grid-cols-4 md:grid-cols-5"
          itemClassName="relative"
          renderItem={(image) => (
            <div className="relative group">
              <img
                src={image.url}
                alt=""
                className="w-full h-24 object-cover rounded border"
              />
              <button
                type="button"
                title="Set primary"
                onClick={() => handleSetPrimary(image)}
                className={`absolute top-1 left-1 rounded-full p-1 text-lg leading-none ${
                  image.isPrimary
                    ? "text-yellow-400"
                    : "text-gray-400 hover:text-yellow-400"
                }`}
              >
                ★
              </button>
              <button
                type="button"
                title="Delete"
                onClick={() => handleDelete(image.id)}
                className="absolute top-1 right-1 rounded-full p-1 text-lg leading-none text-gray-400 hover:text-red-500"
              >
                ✕
              </button>
            </div>
          )}
        />
      )}
      <p className="text-small text-muted mt-2">
        Drag thumbnails to reorder. Star marks the primary image.
      </p>
    </div>
  );
}
