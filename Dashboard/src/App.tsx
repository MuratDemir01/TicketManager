import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout, ProtectedRoute } from './components/Layout'
import { CreateTicketPage } from './pages/CreateTicketPage'
import { LoginPage } from './pages/LoginPage'
import { TicketDetailPage } from './pages/TicketDetailPage'
import { TicketsPage } from './pages/TicketsPage'
import { UsersPage } from './pages/UsersPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<Navigate to="/tickets" replace />} />
          <Route path="/tickets" element={<TicketsPage />} />
          <Route element={<ProtectedRoute adminOnly />}>
            <Route path="/tickets/new" element={<CreateTicketPage />} />
            <Route path="/users" element={<UsersPage />} />
          </Route>
          <Route path="/tickets/:id" element={<TicketDetailPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/tickets" replace />} />
    </Routes>
  )
}
