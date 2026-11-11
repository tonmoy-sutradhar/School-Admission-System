# AI usage

## Tools used

- **Cursor** (Composer agent) for scaffolding the Admissions module, React UI, Docker wiring, tests, and documentation against the take-home PDF / internal plan docs.

## What AI helped with

- Mapping the assignment API contract onto the existing Modular Monolith template
- Implementing domain entities, EF configurations, hold concurrency (`FOR UPDATE` + partial unique index), expiration worker, and controllers
- Building the Vite/React screens (student id bar, class list polling, hold countdown, my holds)
- Drafting `README.md`, `ASSUMPTIONS.md`, and integration/concurrency tests

## What I reviewed / own

- Final business rules (HTTP status codes, idempotent confirm, dual expiry strategy)
- Concurrency design choice (DB locks + constraints, not in-memory)
- Verifying routes match the assignment exactly (`/classes`, `/holds`, `/me/holds` without `/api/v1`)
- Ensuring Docker Compose brings up Postgres + backend (`:8080`) + frontend (`:3000`)

## Honest note

AI accelerated boilerplate and consistent structure. Design decisions, assumption documentation, and acceptance of the concurrency approach are intentional and documented in `ASSUMPTIONS.md`.
