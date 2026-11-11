import { useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { useActiveHold, useCreateHold } from '@/modules/holds'
import { openStudentIdDialog, useStudentId } from '@/modules/student'
import { ApiError, useErrorMessage } from '@/shared/api'
import { useI18n } from '@/shared/i18n'
import { Skeleton } from '@/shared/ui/skeleton'
import { classesQueryKey, useClasses } from '../hooks/useClasses'
import { ClassCard } from './ClassCard'

/**
 * Apply flow (UI_DESIGN §3, §4): asks for the student ID first if none is saved, sends one request at a time,
 * opens the application on success and explains any failure in a toast. Counts refresh either way.
 */
function useApply() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const studentId = useStudentId()
  const createHold = useCreateHold()
  const [applyingClassId, setApplyingClassId] = useState<string | null>(null)
  // Set synchronously, so a double click can't start a second request before React re-renders.
  const inFlight = useRef(false)

  const send = (classId: string) => {
    if (inFlight.current) {
      return
    }
    inFlight.current = true
    setApplyingClassId(classId)
    createHold.mutate(classId, {
      onSuccess: (hold) => navigate(`/holds/${hold.holdId}`),
      onError: (error) =>
        toast.error(error instanceof ApiError && error.isNetworkError ? t('error.NETWORK_SUBMIT') : errorMessage(error)),
      onSettled: () => {
        inFlight.current = false
        setApplyingClassId(null)
        void queryClient.invalidateQueries({ queryKey: classesQueryKey })
      },
    })
  }

  const apply = (classId: string) => {
    if (inFlight.current) {
      return
    }
    if (studentId) {
      send(classId)
    } else {
      // Sent automatically once a valid ID is saved; closing the dialog any other way sends nothing.
      openStudentIdDialog(() => send(classId))
    }
  }

  return { apply, applyingClassId }
}

/** Route `/`: every class with live seat counts. */
export function ClassListPage() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const { data, error } = useClasses()
  const { apply, applyingClassId } = useApply()
  // Undefined until /me/holds has answered (or with no ID): Apply stays enabled and the server's 409 is the fallback.
  const activeHold = useActiveHold()

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
          <ClassCard
            key={schoolClass.id}
            schoolClass={schoolClass}
            onApply={apply}
            applying={applyingClassId === schoolClass.id}
            applyLocked={applyingClassId !== null || activeHold !== undefined}
          />
        ))}
      </div>
    )
  }

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold">{t('classes.heading')}</h1>
      {activeHold && (
        // One active application per student (UI_DESIGN §4): every Apply is disabled while this shows.
        <p className="flex flex-wrap items-center gap-x-2 gap-y-1 rounded-lg border border-primary/30 bg-primary/5 px-4 py-3">
          <span>{t('classes.activeBanner')}</span>
          <Link
            to={`/holds/${activeHold.holdId}`}
            className="rounded-sm font-medium text-primary underline underline-offset-4 outline-none hover:no-underline focus-visible:ring-2 focus-visible:ring-ring"
          >
            {t('classes.activeBannerLink')}
          </Link>
        </p>
      )}
      {content}
    </section>
  )
}
