import { Link } from 'react-router'
import { useErrorMessage } from '@/shared/api'
import { useI18n } from '@/shared/i18n'
import { Skeleton } from '@/shared/ui/skeleton'
import { useClasses } from '../hooks/useClasses'
import { ClassCard } from './ClassCard'

/** Route `/`: every class with live seat counts. */
export function ClassListPage() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const { data, error } = useClasses()

  let content
  if (data === undefined && error) {
    // First load failed: say so in plain words; polling keeps trying and replaces this when the server answers.
    content = <p className="rounded-lg border px-4 py-10 text-center text-muted-foreground">{errorMessage(error)}</p>
  } else if (data === undefined) {
    content = (
      <div className="grid gap-4 sm:grid-cols-2">
        {[0, 1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-44" />
        ))}
      </div>
    )
  } else if (data.length === 0) {
    content = (
      <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed px-4 py-10 text-center">
        <p className="text-muted-foreground">{t('classes.empty')}</p>
        <Link to="/admin" className="font-medium text-primary underline underline-offset-4 hover:no-underline">
          {t('classes.createLink')}
        </Link>
      </div>
    )
  } else {
    // A failed poll keeps this list on screen; the offline banner explains why it isn't updating.
    content = (
      <div className="grid gap-4 sm:grid-cols-2">
        {data.map((schoolClass) => (
          <ClassCard key={schoolClass.id} schoolClass={schoolClass} />
        ))}
      </div>
    )
  }

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold">{t('classes.heading')}</h1>
      {content}
    </section>
  )
}
