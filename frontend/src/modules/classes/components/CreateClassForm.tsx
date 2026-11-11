import { useState, type FormEvent } from 'react'
import { Loader2Icon } from 'lucide-react'
import { toast } from 'sonner'
import { ApiError, useErrorMessage } from '@/shared/api'
import { useI18n } from '@/shared/i18n'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { Label } from '@/shared/ui/label'
import { useCreateClass } from '../hooks/useCreateClass'

const MAX_SEATS = 2147483647

/** Whole number 1..2147483647 in English digits; surrounding spaces and leading zeros are fine ("05" → 5). */
export function parseSeats(raw: string): number | null {
  const trimmed = raw.trim()
  if (!/^[0-9]+$/.test(trimmed)) {
    return null
  }
  const value = Number(trimmed)
  return value >= 1 && value <= MAX_SEATS ? value : null
}

type Field = 'name' | 'seats'
const noFlags = { name: false, seats: false }

/** Admin form (UI_DESIGN §5). Errors show once a field was left or the form was submitted. */
export function CreateClassForm() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const createClass = useCreateClass()
  const [name, setName] = useState('')
  const [seats, setSeats] = useState('')
  const [touched, setTouched] = useState(noFlags)
  const [submitted, setSubmitted] = useState(false)
  const [serverErrors, setServerErrors] = useState(noFlags)

  const nameValid = name.trim().length > 0
  const seatsValue = parseSeats(seats)
  const show = (field: Field, valid: boolean) => serverErrors[field] || ((touched[field] || submitted) && !valid)
  const showNameError = show('name', nameValid)
  const showSeatsError = show('seats', seatsValue !== null)

  const edit = (field: Field, value: string) => {
    if (field === 'name') setName(value)
    else setSeats(value)
    // A server error belongs to the value that was sent; editing the field clears it.
    setServerErrors((current) => ({ ...current, [field]: false }))
  }

  const reset = () => {
    setName('')
    setSeats('')
    setTouched(noFlags)
    setSubmitted(false)
    setServerErrors(noFlags)
  }

  const handleError = (error: unknown) => {
    if (error instanceof ApiError && error.isNetworkError) {
      toast.error(t('error.NETWORK_SUBMIT'))
      return
    }
    const fields = error instanceof ApiError && error.code === 'VALIDATION_FAILED' ? error.errors.map((e) => e.field) : []
    const known = { name: fields.includes('name'), seats: fields.includes('totalSeats') }
    if (known.name || known.seats) {
      setServerErrors(known)
    } else {
      toast.error(errorMessage(error))
    }
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (createClass.isPending) {
      return // double click / double Enter: one request only
    }
    setSubmitted(true)
    if (!nameValid || seatsValue === null) {
      return
    }
    createClass.mutate(
      { name, totalSeats: seatsValue },
      {
        onSuccess: () => {
          toast.success(t('admin.created'))
          reset()
        },
        onError: handleError,
      },
    )
  }

  return (
    <form onSubmit={submit} noValidate className="flex max-w-md flex-col gap-4">
      <div className="flex flex-col gap-2">
        <Label htmlFor="class-name">{t('admin.nameLabel')}</Label>
        <Input
          id="class-name"
          value={name}
          onChange={(event) => edit('name', event.target.value)}
          onBlur={() => setTouched((current) => ({ ...current, name: true }))}
          aria-invalid={showNameError}
          aria-describedby={showNameError ? 'class-name-error' : undefined}
        />
        {showNameError && (
          <p id="class-name-error" className="text-sm text-destructive">
            {t('admin.nameError')}
          </p>
        )}
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="class-seats">{t('admin.seatsLabel')}</Label>
        <Input
          id="class-seats"
          inputMode="numeric"
          autoComplete="off"
          value={seats}
          onChange={(event) => edit('seats', event.target.value)}
          onBlur={() => setTouched((current) => ({ ...current, seats: true }))}
          className="max-w-40 tabular-nums"
          aria-invalid={showSeatsError}
          aria-describedby={showSeatsError ? 'class-seats-error' : undefined}
        />
        {showSeatsError && (
          <p id="class-seats-error" className="text-sm text-destructive">
            {t('admin.seatsError')}
          </p>
        )}
      </div>
      <div>
        <Button type="submit" disabled={createClass.isPending}>
          {createClass.isPending && <Loader2Icon aria-hidden="true" className="animate-spin" />}
          {t('admin.submit')}
        </Button>
      </div>
    </form>
  )
}
