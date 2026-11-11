import { Link, useParams } from 'react-router'
import { ApiError, useErrorMessage } from '@/shared/api'
import { useI18n } from '@/shared/i18n'
import { formatDigits } from '@/shared/lib/digits'
import { Skeleton } from '@/shared/ui/skeleton'
import { useHold } from '../hooks/useHold'
import { HoldStatusBadge } from './HoldStatusBadge'

// 24-hour HH:mm in the browser's local time zone (DECISIONS T-130); digits are localised afterwards.
const clockFormat = new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', hourCycle: 'h23' })

const linkClass =
  'rounded-sm font-medium text-primary underline underline-offset-4 outline-none hover:no-underline focus-visible:ring-2 focus-visible:ring-ring'

/**
 * Route `/holds/:holdId` (UI_DESIGN §6), minimal T-130 version: class name, status and, while HELD, the expiry
 * time. The countdown (T-140) and the Pay/Cancel buttons (T-150) come later.
 */
export function HoldPage() {
  const { holdId = '' } = useParams()
  const { t, lang } = useI18n()
  const errorMessage = useErrorMessage()
  const { data, error } = useHold(holdId)

  if (data === undefined && error instanceof ApiError && error.status === 404) {
    return (
      <div className="flex flex-col items-start gap-3 py-6">
        <h1 className="text-2xl font-semibold">{t('error.HOLD_NOT_FOUND')}</h1>
        <Link to="/" className={linkClass}>
          {t('notFound.back')}
        </Link>
      </div>
    )
  }

  if (data === undefined && error) {
    // First load failed: plain words; polling keeps trying and replaces this when the server answers.
    return <p className="rounded-lg border px-4 py-10 text-center text-muted-foreground">{errorMessage(error)}</p>
  }

  if (data === undefined) {
    return <Skeleton className="h-40 max-w-xl" />
  }

  // A failed poll keeps this on screen; the offline banner explains why it isn't updating.
  return (
    <article className="flex max-w-xl flex-col gap-4 rounded-lg border bg-card p-5 shadow-xs">
      <h1 className="text-xl font-semibold break-words">
        {t('holds.heading')} — {data.className}
      </h1>
      <p className="flex items-center gap-2">
        <span className="text-muted-foreground">{t('holds.statusLabel')}</span>
        <HoldStatusBadge status={data.status} />
      </p>
      {data.status === 'HELD' && (
        <p className="tabular-nums">
          <span className="text-muted-foreground">{t('holds.expiresAt')}</span>{' '}
          <span className="font-medium">{formatDigits(clockFormat.format(new Date(data.expiresAt)), lang)}</span>
        </p>
      )}
    </article>
  )
}
