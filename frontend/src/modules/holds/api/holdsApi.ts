import { apiClient } from '@/shared/api'
import type { Hold, HoldCreated } from '../types'

export async function fetchHold(holdId: string, signal?: AbortSignal): Promise<Hold> {
  const response = await apiClient.get<Hold>(`/holds/${encodeURIComponent(holdId)}`, { signal })
  return response.data
}

/** Applies for a seat; the API client adds `X-Student-Id`. No request body. */
export async function createHold(classId: string): Promise<HoldCreated> {
  const response = await apiClient.post<HoldCreated>(`/classes/${encodeURIComponent(classId)}/holds`)
  return response.data
}

/** Pays for (confirms) the hold; 200 returns the hold with the same fields as `GET /holds/{id}`. No request body. */
export async function confirmHold(holdId: string): Promise<Hold> {
  const response = await apiClient.post<Hold>(`/holds/${encodeURIComponent(holdId)}/confirm`)
  return response.data
}

/** Cancels the hold; 204 with no body (also for a hold that was already expired or cancelled). */
export async function cancelHold(holdId: string): Promise<void> {
  await apiClient.delete(`/holds/${encodeURIComponent(holdId)}`)
}
