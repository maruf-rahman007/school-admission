import { useSyncExternalStore } from 'react'
import { setStudentIdSource } from '@/shared/api'
import { storage } from '@/shared/lib/storage'
import { isValidStudentId, type StudentId } from '../types'

const STORAGE_KEY = 'admission.studentId'
const listeners = new Set<() => void>()

/** The saved ID, or null. A stored value that isn't 17 digits (e.g. edited by hand) counts as "no ID". */
function readStudentId(): StudentId | null {
  const stored = storage.get(STORAGE_KEY)
  return isValidStudentId(stored) ? stored : null
}

let current = readStudentId()

// The API client sends this as X-Student-Id; nothing is sent while there is no ID.
setStudentIdSource(() => current)

export function setStudentId(id: StudentId): void {
  storage.set(STORAGE_KEY, id)
  current = id
  listeners.forEach((listener) => listener())
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

/** Current student ID (kept in localStorage, so it survives refresh and browser restart). */
export function useStudentId(): StudentId | null {
  return useSyncExternalStore(subscribe, () => current)
}
