import { useSyncExternalStore } from 'react'

let open = false
// Something to do once a valid ID is saved (e.g. send the Apply request). Never outlives the dialog it was opened with.
let afterSave: (() => void) | null = null
const listeners = new Set<() => void>()

function setOpen(value: boolean): void {
  open = value
  if (!value) {
    afterSave = null // closing (save, cancel, ✕, Esc, outside click) always drops a pending action
  }
  listeners.forEach((listener) => listener())
}

/**
 * Opens the student ID dialog from anywhere. `onSaved` runs once, only if the parent saves a valid ID
 * (Apply uses it to send the request straight away, UI_DESIGN §3).
 */
export function openStudentIdDialog(onSaved?: () => void): void {
  afterSave = onSaved ?? null
  setOpen(true)
}

/** Takes the pending "after save" action (at most once); the dialog calls it right after saving. */
export function takeAfterSave(): (() => void) | null {
  const action = afterSave
  afterSave = null
  return action
}

export function useStudentIdDialog(): [boolean, (value: boolean) => void] {
  const isOpen = useSyncExternalStore(
    (listener) => {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
    () => open,
  )
  return [isOpen, setOpen]
}
