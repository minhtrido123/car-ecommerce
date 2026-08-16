"use client";

import { useState } from "react";
import type { ReactNode } from "react";

type SortableItem = { id: string };

export default function SortableList<T extends SortableItem>({
  items,
  onReorder,
  renderItem,
  className = "",
  itemClassName = "",
}: {
  items: T[];
  onReorder: (orderedIds: string[]) => void;
  renderItem: (item: T, index: number) => ReactNode;
  className?: string;
  itemClassName?: string;
}) {
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [overIndex, setOverIndex] = useState<number | null>(null);

  const finishDrop = () => {
    if (dragIndex != null && overIndex != null && dragIndex !== overIndex) {
      const reordered = [...items];
      const [moved] = reordered.splice(dragIndex, 1);
      reordered.splice(overIndex, 0, moved);
      onReorder(reordered.map((i) => i.id));
    }
    setDragIndex(null);
    setOverIndex(null);
  };

  return (
    <div className={className}>
      {items.map((item, index) => (
        <div
          key={item.id}
          draggable
          onDragStart={() => setDragIndex(index)}
          onDragOver={(e) => {
            e.preventDefault();
            setOverIndex(index);
          }}
          onDrop={finishDrop}
          onDragEnd={finishDrop}
          className={`${itemClassName} cursor-grab active:cursor-grabbing select-none ${
            dragIndex === index ? "opacity-40" : ""
          } ${
            overIndex === index && dragIndex !== index
              ? "ring-2 ring-blue-400 rounded"
              : ""
          }`}
        >
          {renderItem(item, index)}
        </div>
      ))}
    </div>
  );
}
