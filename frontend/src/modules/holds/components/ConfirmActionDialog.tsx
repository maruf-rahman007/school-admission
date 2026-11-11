import { Loader2Icon } from 'lucide-react'
import { useI18n } from '@/shared/i18n'
import { Button } from '@/shared/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog'

interface ConfirmActionDialogProps {
  open: boolean
  title: string
  body: string
  confirmLabel: string
  /** The request is running: spinner on the action button, both buttons disabled, and the dialog can't be closed. */
  pending: boolean
  onConfirm: () => void
  /** Called when the parent closes the dialog ([না], ✕, Esc, outside click); ignored while pending. */
  onClose: () => void
}

/** "Are you sure?" dialog used for both Pay and Cancel (UI_DESIGN §6). */
export function ConfirmActionDialog({ open, title, body, confirmLabel, pending, onConfirm, onClose }: ConfirmActionDialogProps) {
  const { t } = useI18n()
  const close = () => {
    if (!pending) {
      onClose()
    }
  }

  return (
    <Dialog open={open} onOpenChange={(next) => !next && close()}>
      <DialogContent closeLabel={t('common.close')}>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription className="break-words">{body}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button type="button" variant="outline" disabled={pending} onClick={close}>
            {t('holds.no')}
          </Button>
          <Button type="button" disabled={pending} aria-busy={pending} onClick={onConfirm}>
            {pending && <Loader2Icon aria-hidden="true" className="animate-spin" />}
            {pending && <span className="sr-only">{t('holds.sending')}</span>}
            {confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
