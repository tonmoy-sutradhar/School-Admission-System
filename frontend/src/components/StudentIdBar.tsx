import { useStudent } from '../context/StudentContext'

export function StudentIdBar() {
  const { studentId, setStudentId, isValidStudentId } = useStudent()

  return (
    <div className="student-bar">
      <label htmlFor="student-id">
        জন্ম নিবন্ধন নম্বর (১৭ অঙ্ক)
      </label>
      <input
        id="student-id"
        inputMode="numeric"
        autoComplete="off"
        placeholder="17-digit birth registration number"
        value={studentId}
        onChange={(e) => setStudentId(e.target.value)}
        maxLength={17}
      />
      <span className={isValidStudentId ? 'ok' : 'warn'}>
        {isValidStudentId ? 'Ready' : 'Enter 17 digits to apply / pay'}
      </span>
    </div>
  )
}
