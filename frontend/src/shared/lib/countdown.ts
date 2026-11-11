import { formatDigits } from './digits'

/**
 * Whole seconds left until `expiresAtMs`, rounded up, never below 0. Rounding up means a countdown shows `00:01`
 * until the very end and reaches 0 only at the moment of expiry (DECISIONS T-140).
 */
export function remainingSeconds(expiresAtMs: number, nowMs: number): number {
  return Math.max(0, Math.ceil((expiresAtMs - nowMs) / 1000))
}

const pad = (value: number) => String(value).padStart(2, '0')

/** `mm:ss` below one hour, `h:mm:ss` from one hour (582 → `09:42`, 3725 → `1:02:05`), in the language's digits. */
export function formatCountdown(seconds: number, lang: 'bn' | 'en'): string {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const rest = seconds % 60
  const text = hours > 0 ? `${hours}:${pad(minutes)}:${pad(rest)}` : `${pad(minutes)}:${pad(rest)}`
  return formatDigits(text, lang)
}
