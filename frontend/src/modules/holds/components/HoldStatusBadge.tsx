import { useI18n } from '@/shared/i18n'
import { cn } from '@/shared/lib/utils'
import type { HoldStatus } from '../types'

const colours: Record<HoldStatus, string> = {
  HELD: 'bg-amber-100 text-amber-900 ring-amber-300',
  CONFIRMED: 'bg-green-100 text-green-900 ring-green-300',
  EXPIRED: 'bg-muted text-muted-foreground ring-border',
  CANCELLED: 'bg-muted text-muted-foreground ring-border',
}

/** Status badge with the UI_DESIGN §8 texts and colours. */
export function HoldStatusBadge({ status }: { status: HoldStatus }) {
  const { t } = useI18n()
  return (
    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-sm font-medium ring-1 ring-inset', colours[status])}>
      {t(`holds.status.${status}`)}
    </span>
  )
}
