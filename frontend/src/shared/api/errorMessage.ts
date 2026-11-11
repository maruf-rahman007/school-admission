import { errorMessageKey, useI18n } from '@/shared/i18n'
import { ApiError } from './ApiError'

/** Returns a function that turns any thrown error into user-facing text in the current language. */
export function useErrorMessage(): (error: unknown) => string {
  const { t } = useI18n()
  return (error) => {
    if (error instanceof ApiError && error.isNetworkError) {
      return t('error.NETWORK')
    }
    return t(errorMessageKey(error instanceof ApiError ? error.code : null))
  }
}
