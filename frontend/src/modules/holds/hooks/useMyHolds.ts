import { useQuery } from '@tanstack/react-query'
import { useStudentId } from '@/modules/student'
import { fetchMyHolds } from '../api/holdsApi'
import type { MyHold } from '../types'

/** Prefix of every "my applications" query; Apply, Pay and Cancel invalidate it for any student ID. */
export const myHoldsKeyPrefix = ['myHolds'] as const

export const myHoldsQueryKey = (studentId: string | null) => [...myHoldsKeyPrefix, studentId] as const

/**
 * The saved student's applications (`GET /me/holds`), polled every 3 s (UI_DESIGN §1). Keyed by student ID, so a
 * changed ID never shows the previous student's list: `data` is undefined until the new ID's answer arrives.
 * Disabled (no request) while no ID is saved. No extra retries: the next poll is the retry; a failed poll keeps the
 * last good data.
 */
export function useMyHolds() {
  const studentId = useStudentId()
  const query = useQuery({
    queryKey: myHoldsQueryKey(studentId),
    queryFn: ({ signal }) => fetchMyHolds(signal),
    enabled: studentId !== null,
    refetchInterval: 3000,
    retry: false,
  })
  return { ...query, studentId }
}

/**
 * The student's active (HELD or CONFIRMED) application, by server status only, or undefined: no ID saved, list not
 * answered yet (loading or failed first load), or nothing active. The class list then leaves Apply enabled and relies
 * on the server's 409 (DECISIONS T-160).
 */
export function useActiveHold(): MyHold | undefined {
  const { data } = useMyHolds()
  return data?.find((hold) => hold.status === 'HELD' || hold.status === 'CONFIRMED')
}
