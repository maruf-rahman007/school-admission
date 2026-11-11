import { useEffect, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { serverNow } from '@/shared/api'
import { remainingSeconds } from '@/shared/lib/countdown'
import { holdQueryKey } from './useHold'

export interface HoldCountdown {
  /** Whole seconds left, rounded up (0 once expired). */
  seconds: number
  /** True from the moment the countdown reaches 0; never turns back to false for the same hold. */
  expired: boolean
}

/**
 * Live countdown to a hold's `expiresAt` on the server clock (`serverNow()`, corrected by `X-Server-Time`), so a
 * wrong client clock doesn't matter. Recomputed from absolute times every second, so a throttled background tab
 * is right again on its first tick back.
 *
 * At 0, `expired` becomes true and stays true for this hold even if a later poll still says HELD (DECISIONS T-140);
 * the hold is fetched once right away to confirm, and the normal 3 s polling continues. A server CONFIRMED or
 * CANCELLED status wins over `expired`: callers check the server status first. T-150 disables Pay with `expired`.
 */
export function useHoldCountdown(holdId: string, expiresAt: string | undefined): HoldCountdown {
  const queryClient = useQueryClient()
  const key = `${holdId}|${expiresAt ?? ''}`
  const expiresAtMs = expiresAt === undefined ? Number.NaN : Date.parse(expiresAt)

  // Only forces a re-render each second; the time itself is read fresh on every render (below).
  const [, setTick] = useState(0)
  const [expiredKey, setExpiredKey] = useState<string | null>(null)
  const refetchedKey = useRef<string | null>(null)

  const known = Number.isFinite(expiresAtMs)
  // Read now, not at mount: on a hard reload the offset is only known once the first response has arrived, and a
  // stale reading with a fast client clock would wrongly (and stickily) expire a hold that is still HELD.
  const seconds = known ? remainingSeconds(expiresAtMs, serverNow()) : 0
  const expired = expiredKey === key || (known && seconds === 0)

  // Re-render once a second until expired.
  useEffect(() => {
    if (!known || expired) {
      return
    }
    const id = window.setInterval(() => setTick((tick) => tick + 1), 1000)
    return () => window.clearInterval(id)
  }, [key, known, expired])

  // Reaching 0: remember it (sticky) and ask the server once. The ref keeps StrictMode's re-run from refetching twice.
  useEffect(() => {
    if (!expired) {
      return
    }
    if (expiredKey !== key) {
      setExpiredKey(key)
    }
    if (refetchedKey.current !== key) {
      refetchedKey.current = key
      // Errors are kept in the query state (offline banner); polling keeps retrying.
      void queryClient.refetchQueries({ queryKey: holdQueryKey(holdId) })
    }
  }, [expired, expiredKey, key, holdId, queryClient])

  return { seconds: expired ? 0 : seconds, expired }
}
