import { Link } from 'react-router'
import { useI18n } from '@/shared/i18n'

export function NotFoundPage() {
  const { t } = useI18n()
  return (
    <div className="flex flex-col items-start gap-3 py-6">
      <h1 className="text-2xl font-semibold">{t('notFound.title')}</h1>
      <Link
        to="/"
        className="rounded-sm font-medium text-primary underline underline-offset-4 outline-none hover:no-underline focus-visible:ring-2 focus-visible:ring-ring"
      >
        {t('notFound.back')}
      </Link>
    </div>
  )
}
