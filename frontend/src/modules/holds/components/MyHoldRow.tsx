import type { QueryKey } from '@tanstack/react-query'
import { Link } from 'react-router'
import { useI18n } from '@/shared/i18n'
import { formatCountdown } from '@/shared/lib/countdown'
import { useHoldCountdown } from '../hooks/useHoldCountdown'
import type { MyHold } from '../types'
import { HoldStatusBadge } from './HoldStatusBadge'

const linkClass =
  'rounded-sm font-medium text-primary underline underline-offset-4 outline-none hover:no-underline focus-visible:ring-2 focus-visible:ring-ring'

interface MyHoldRowProps {
  hold: MyHold
  /** The list query, fetched once when this row's countdown reaches 0. */
  listKey: QueryKey
}

/** One application on `/me` (UI_DESIGN §7): class, status, live countdown while HELD, and a link to it. */
export function MyHoldRow({ hold, listKey }: MyHoldRowProps) {
  const { t, lang } = useI18n()
  // Only a HELD hold counts down; other statuses pass no expiry, so no timer runs.
  const countdown = useHoldCountdown(hold.holdId, hold.status === 'HELD' ? hold.expiresAt : undefined, listKey)
  // Server CONFIRMED/CANCELLED win; a local 0 turns only HELD into EXPIRED, and stays (DECISIONS T-140/T-160).
  const status = hold.status === 'HELD' && countdown.expired ? 'EXPIRED' : hold.status

  return (
    <li className="flex flex-wrap items-center gap-x-4 gap-y-2 rounded-lg border bg-card px-4 py-3 shadow-xs">
      <span className="min-w-0 flex-1 basis-40 font-medium break-words">{hold.className}</span>
      <HoldStatusBadge status={status} />
      {status === 'HELD' && (
        // Red under one minute: from 00:59 on screen (UI_DESIGN §1).
        <span className={countdown.seconds < 60 ? 'font-medium text-destructive tabular-nums' : 'tabular-nums'}>
          {formatCountdown(countdown.seconds, lang)}
        </span>
      )}
      <Link to={`/holds/${hold.holdId}`} className={linkClass}>
        {t('me.view')}
      </Link>
    </li>
  )
}
