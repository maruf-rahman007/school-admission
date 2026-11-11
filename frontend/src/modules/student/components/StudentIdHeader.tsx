import { useI18n } from '@/shared/i18n'
import { Button } from '@/shared/ui/button'
import { useStudentId } from '../hooks/useStudentId'
import { openStudentIdDialog } from '../hooks/useStudentIdDialog'
import { StudentIdDialog } from './StudentIdDialog'

/** Header slot (UI_DESIGN §2): the saved ID with "Change", or a button to enter one. Hosts the dialog. */
export function StudentIdHeader() {
  const { t } = useI18n()
  const studentId = useStudentId()

  return (
    <div className="flex min-w-0 items-center gap-2">
      {studentId ? (
        <>
          <span className="min-w-0 truncate text-muted-foreground">
            {t('student.label')}{' '}
            {/* Shown as typed, in ASCII digits (UI_DESIGN §2); shortened on phones. */}
            <span className="font-medium tabular-nums tracking-wide text-foreground sm:hidden">
              {`${studentId.slice(0, 4)}…${studentId.slice(-4)}`}
            </span>
            <span className="hidden font-medium tabular-nums tracking-wide text-foreground sm:inline">{studentId}</span>
          </span>
          <Button size="sm" variant="link" className="h-auto px-1 py-0" onClick={openStudentIdDialog}>
            {t('student.change')}
          </Button>
        </>
      ) : (
        <Button size="sm" onClick={openStudentIdDialog}>
          {t('student.enter')}
        </Button>
      )}
      <StudentIdDialog />
    </div>
  )
}
