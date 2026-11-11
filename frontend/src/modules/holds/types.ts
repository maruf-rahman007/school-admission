/** Hold status values of the contract (GET /holds/{id} reports an expired HELD hold as EXPIRED). */
export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED'

/** `GET /holds/{holdId}`: contract fields plus the extra `className` and `serverTime`. */
export interface Hold {
  holdId: string
  classId: string
  status: HoldStatus
  expiresAt: string
  className: string
  serverTime: string
}

/** One element of `GET /me/holds` (newest first; an expired HELD hold reads EXPIRED). Server time comes in the header. */
export interface MyHold {
  holdId: string
  classId: string
  className: string
  status: HoldStatus
  expiresAt: string
}

/** 201 body of `POST /classes/{classId}/holds`. */
export interface HoldCreated {
  holdId: string
  classId: string
  studentId: string
  status: HoldStatus
  expiresAt: string
  serverTime: string
}
