import { useState, type ReactNode } from 'react'
import { MenuIcon, XIcon } from 'lucide-react'
import { NavLink } from 'react-router'
import { LanguageToggle, useI18n, type MessageKey } from '@/shared/i18n'
import { cn } from '@/shared/lib/utils'
import { BrandMark } from '@/shared/ui/BrandMark'

const links: { to: string; label: MessageKey }[] = [
  { to: '/', label: 'nav.classes' },
  { to: '/me', label: 'nav.myApplications' },
  { to: '/admin', label: 'nav.createClass' },
]

const focusRing = 'outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2'

/**
 * Top bar (UI_DESIGN §2): brand, nav, language toggle, and a second strip with the student ID.
 * The active page is marked with a green underline; on phones the nav folds into ☰.
 */
export function Header({ studentSlot }: { studentSlot?: ReactNode }) {
  const { t } = useI18n()
  const [menuOpen, setMenuOpen] = useState(false)

  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex h-14 max-w-5xl items-center gap-6 px-4">
        <NavLink to="/" className={cn('flex shrink-0 items-center gap-2 rounded-md', focusRing)}>
          <BrandMark />
          <span className="text-xl font-semibold">{t('app.brand')}</span>
        </NavLink>

        <nav className="hidden h-full items-stretch gap-5 md:flex">
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end
              className={({ isActive }) =>
                cn(
                  'relative flex items-center text-muted-foreground transition-colors hover:text-foreground',
                  'after:absolute after:inset-x-0 after:-bottom-px after:h-0.5 after:rounded-full',
                  focusRing,
                  isActive && 'text-foreground after:bg-primary',
                )
              }
            >
              {t(link.label)}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-2">
          <LanguageToggle />
          <button
            type="button"
            className={cn('rounded-md p-1.5 hover:bg-muted md:hidden', focusRing)}
            aria-label={t('nav.menu')}
            aria-expanded={menuOpen}
            onClick={() => setMenuOpen((open) => !open)}
          >
            {menuOpen ? <XIcon className="size-5" /> : <MenuIcon className="size-5" />}
          </button>
        </div>
      </div>

      {menuOpen && (
        <nav className="mx-auto flex max-w-5xl flex-col border-t px-2 py-2 md:hidden">
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end
              onClick={() => setMenuOpen(false)}
              className={({ isActive }) =>
                cn(
                  'rounded-md border-l-2 border-transparent px-3 py-2.5 hover:bg-muted',
                  focusRing,
                  isActive && 'border-primary bg-accent font-medium text-accent-foreground',
                )
              }
            >
              {t(link.label)}
            </NavLink>
          ))}
        </nav>
      )}

      {studentSlot && (
        <div className="border-t bg-muted">
          <div className="mx-auto flex max-w-5xl justify-end px-4 py-1.5 text-sm">{studentSlot}</div>
        </div>
      )}
    </header>
  )
}
