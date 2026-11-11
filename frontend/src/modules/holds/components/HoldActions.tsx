import { useRef, useState } from 'react'
import { openStudentIdDialog, useStudentId } from '@/modules/student'
import { useI18n } from '@/shared/i18n'
import { Button } from '@/shared/ui/button'
import { useCancelHold } from '../hooks/useCancelHold'
import { useConfirmHold } from '../hooks/useConfirmHold'
import { ConfirmActionDialog } from './ConfirmActionDialog'

type Action = 'pay' | 'cancel'

interface HoldActionsProps {
  holdId: string
  /** Shown in the Pay dialog. */
  schoolClassName: string
  /** The effective status is HELD. When it stops being HELD the buttons go away and an idle dialog closes. */
  active: boolean
}

/**
 * Pay and Cancel on the application page (UI_DESIGN §6). Each asks for confirmation first; with no student ID saved
 * the ID dialog comes first (DECISIONS T-150). Only one request at a time; the result is a toast plus the new status.
 * Stays mounted while a request runs, so the result is still shown if the hold expires meanwhile.
 */
export function HoldActions({ holdId, schoolClassName, active }: HoldActionsProps) {
  const { t } = useI18n()
  const studentId = useStudentId()
  const confirmHold = useConfirmHold(holdId)
  const cancelHold = useCancelHold(holdId)
  const [dialogOpen, setDialogOpen] = useState(false)
  // Kept after closing, so the texts don't switch during the closing animation.
  const [action, setAction] = useState<Action>('pay')
  // Set synchronously, so a double click can't start a second request before React re-renders.
  const inFlight = useRef(false)
  const pending = confirmHold.isPending || cancelHold.isPending

  // Expiry closes an idle dialog; a running request keeps it open until the answer arrives.
  const open = dialogOpen && (active || pending)

  const ask = (next: Action) => {
    if (inFlight.current) {
      return
    }
    const show = () => {
      setAction(next)
      setDialogOpen(true)
    }
    if (studentId) {
      show()
    } else {
      // Opens the confirm dialog once a valid ID is saved; dismissing the ID dialog does nothing.
      openStudentIdDialog(show)
    }
  }

  // Toasts and cache updates are done by the hooks; here only this component's own state is reset.
  const onSettled = () => {
    inFlight.current = false
    setDialogOpen(false)
  }

  const run = () => {
    if (inFlight.current || !open) {
      return
    }
    inFlight.current = true
    if (action === 'pay') {
      confirmHold.mutate(undefined, { onSettled })
    } else {
      cancelHold.mutate(undefined, { onSettled })
    }
  }

  return (
    <>
      {active && (
        <div className="flex flex-wrap gap-2">
          <Button type="button" disabled={pending} onClick={() => ask('pay')}>
            {t('holds.pay')}
          </Button>
          <Button type="button" variant="outline" disabled={pending} onClick={() => ask('cancel')}>
            {t('holds.cancel')}
          </Button>
        </div>
      )}
      <ConfirmActionDialog
        open={open}
        title={t(action === 'pay' ? 'holds.payTitle' : 'holds.cancelTitle')}
        body={action === 'pay' ? t('holds.payBody').replace('{class}', schoolClassName) : t('holds.cancelBody')}
        confirmLabel={t(action === 'pay' ? 'holds.payConfirm' : 'holds.cancelConfirm')}
        pending={pending}
        onConfirm={run}
        onClose={() => setDialogOpen(false)}
      />
    </>
  )
}
