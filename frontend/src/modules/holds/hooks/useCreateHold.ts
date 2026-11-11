import { useMutation } from '@tanstack/react-query'
import { createHold } from '../api/holdsApi'

/** POST /classes/{classId}/holds. Never retried automatically: a repeated click must not send a second request. */
export function useCreateHold() {
  return useMutation({
    mutationFn: (classId: string) => createHold(classId),
    retry: false,
  })
}
