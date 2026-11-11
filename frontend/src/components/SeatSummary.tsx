import type { ClassSeat } from '../types/admission'

export function SeatSummary({ cls }: { cls: ClassSeat }) {
  return (
    <div className="seat-summary">
      <span>Total: {cls.totalSeats}</span>
      <span>Held: {cls.heldSeats}</span>
      <span>Confirmed: {cls.confirmedSeats}</span>
      <span className="available">Available: {cls.availableSeats}</span>
    </div>
  )
}
