import { Loader2Icon } from 'lucide-react'
import { useI18n } from '@/shared/i18n'
import { formatDigits } from '@/shared/lib/digits'
import { Button } from '@/shared/ui/button'
import type { ClassSummary } from '../types'

interface ClassCardProps {
  schoolClass: ClassSummary
  /** Called when Apply is clicked. */
  onApply: (classId: string) => void
  /** This card's application is being sent (spinner). */
  applying: boolean
  /** Some application is being sent, or the student already has an active one: every Apply button waits (UI_DESIGN §4). */
  applyLocked: boolean
}

/** One class with its live seat counts and the Apply button (UI_DESIGN §4). */
export function ClassCard({ schoolClass, onApply, applying, applyLocked }: ClassCardProps) {
  const { t, lang } = useI18n()
  const n = (value: number) => formatDigits(value, lang)
  const { name, totalSeats, heldSeats, confirmedSeats, availableSeats } = schoolClass
  const takenPercent = Math.min(100, Math.max(0, ((heldSeats + confirmedSeats) / totalSeats) * 100))
  const full = availableSeats <= 0

  return (
    <article className="flex flex-col gap-3 rounded-lg border bg-card p-4 shadow-xs">
      <h2 className="text-lg font-semibold break-words">{name}</h2>
      <p className="text-sm text-muted-foreground tabular-nums">
        {t('classes.total')} {n(totalSeats)} · {t('classes.held')} {n(heldSeats)} · {t('classes.confirmed')}{' '}
        {n(confirmedSeats)}
      </p>
      <div className="flex items-center gap-3">
        <p className="shrink-0 font-medium tabular-nums">
          {t('classes.available')} {n(availableSeats)}
        </p>
        {/* Filled part = taken seats (held + confirmed); the numbers above say the same for screen readers. */}
        <div aria-hidden="true" className="h-2 min-w-0 flex-1 overflow-hidden rounded-full bg-muted">
          <div className="h-full rounded-full bg-primary" style={{ width: `${takenPercent}%` }} />
        </div>
      </div>
      <div className="flex justify-end">
        <Button disabled={full || applyLocked} aria-busy={applying} onClick={() => onApply(schoolClass.id)}>
          {applying && <Loader2Icon aria-hidden="true" className="animate-spin" />}
          {applying && <span className="sr-only">{t('holds.applying')}</span>}
          {full ? t('classes.full') : t('classes.apply')}
        </Button>
      </div>
    </article>
  )
}
