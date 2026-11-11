import { useSyncExternalStore } from 'react'

/**
 * Online/offline state, driven only by API results: a request with no response sets offline,
 * the next response of any status sets online. Starts online, so nothing flashes on load.
 */
let offline = false
const listeners = new Set<() => void>()

export function setOffline(value: boolean): void {
  if (offline !== value) {
    offline = value
    listeners.forEach((listener) => listener())
  }
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export function useIsOffline(): boolean {
  return useSyncExternalStore(subscribe, () => offline)
}
