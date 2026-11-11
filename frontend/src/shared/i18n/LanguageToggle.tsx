import { cn } from '@/shared/lib/utils'
import { useI18n, type Language } from './language'

/** The `[বাংলা|EN]` switch in the header: a two-part control, the current language filled green. */
export function LanguageToggle() {
  const { lang, setLang, t } = useI18n()
  const options: Language[] = ['bn', 'en']

  return (
    <div className="inline-flex shrink-0 rounded-md border p-0.5 text-sm" role="group">
      {options.map((option) => (
        <button
          key={option}
          type="button"
          aria-pressed={lang === option}
          onClick={() => setLang(option)}
          className={cn(
            'rounded-[5px] px-2.5 py-0.5 outline-none transition-colors focus-visible:ring-2 focus-visible:ring-ring',
            lang === option ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:text-foreground',
          )}
        >
          {t(option === 'bn' ? 'lang.bn' : 'lang.en')}
        </button>
      ))}
    </div>
  )
}
