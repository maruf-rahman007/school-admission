export interface ApiFieldError {
  field: string
  code: string
  message: string
}

/**
 * Every failed API call ends up as this error. `status` is null when no response arrived (network down);
 * `code` is the backend's `userResponse.code`, or null when the body is not our error body.
 */
export class ApiError extends Error {
  readonly status: number | null
  readonly code: string | null
  readonly errors: ApiFieldError[]

  constructor(status: number | null, code: string | null, errors: ApiFieldError[], message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.errors = errors
  }

  get isNetworkError(): boolean {
    return this.status === null
  }

  /** Builds the error from any response body; a body that isn't our error shape gives `code: null`. */
  static fromResponse(status: number, body: unknown): ApiError {
    const user = isRecord(body) && isRecord(body.userResponse) ? body.userResponse : null
    const code = user && typeof user.code === 'string' ? user.code : null
    const message = user && typeof user.message === 'string' ? user.message : `HTTP ${status}`
    const errors = user && Array.isArray(user.errors) ? user.errors.filter(isFieldError) : []
    return new ApiError(status, code, errors, message)
  }

  static network(message: string): ApiError {
    return new ApiError(null, null, [], message)
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isFieldError(value: unknown): value is ApiFieldError {
  return isRecord(value) && typeof value.field === 'string' && typeof value.code === 'string'
    && typeof value.message === 'string'
}
