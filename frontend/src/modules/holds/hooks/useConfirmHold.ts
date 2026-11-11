import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useI18n } from '@/shared/i18n'
import { confirmHold } from '../api/holdsApi'
import { classesListKey } from './classesListKey'
import { useActionErrorToast } from './useActionErrorToast'
import { holdQueryKey } from './useHold'
import { myHoldsKeyPrefix } from './useMyHolds'

/**
 * POST /holds/{holdId}/confirm. Never retried automatically: one click sends one request.
 * The cache work and the result toast live here (not in `mutate` callbacks), so they still happen if the caller
 * unmounts before the answer arrives (TanStack Query skips `mutate` callbacks after unmount).
 */
export function useConfirmHold(holdId: string) {
  const queryClient = useQueryClient()
  const { t } = useI18n()
  const errorToast = useActionErrorToast()
  return useMutation({
    mutationFn: () => confirmHold(holdId),
    retry: false,
    onSuccess: async (hold) => {
      // A poll that started before the confirm could land afterwards with stale HELD data: stop it first.
      await queryClient.cancelQueries({ queryKey: holdQueryKey(holdId) })
      queryClient.setQueryData(holdQueryKey(holdId), hold)
      toast.success(t('holds.paidToast'))
    },
    // 403/409/410 etc.: show what the server really has now.
    onError: (error) => {
      errorToast(error)
      // Not awaited: the dialog closes right after the toast; the view updates when the refetch lands.
      void queryClient.refetchQueries({ queryKey: holdQueryKey(holdId) })
    },
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: classesListKey }),
        queryClient.invalidateQueries({ queryKey: myHoldsKeyPrefix }),
      ]),
  })
}
