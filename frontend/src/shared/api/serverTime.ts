/** Header the backend adds to every response (ARCHITECTURE §8.3). */
export const SERVER_TIME_HEADER = 'x-server-time'

let offsetMs = 0

/** Updates `offset = serverTime − Date.now()`. A missing or unparseable header keeps the previous offset. */
export function recordServerTime(header: unknown): void {
  if (typeof header !== 'string') {
    return
  }
  const serverMs = Date.parse(header)
  if (Number.isFinite(serverMs)) {
    offsetMs = serverMs - Date.now()
  }
}

/** Current time corrected to the server clock, in ms. Countdowns use this, not Date.now(). */
export function serverNow(): number {
  return Date.now() + offsetMs
}
