// stores/loading-store.ts
import { create } from "zustand";

interface LoadingState {
  requestCount: number;
  isLoading: boolean;
  start: () => void;
  stop: () => void;
}

export const useLoadingStore = create<LoadingState>((set) => ({
  requestCount: 0,
  isLoading: false,

  start: () =>
    set((state) => ({
      requestCount: state.requestCount + 1,
      isLoading: true,
    })),

  stop: () =>
    set((state) => {
      const count = Math.max(0, state.requestCount - 1);

      return {
        requestCount: count,
        isLoading: count > 0,
      };
    }),
}));