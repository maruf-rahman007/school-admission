import { Component, type ErrorInfo, type ReactNode } from 'react'
import { readStoredLanguage, translate } from '@/shared/i18n'
import { BrandMark } from '@/shared/ui/BrandMark'

interface Props {
  children: ReactNode
}

interface State {
  crashed: boolean
}

/**
 * Wraps the whole app (outside every provider) so a render crash shows the crash screen (UI_DESIGN §9)
 * instead of a blank page. It reads the language straight from storage because providers may be broken.
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { crashed: false }

  static getDerivedStateFromError(): State {
    return { crashed: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Render crash', error, info.componentStack)
  }

  render() {
    if (!this.state.crashed) {
      return this.props.children
    }

    const lang = readStoredLanguage()
    return (
      <main className="flex min-h-screen flex-col items-center justify-center gap-5 p-4 text-center">
        <BrandMark className="size-12 text-2xl" />
        <h1 className="text-xl font-semibold">{translate(lang, 'crash.title')}</h1>
        <button
          type="button"
          onClick={() => window.location.reload()}
          className="rounded-md bg-primary px-5 py-2 font-medium text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        >
          {translate(lang, 'crash.refresh')}
        </button>
      </main>
    )
  }
}
