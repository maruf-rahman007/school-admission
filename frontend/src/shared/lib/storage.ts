/**
 * localStorage that never throws: in private mode or with storage blocked, reads return null and writes
 * are skipped, so the app keeps working (it just doesn't remember).
 */
export const storage = {
  get(key: string): string | null {
    try {
      return window.localStorage.getItem(key)
    } catch {
      return null
    }
  },
  set(key: string, value: string): void {
    try {
      window.localStorage.setItem(key, value)
    } catch {
      // Storage unavailable: nothing to remember.
    }
  },
  remove(key: string): void {
    try {
      window.localStorage.removeItem(key)
    } catch {
      // Storage unavailable.
    }
  },
}
