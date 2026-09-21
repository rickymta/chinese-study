/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Gốc API identity. Dev mặc định `/identity/api` (qua Vite proxy); production `https://id.antfarms.xyz/api`. */
  readonly VITE_IDENTITY_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
