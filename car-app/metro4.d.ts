declare module "metro4-dist/js/metro.min.js" {
  const value: unknown;
  export default value;
}

declare const Metro: {
  getPlugin(selector: string, name: string): {
    _create(): void;
    sorting(col: string, dir: string, reorder?: boolean): void;
  };
};

interface Window {
  METRO_AUTO_INIT?: boolean;
  Metro?: { init(): void };
}
