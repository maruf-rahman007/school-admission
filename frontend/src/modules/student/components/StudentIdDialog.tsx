import { useState, type FormEvent } from 'react'
import { useI18n } from '@/shared/i18n'
import { normalizeDigits } from '@/shared/lib/digits'
import { Button } from '@/shared/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/shared/ui/dialog'
import { Input } from '@/shared/ui/input'
import { setStudentId, useStudentId } from '../hooks/useStudentId'
import { useStudentIdDialog } from '../hooks/useStudentIdDialog'
import { isValidStudentId } from '../types'

/** "Enter birth registration number" dialog (UI_DESIGN §3). Accepts Bangla or English digits, saves ASCII. */
export function StudentIdDialog() {
  const [open, setOpen] = useStudentIdDialog()
  const studentId = useStudentId()
  const { t } = useI18n()

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent aria-describedby={undefined} closeLabel={t('common.close')}>
        {/* Remounted on every open, so the field starts from the saved ID and a cancelled edit is dropped. */}
        {open && <StudentIdForm initial={studentId ?? ''} onDone={() => setOpen(false)} />}
      </DialogContent>
    </Dialog>
  )
}

function StudentIdForm({ initial, onDone }: { initial: string; onDone: () => void }) {
  const { t } = useI18n()
  const [value, setValue] = useState(initial)
  const normalized = normalizeDigits(value)
  const valid = isValidStudentId(normalized)

  const save = (event: FormEvent) => {
    event.preventDefault()
    if (valid) {
      setStudentId(normalized)
      onDone()
    }
  }

  return (
    <form onSubmit={save} className="flex flex-col gap-4">
      <DialogHeader>
        <DialogTitle>{t('student.dialogTitle')}</DialogTitle>
      </DialogHeader>
      <div className="flex flex-col gap-1">
        <div className="flex items-center gap-2">
          <Input
            autoFocus
            inputMode="numeric"
            autoComplete="off"
            value={value}
            onChange={(event) => setValue(event.target.value)}
            className="h-11 text-lg tabular-nums tracking-wider"
            aria-invalid={!valid}
            aria-describedby="student-id-warning"
          />
          <span className="shrink-0 text-sm text-muted-foreground">{t('student.hint')}</span>
        </div>
        {!valid && (
          <p id="student-id-warning" className="text-sm text-destructive">
            ⚠ {t('student.invalid')}
          </p>
        )}
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          {t('common.cancel')}
        </Button>
        <Button type="submit" disabled={!valid}>
          {t('common.save')}
        </Button>
      </DialogFooter>
    </form>
  )
}
