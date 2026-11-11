/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Backend base URL, baked in at build time (ADR-0003). */
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
