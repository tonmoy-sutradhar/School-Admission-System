# Assumptions

Decisions made where the assignment brief left room for judgment.

## Identity & admin

1. **No login.** Parents identify with a 17-digit birth registration number via `X-Student-Id`. The UI stores it in `localStorage`.
2. **No separate admin auth.** Anyone who can open the UI may create classes. This matches the “no login” requirement and keeps the demo self-contained.
3. Missing / non-17-digit `X-Student-Id` on hold/confirm/cancel/`me` endpoints → **400** with a clear message.

## Hold lifecycle

4. Default hold window is **600 seconds**, configurable with `HOLD_DURATION_SECONDS` / `Admissions:HoldDurationSeconds`.
5. Status values: `HELD`, `CONFIRMED`, `EXPIRED`, `CANCELLED`.
6. Expiry uses **both**:
   - a background worker (every ~5s) that marks due `HELD` rows as `EXPIRED`
   - **lazy expiry** on seat counts and hold reads (`expires_at <= now` never counts as held)
7. All `expiresAt` values are **UTC** (ISO-8601 with `Z` when serialized by ASP.NET).

## Concurrency & two-process safety

8. Seat safety is enforced in **PostgreSQL**, not in memory:
   - `SELECT … FOR UPDATE` on the class row inside a transaction before creating a hold
   - occupancy count = active `HELD` (`expires_at > now`) + `CONFIRMED`
   - partial unique index on `(student_id, class_id) WHERE status IN ('HELD','CONFIRMED')`
9. Therefore two backend processes sharing one DB cannot overbook; an in-process lock alone would not be enough.
10. Invariant: `heldSeats + confirmedSeats ≤ totalSeats` under concurrent load.

## Business rules / HTTP mapping

11. No seats left → **409** `"No seats available for this class."`
12. Student already has an active hold/confirmation for that class → **409**
13. Confirm someone else’s hold → **403**
14. Confirm after expiry / non-active hold → **409**
15. Confirm on an already `CONFIRMED` hold → **200** with `status: CONFIRMED` (**idempotent**; confirmed seat count does not increase again)
16. Cancel only allowed for own active `HELD` → otherwise **403** / **409**
17. Unknown class/hold id → **404**

## IDs & API shape

18. IDs are **GUID** strings (not `c_123` / `h_789` prefixes; those were examples only).
19. Admission endpoints return **raw JSON** matching the assignment fields (no `ApiResponse` envelope), so automated graders can assert on `id`, `holdId`, `status`, etc. Extra fields may appear on error bodies (`message`).
20. Template JWT/Identity modules remain in the solution for the starter architecture but are **not required** for the admission UI flow.

## Persistence

21. PostgreSQL Docker volume `pgdata` keeps data across `docker compose down` / `up`.
22. Migrations apply on startup when `Database:ApplyMigrationsOnStartup=true` (default in compose).
