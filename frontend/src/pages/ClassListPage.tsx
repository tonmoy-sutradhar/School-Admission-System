import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ApiError, apiFetch } from "../api/client";
import { ErrorBanner } from "../components/ErrorBanner";
import { SeatSummary } from "../components/SeatSummary";
import { useStudent } from "../context/StudentContext";
import type { ClassSeat, HoldDetail } from "../types/admission";

export function ClassListPage() {
  const { studentId, isValidStudentId } = useStudent();
  const navigate = useNavigate();
  const [classes, setClasses] = useState<ClassSeat[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [applyingId, setApplyingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const data = await apiFetch<ClassSeat[]>("/classes");
      setClasses(data);
      setError(null);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Could not load classes.",
      );
    }
  }, []);

  useEffect(() => {
    void load();
    const id = window.setInterval(() => void load(), 4000);
    return () => window.clearInterval(id);
  }, [load]);

  async function apply(classId: string) {
    if (!isValidStudentId) {
      setError("Enter a valid 17-digit birth registration number first.");
      return;
    }
    setApplyingId(classId);
    setError(null);
    try {
      const hold = await apiFetch<HoldDetail>(`/classes/${classId}/holds`, {
        method: "POST",
        studentId,
      });
      navigate(`/holds/${hold.holdId}`);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Could not create application.",
      );
    } finally {
      setApplyingId(null);
    }
  }

  return (
    <section className="page">
      <h1>Classes</h1>
      <p className="muted">Seat counts refresh every few seconds.</p>
      <ErrorBanner message={error} />
      {classes.length === 0 ? (
        <p>
          No classes yet. <Link to="/admin/classes">Create one</Link>.
        </p>
      ) : (
        <ul className="class-list">
          {classes.map((cls) => (
            <li key={cls.id}>
              <div>
                <h2>{cls.name}</h2>
                <SeatSummary cls={cls} />
              </div>
              <button
                type="button"
                disabled={cls.availableSeats <= 0 || applyingId === cls.id}
                onClick={() => void apply(cls.id)}
              >
                {applyingId === cls.id ? "Applying…" : "Submit Application"}
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
