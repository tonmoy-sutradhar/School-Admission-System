# School Admission API — Postman Guide

Base URL: `http://localhost:8080`

সব response JSON। Student endpoints-এ header লাগবে:

| Header | Value |
|--------|--------|
| `Content-Type` | `application/json` |
| `X-Student-Id` | `12345678901234567` (ঠিক **১৭ digit**) |

> Demo student id: `12345678901234567`  
> আরেকটা student: `76543210987654321`

---

## 1) Create class

- **Method:** `POST`
- **URL:** `http://localhost:8080/classes`
- **Headers:** `Content-Type: application/json`
- **Body (raw JSON):**

```json
{
  "name": "Class 6 - Morning Shift",
  "totalSeats": 40
}
```

- **Expected:** `201`

```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "name": "Class 6 - Morning Shift",
  "totalSeats": 40
}
```

`id` কপি করে পরের request-এ `{{classId}}` হিসেবে ব্যবহার করো।

---

## 2) List classes

- **Method:** `GET`
- **URL:** `http://localhost:8080/classes`
- **Body:** নেই
- **Expected:** `200` (array)

```json
[
  {
    "id": "...",
    "name": "Class 6 - Morning Shift",
    "totalSeats": 40,
    "heldSeats": 0,
    "confirmedSeats": 0,
    "availableSeats": 40
  }
]
```

---

## 3) Get one class

- **Method:** `GET`
- **URL:** `http://localhost:8080/classes/{{classId}}`
- **Example:** `http://localhost:8080/classes/a1b2c3d4-e5f6-7890-abcd-ef1234567890`
- **Body:** নেই
- **Expected:** `200`

---

## 4) Create hold (apply)

- **Method:** `POST`
- **URL:** `http://localhost:8080/classes/{{classId}}/holds`
- **Headers:**
  - `X-Student-Id: 12345678901234567`
- **Body:** নেই (empty)
- **Expected:** `201`

```json
{
  "holdId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "classId": "...",
  "studentId": "12345678901234567",
  "status": "HELD",
  "expiresAt": "2026-11-11T03:30:00Z"
}
```

Seat না থাকলে / already active application থাকলে → `409`

`holdId` কপি করে পরের request-এ ব্যবহার করো।

---

## 5) Get hold status

- **Method:** `GET`
- **URL:** `http://localhost:8080/holds/{{holdId}}`
- **Body:** নেই
- **Expected:** `200`

```json
{
  "holdId": "...",
  "classId": "...",
  "studentId": "12345678901234567",
  "status": "HELD",
  "expiresAt": "2026-11-11T03:30:00Z"
}
```

`status` হতে পারে: `HELD` | `CONFIRMED` | `EXPIRED` | `CANCELLED`

---

## 6) Confirm hold (fee paid)

- **Method:** `POST`
- **URL:** `http://localhost:8080/holds/{{holdId}}/confirm`
- **Headers:**
  - `X-Student-Id: 12345678901234567`
- **Body:** নেই
- **Expected:** `200`

```json
{
  "holdId": "...",
  "status": "CONFIRMED"
}
```

আবার একই confirm → আবার `200` (idempotent)।

---

## 7) Cancel hold

- **Method:** `DELETE`
- **URL:** `http://localhost:8080/holds/{{holdId}}`
- **Headers:**
  - `X-Student-Id: 12345678901234567`
- **Body:** নেই
- **Expected:** `204` (no body)

---

## 8) My holds

- **Method:** `GET`
- **URL:** `http://localhost:8080/me/holds`
- **Headers:**
  - `X-Student-Id: 12345678901234567`
- **Body:** নেই
- **Expected:** `200`

```json
[
  {
    "holdId": "...",
    "classId": "...",
    "className": "Class 6 - Morning Shift",
    "status": "CONFIRMED",
    "expiresAt": "2026-11-11T03:30:00Z"
  }
]
```

---

## Suggested Postman flow

1. `POST /classes` → class তৈরি  
2. `GET /classes` → seat দেখো  
3. `POST /classes/{classId}/holds` + `X-Student-Id` → hold নাও  
4. `GET /holds/{holdId}` → status `HELD`  
5. `POST /holds/{holdId}/confirm` → `CONFIRMED`  
6. `GET /me/holds` → নিজের আবেদন দেখো  

Cancel টেস্ট করতে চাইলে confirm-এর আগে step 5-এর জায়গায় `DELETE /holds/{holdId}` চালাও।

---

## Health check (optional)

- **GET** `http://localhost:8080/health`

---

## Common errors

| Case | Status |
|------|--------|
| Invalid / missing `X-Student-Id` | `400` |
| Class / hold not found | `404` |
| No seats / duplicate active apply / expired confirm | `409` |
| Confirm/cancel অন্যের hold | `403` |
