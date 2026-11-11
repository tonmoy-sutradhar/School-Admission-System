import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'

const STORAGE_KEY = 'school_admission_student_id'

interface StudentContextValue {
  studentId: string
  setStudentId: (value: string) => void
  isValidStudentId: boolean
}

const StudentContext = createContext<StudentContextValue | null>(null)

function isValid(id: string) {
  return /^\d{17}$/.test(id)
}

export function StudentProvider({ children }: { children: ReactNode }) {
  const [studentId, setStudentIdState] = useState(() => localStorage.getItem(STORAGE_KEY) ?? '')

  const setStudentId = useCallback((value: string) => {
    const cleaned = value.replace(/\D/g, '').slice(0, 17)
    setStudentIdState(cleaned)
    if (cleaned) localStorage.setItem(STORAGE_KEY, cleaned)
    else localStorage.removeItem(STORAGE_KEY)
  }, [])

  const value = useMemo(
    () => ({
      studentId,
      setStudentId,
      isValidStudentId: isValid(studentId),
    }),
    [studentId, setStudentId],
  )

  return <StudentContext.Provider value={value}>{children}</StudentContext.Provider>
}

export function useStudent() {
  const ctx = useContext(StudentContext)
  if (!ctx) throw new Error('useStudent must be used inside StudentProvider')
  return ctx
}
