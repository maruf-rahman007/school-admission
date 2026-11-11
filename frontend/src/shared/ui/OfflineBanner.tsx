import { WifiOffIcon } from 'lucide-react'
import { useIsOffline } from '@/shared/api'
import { useI18n } from '@/shared/i18n'

/** Yellow bar under the header while the backend can't be reached (UI_DESIGN §1, "Network down"). */
export function OfflineBanner() {
  const offline = useIsOffline()
  const { t } = useI18n()
  if (!offline) {
    return null
  }
  return (
    <div role="status" className="border-b border-amber-300 bg-amber-50 text-amber-950">
      <p className="mx-auto flex max-w-5xl items-center gap-2 px-4 py-2 text-sm">
        <WifiOffIcon aria-hidden="true" className="size-4 shrink-0" />
        {t('error.NETWORK')}
      </p>
    </div>
  )
}
