import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createHold } from '../api/holdsApi'
import { myHoldsKeyPrefix } from './useMyHolds'

/** POST /classes/{classId}/holds. Never retried automatically: a repeated click must not send a second request. */
export function useCreateHold() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (classId: string) => createHold(classId),
    retry: false,
    // Success or failure (e.g. 409 because an active hold exists): "my applications" and the class-list banner update.
    onSettled: () => queryClient.invalidateQueries({ queryKey: myHoldsKeyPrefix }),
  })
}
