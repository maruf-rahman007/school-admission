import { apiClient } from '@/shared/api'
import type { ClassDetails, ClassSummary, CreateClassInput } from '../types'

export async function fetchClasses(signal?: AbortSignal): Promise<ClassSummary[]> {
  const response = await apiClient.get<ClassSummary[]>('/classes', { signal })
  return response.data
}

export async function createClass(input: CreateClassInput): Promise<ClassDetails> {
  const response = await apiClient.post<ClassDetails>('/classes', input)
  return response.data
}
