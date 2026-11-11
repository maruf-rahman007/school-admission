import { useI18n } from '@/shared/i18n'
import { ClassCompactList } from './ClassCompactList'
import { CreateClassForm } from './CreateClassForm'

/** Route `/admin`: create a class, with the live list of existing classes below. */
export function AdminPage() {
  const { t } = useI18n()
  return (
    <div className="flex flex-col gap-8">
      <section className="flex flex-col gap-4">
        <h1 className="text-2xl font-semibold">{t('admin.title')}</h1>
        <CreateClassForm />
      </section>
      <hr />
      <ClassCompactList />
    </div>
  )
}
