import { useQuery } from '@tanstack/react-query'
import { fetchHold } from '../api/holdsApi'

export const holdQueryKey = (holdId: string) => ['hold', holdId] as const

/**
 * One application, polled every 3 s (UI_DESIGN §1) so a status change (e.g. expiry) shows without a reload.
 * The hold does not depend on who is looking, so the student ID is not part of the key. No extra retries:
 * the next poll is the retry, so a 404 shows at once. After a failed poll the last good data stays.
 */
export function useHold(holdId: string) {
  return useQuery({
    queryKey: holdQueryKey(holdId),
    queryFn: ({ signal }) => fetchHold(holdId, signal),
    refetchInterval: 3000,
    retry: false,
  })
}
