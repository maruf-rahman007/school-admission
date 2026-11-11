/** A birth registration number: exactly 17 ASCII digits (same rule as the backend's X-Student-Id check). */
export type StudentId = string

const STUDENT_ID_PATTERN = /^[0-9]{17}$/

export function isValidStudentId(value: string | null | undefined): value is StudentId {
  return typeof value === 'string' && STUDENT_ID_PATTERN.test(value)
}
