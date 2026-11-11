import { useQuery } from '@tanstack/react-query'
import { fetchClasses } from '../api/classesApi'

/** Shared by the class list and the admin page, so a created class shows up in both. */
export const classesQueryKey = ['classes'] as const

/**
 * All classes, polled every 3 s (UI_DESIGN §1). No extra retries: the next poll is the retry, so a failed
 * first load shows its error at once. After a failed poll the last good data stays on screen.
 */
export function useClasses() {
  return useQuery({
    queryKey: classesQueryKey,
    queryFn: ({ signal }) => fetchClasses(signal),
    refetchInterval: 3000,
    retry: false,
  })
}
