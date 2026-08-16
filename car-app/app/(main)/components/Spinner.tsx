"use client";

import { useLoadingStore } from "../stores/loading-store";

export default function GlobalSpinner() {
  const loading = useLoadingStore((s) => s.isLoading);

  if (!loading) return null;

  return (
    <div className="fixed! inset-0! bg-black/30! flex! items-center! justify-center! z-[2000]!">
        <div data-role="activity" data-type="metro" data-style="color"></div>
    </div>
  );
}