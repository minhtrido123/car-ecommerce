"use client";

import { Slider } from "@/components/ui/slider";

export default function RangeFilter({
  label,
  min,
  max,
  step,
  lo,
  hi,
  format,
  onChange,
}: {
  label: string;
  min: number;
  max: number;
  step: number;
  lo: number | null;
  hi: number | null;
  format: (n: number) => string;
  onChange: (lo: number | null, hi: number | null) => void;
}) {
  const loVal = lo ?? min;
  const hiVal = hi ?? max;

  return (
    <div className="m-2 flex-grow">
      <div className="text-[var(--foreground)] text-bold">{label}</div>

      <Slider
        value={[loVal, hiVal]}
        min={min}
        max={max}
        step={step}
        onValueChange={([newLo, newHi]: any) => {
          onChange(newLo, newHi);
        }}
        className="size-5"
      />

      <div className="text-center text-[var(--foreground)] text-small">
        {format(loVal)} — {format(hiVal)}
      </div>
    </div>
  );
}
