// app/components/MetroProvider.tsx
'use client';

import Script from 'next/script';
import { useEffect } from 'react';

export default function MetroProvider({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    const initMetroGlobal = async () => {
      if (typeof window !== 'undefined') {
        // Prevent early automatic parsing before React mounts
        window.METRO_AUTO_INIT = false;

        try {
          // Import the engine once globally
          await import('metro4-dist/js/metro.min.js');

          const Metro = window.Metro;
          if (Metro) {
            // Run a clean global scan
            Metro.init();
          }
        } catch (error) {
          console.error("Global Metro engine failed to boot:", error);
        }
      }
    };

    initMetroGlobal();
  }, []);

  return <>
    {children}
    </>
}