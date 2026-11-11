import axios, { AxiosError } from 'axios'
import { ApiError } from './ApiError'
import { API_BASE_URL } from './config'
import { setOffline } from './connectivity'
import { SERVER_TIME_HEADER, recordServerTime } from './serverTime'

export const STUDENT_ID_HEADER = 'X-Student-Id'

let studentIdSource: () => string | null = () => null

/**
 * Lets the student module supply the current ID without `shared/` importing a module (STANDARDS §1b).
 * The header is only sent when the source returns an ID.
 */
export function setStudentIdSource(source: () => string | null): void {
  studentIdSource = source
}

/** The one HTTP client of the app (STANDARDS §3). Failures always reject with {@link ApiError}. */
export const apiClient = axios.create({ baseURL: API_BASE_URL })

apiClient.interceptors.request.use((config) => {
  const studentId = studentIdSource()
  if (studentId) {
    config.headers.set(STUDENT_ID_HEADER, studentId)
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => {
    recordServerTime(response.headers[SERVER_TIME_HEADER])
    setOffline(false)
    return response
  },
  (error: unknown) => {
    // A request cancelled by the caller (e.g. TanStack Query) says nothing about the server.
    if (axios.isCancel(error) || !(error instanceof AxiosError)) {
      return Promise.reject(error)
    }
    if (!error.response) {
      setOffline(true)
      return Promise.reject(ApiError.network(error.message))
    }
    recordServerTime(error.response.headers[SERVER_TIME_HEADER])
    setOffline(false)
    return Promise.reject(ApiError.fromResponse(error.response.status, error.response.data))
  },
)
