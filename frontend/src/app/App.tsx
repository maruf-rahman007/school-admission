import { BrowserRouter, Outlet, Route, Routes } from 'react-router'
import { StudentIdHeader } from '@/modules/student'
import { OfflineBanner } from '@/shared/ui/OfflineBanner'
import { ErrorBoundary } from './ErrorBoundary'
import { Header } from './Header'
import { NotFoundPage, PlaceholderPage } from './pages'
import { Providers } from './providers'

function Layout() {
  return (
    <div className="min-h-screen overflow-x-hidden bg-background text-foreground">
      <Header studentSlot={<StudentIdHeader />} />
      <OfflineBanner />
      <main className="mx-auto max-w-5xl px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}

export function App() {
  return (
    <ErrorBoundary>
      <Providers>
        <BrowserRouter>
          <Routes>
            <Route element={<Layout />}>
              <Route index element={<PlaceholderPage />} />
              <Route path="admin" element={<PlaceholderPage />} />
              <Route path="holds/:holdId" element={<PlaceholderPage />} />
              <Route path="me" element={<PlaceholderPage />} />
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </Providers>
    </ErrorBoundary>
  )
}
