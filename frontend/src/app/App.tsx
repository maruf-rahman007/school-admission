import { BrowserRouter, Outlet, Route, Routes } from 'react-router'
import { AdminPage, ClassListPage } from '@/modules/classes'
import { StudentIdHeader } from '@/modules/student'
import { OfflineBanner } from '@/shared/ui/OfflineBanner'
import { Toaster } from '@/shared/ui/sonner'
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
      <Toaster position="top-center" />
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
              <Route index element={<ClassListPage />} />
              <Route path="admin" element={<AdminPage />} />
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
