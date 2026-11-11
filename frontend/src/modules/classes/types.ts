/** One item of `GET /classes` (contract fields, frozen). */
export interface ClassSummary {
  id: string
  name: string
  totalSeats: number
  heldSeats: number
  confirmedSeats: number
  availableSeats: number
}

/** An object response (`POST /classes`, `GET /classes/{id}`): the same fields plus `serverTime`. */
export interface ClassDetails extends ClassSummary {
  serverTime: string
}

/** Body of `POST /classes`. `totalSeats` is sent as a JSON number. */
export interface CreateClassInput {
  name: string
  totalSeats: number
}
