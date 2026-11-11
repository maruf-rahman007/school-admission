import { useSyncExternalStore } from 'react'

let open = false
const listeners = new Set<() => void>()

function setOpen(value: boolean): void {
  open = value
  listeners.forEach((listener) => listener())
}

/** Opens the student ID dialog from anywhere (Apply and /me use this in later tickets). */
export function openStudentIdDialog(): void {
  setOpen(true)
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
