import { useEffect, useRef } from 'react'
import { Link } from 'react-router'
import { openStudentIdDialog } from '@/modules/student'
import { useErrorMessage } from '@/shared/api'
import { useI18n } from '@/shared/i18n'
import { Button } from '@/shared/ui/button'
import { Skeleton } from '@/shared/ui/skeleton'
import { myHoldsQueryKey, useMyHolds } from '../hooks/useMyHolds'
import { MyHoldRow } from './MyHoldRow'

const linkClass =
  'rounded-sm font-medium text-primary underline underline-offset-4 outline-none hover:no-underline focus-visible:ring-2 focus-visible:ring-ring'

/**
 * Route `/me` (UI_DESIGN §7): every application of the saved student, newest first, polled every 3 s. With no ID saved,
 * the ID dialog opens once on arrival; if it is dismissed, a message and a button to reopen it are shown.
 */
export function MyApplicationsPage() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const { data, error, studentId } = useMyHolds()

  // Once per visit (the ref survives StrictMode's effect re-run and later re-renders).
  const askedForId = useRef(false)
  useEffect(() => {
    if (studentId === null && !askedForId.current) {
      askedForId.current = true
      openStudentIdDialog()
    }
  }, [studentId])

  let content
  if (studentId === null) {
    // The query is disabled without an ID: never show skeletons here.
    content = (
      <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed px-4 py-10 text-center">
        <p className="text-muted-foreground">{t('me.noId')}</p>
        <Button onClick={() => openStudentIdDialog()}>{t('student.enter')}</Button>
      </div>
    )
  } else if (data === undefined && error) {
    // First load failed: plain words; polling keeps trying and replaces this when the server answers.
    content = <p className="rounded-lg border px-4 py-10 text-center text-muted-foreground">{errorMessage(error)}</p>
  } else if (data === undefined) {
    content = (
      <div className="flex flex-col gap-3">
        {[0, 1, 2].map((i) => (
          <Skeleton key={i} className="h-14" />
        ))}
      </div>
    )
  } else if (data.length === 0) {
    content = (
      <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed px-4 py-10 text-center">
        <p className="text-muted-foreground">{t('me.empty')}</p>
        <Link to="/" className={linkClass}>
          {t('me.emptyLink')}
        </Link>
      </div>
    )
  } else {
    // Rows in API order (newest first); a failed poll keeps them on screen and the offline banner explains.
    const listKey = myHoldsQueryKey(studentId)
    content = (
      <ul className="flex flex-col gap-3">
        {data.map((hold) => (
          <MyHoldRow key={hold.holdId} hold={hold} listKey={listKey} />
        ))}
      </ul>
    )
  }

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold">{t('me.heading')}</h1>
      {content}
    </section>
  )
}
