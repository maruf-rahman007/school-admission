import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useI18n } from '@/shared/i18n'
import { cancelHold } from '../api/holdsApi'
import { classesListKey } from './classesListKey'
import { useActionErrorToast } from './useActionErrorToast'
import { holdQueryKey } from './useHold'

/**
 * DELETE /holds/{holdId}. Never retried automatically: one click sends one request.
 * 204 has no body, so the hold is fetched again (CANCELLED, or EXPIRED if it had already run out).
 * The cache work and the result toast live here (not in `mutate` callbacks), so they still happen if the caller
 * unmounts before the answer arrives (TanStack Query skips `mutate` callbacks after unmount).
 */
export function useCancelHold(holdId: string) {
  const queryClient = useQueryClient()
  const { t } = useI18n()
  const errorToast = useActionErrorToast()
  return useMutation({
    mutationFn: () => cancelHold(holdId),
    retry: false,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: holdQueryKey(holdId) })
      toast.success(t('holds.cancelledToast'))
    },
    // 403/409 etc.: show what the server really has now.
    onError: (error) => {
      errorToast(error)
      // Not awaited: the dialog closes right after the toast; the view updates when the refetch lands.
      void queryClient.refetchQueries({ queryKey: holdQueryKey(holdId) })
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: classesListKey }),
  })
}
