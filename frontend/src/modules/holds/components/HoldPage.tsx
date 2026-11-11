import { Link, useParams } from 'react-router'
import { ApiError, useErrorMessage } from '@/shared/api'
import { useI18n } from '@/shared/i18n'
import { formatCountdown } from '@/shared/lib/countdown'
import { Skeleton } from '@/shared/ui/skeleton'
import { useHold } from '../hooks/useHold'
import { useHoldCountdown } from '../hooks/useHoldCountdown'
import { HoldStatusBadge } from './HoldStatusBadge'

const linkClass =
  'rounded-sm font-medium text-primary underline underline-offset-4 outline-none hover:no-underline focus-visible:ring-2 focus-visible:ring-ring'

/**
 * Route `/holds/:holdId` (UI_DESIGN §6): class name, status and, while HELD, a live countdown. When the countdown
 * reaches 0 (or the server says EXPIRED) the page switches at once to the EXPIRED view, without a reload, and stays
 * there (DECISIONS T-140). The Pay/Cancel buttons (T-150) come later.
 */
export function HoldPage() {
  const { holdId = '' } = useParams()
  const { t, lang } = useI18n()
  const errorMessage = useErrorMessage()
  const { data, error } = useHold(holdId)
  // Called on every render (rules of hooks); without data it just waits.
  const countdown = useHoldCountdown(holdId, data?.expiresAt)

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

  // A server CONFIRMED/CANCELLED wins; otherwise the local countdown can turn HELD into EXPIRED before the server does.
  const status = data.status === 'HELD' && countdown.expired ? 'EXPIRED' : data.status

  // A failed poll keeps this on screen (the countdown keeps ticking); the offline banner explains why.
  return (
    <article className="flex max-w-xl flex-col gap-4 rounded-lg border bg-card p-5 shadow-xs">
      <h1 className="text-xl font-semibold break-words">
        {t('holds.heading')} — {data.className}
      </h1>
      <p className="flex items-center gap-2">
        <span className="text-muted-foreground">{t('holds.statusLabel')}</span>
        <HoldStatusBadge status={status} />
      </p>
      {status === 'HELD' && (
        <p className="tabular-nums">
          <span className="text-muted-foreground">{t('holds.timeLeft')}</span>{' '}
          {/* Red under one minute: from 00:59 on screen (UI_DESIGN §1). */}
          <span className={countdown.seconds < 60 ? 'font-medium text-destructive' : 'font-medium'}>
            {formatCountdown(countdown.seconds, lang)}
          </span>
        </p>
      )}
      {status === 'EXPIRED' && (
        <div className="flex flex-col items-start gap-2">
          <p className="text-muted-foreground">{t('holds.expiredMessage')}</p>
          <Link to="/" className={linkClass}>
            {t('holds.applyAgain')}
          </Link>
        </div>
      )}
    </article>
  )
}
