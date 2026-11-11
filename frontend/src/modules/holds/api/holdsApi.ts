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
