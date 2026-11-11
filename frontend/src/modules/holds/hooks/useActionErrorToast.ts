import { toast } from 'sonner'
import { ApiError, useErrorMessage } from '@/shared/api'
import { useI18n } from '@/shared/i18n'

/** Toast for a failed Pay/Cancel: plain words for the error code, or "can't reach the server" when there was no answer. */
export function useActionErrorToast() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  return (error: Error) =>
    toast.error(error instanceof ApiError && error.isNetworkError ? t('error.NETWORK_SUBMIT') : errorMessage(error))
}
