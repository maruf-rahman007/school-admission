const BANGLA_DIGITS = '০১২৩৪৫৬৭৮৯'

/** Replaces ASCII digits with Bangla digits; everything else is kept (`09:42` → `০৯:৪২`). */
export function toBanglaDigits(value: string): string {
  return value.replace(/[0-9]/g, (digit) => BANGLA_DIGITS[Number(digit)])
}

/** Shows a number or text in the digits of the current language: Bangla digits in bn, ASCII in en. */
export function formatDigits(value: string | number, lang: 'bn' | 'en'): string {
  const text = String(value)
  return lang === 'bn' ? toBanglaDigits(text) : text
}

/**
 * Replaces Bangla digits with ASCII digits and keeps every other character, so the caller still validates
 * (`০০১২` → `0012`; leading zeros are kept).
 */
export function normalizeDigits(value: string): string {
  return value.replace(/[০-৯]/g, (digit) => String(BANGLA_DIGITS.indexOf(digit)))
}
