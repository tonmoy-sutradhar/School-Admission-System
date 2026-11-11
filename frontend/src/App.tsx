import { NavLink, Route, Routes } from 'react-router-dom'
import { StudentIdBar } from './components/StudentIdBar'
import { StudentProvider } from './context/StudentContext'
import { ClassListPage } from './pages/ClassListPage'
import { CreateClassPage } from './pages/CreateClassPage'
import { HoldDetailPage } from './pages/HoldDetailPage'
import { MyHoldsPage } from './pages/MyHoldsPage'

export default function App() {
  return (
    <StudentProvider>
      <div className="app-shell">
        <header className="top">
          <div className="brand-row">
            <div className="brand">School Admission</div>
            <nav>
              <NavLink to="/" end>
                Classes
              </NavLink>
              <NavLink to="/admin/classes">Create class</NavLink>
              <NavLink to="/me/holds">My applications</NavLink>
            </nav>
          </div>
          <StudentIdBar />
        </header>
        <main>
          <Routes>
            <Route path="/" element={<ClassListPage />} />
            <Route path="/admin/classes" element={<CreateClassPage />} />
            <Route path="/holds/:holdId" element={<HoldDetailPage />} />
            <Route path="/me/holds" element={<MyHoldsPage />} />
          </Routes>
        </main>
      </div>
    </StudentProvider>
  )
}
