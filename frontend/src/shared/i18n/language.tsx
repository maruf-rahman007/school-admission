import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { storage } from '@/shared/lib/storage'
import { bn, type MessageKey } from './bn'
import { en } from './en'

export type Language = 'bn' | 'en'

const STORAGE_KEY = 'admission.lang'
const dictionaries = { bn, en }

/** The saved language, or bn (the default) when nothing valid is stored. Safe to call outside React. */
export function readStoredLanguage(): Language {
  const stored = storage.get(STORAGE_KEY)
  return stored === 'en' || stored === 'bn' ? stored : 'bn'
}

/** Looks up a text without React context (used by the crash screen, which may render outside providers). */
export function translate(lang: Language, key: MessageKey): string {
  return dictionaries[lang][key]
}

/** Dictionary key for an API error code; unknown or missing codes fall back to the generic server text. */
export function errorMessageKey(code: string | null | undefined): MessageKey {
  const key = `error.${code ?? ''}`
  return key in bn ? (key as MessageKey) : 'error.INTERNAL_ERROR'
}

interface I18nContextValue {
  lang: Language
  setLang: (lang: Language) => void
  t: (key: MessageKey) => string
}

const I18nContext = createContext<I18nContextValue | null>(null)

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Language>(readStoredLanguage)

  const setLang = useCallback((next: Language) => {
    storage.set(STORAGE_KEY, next)
    setLangState(next)
  }, [])

  useEffect(() => {
    document.documentElement.lang = lang
  }, [lang])

  const value = useMemo<I18nContextValue>(
    () => ({ lang, setLang, t: (key) => translate(lang, key) }),
    [lang, setLang],
  )

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}

export function useI18n(): I18nContextValue {
  const value = useContext(I18nContext)
  if (!value) {
    throw new Error('useI18n must be used inside <LanguageProvider>')
  }
  return value
}
