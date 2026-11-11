import { useErrorMessage } from '@/shared/api'
import { useI18n } from '@/shared/i18n'
import { formatDigits } from '@/shared/lib/digits'
import { Skeleton } from '@/shared/ui/skeleton'
import { useClasses } from '../hooks/useClasses'

/** Existing classes under the admin form: same live query as `/`, compact rows, no Apply buttons (UI_DESIGN §5). */
export function ClassCompactList() {
  const { t, lang } = useI18n()
  const errorMessage = useErrorMessage()
  const { data, error } = useClasses()

  let content
  if (data === undefined && error) {
    content = <p className="text-muted-foreground">{errorMessage(error)}</p>
  } else if (data === undefined) {
    content = (
      <div className="flex flex-col gap-2">
        {[0, 1, 2].map((i) => (
          <Skeleton key={i} className="h-6" />
        ))}
      </div>
    )
  } else if (data.length === 0) {
    content = <p className="text-muted-foreground">{t('classes.empty')}</p>
  } else {
    content = (
      <ul className="divide-y rounded-lg border">
        {data.map((c) => (
          <li key={c.id} className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 px-4 py-2">
            <span className="min-w-0 font-medium break-words">{c.name}</span>
            <span className="text-sm text-muted-foreground tabular-nums">
              {t('classes.total')} {formatDigits(c.totalSeats, lang)} · {t('admin.availableShort')}{' '}
              {formatDigits(c.availableSeats, lang)}
            </span>
          </li>
        ))}
      </ul>
    )
  }

  return (
    <section className="flex flex-col gap-3">
      <h2 className="text-lg font-semibold">{t('admin.existing')}</h2>
      {content}
    </section>
  )
}
