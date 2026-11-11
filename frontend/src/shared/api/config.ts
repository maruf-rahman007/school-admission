/** Backend base URL from the build arg; the fallback (same as the compose default) covers `npm run dev`. */
export const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'
