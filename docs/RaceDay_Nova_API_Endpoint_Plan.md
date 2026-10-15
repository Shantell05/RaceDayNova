# RaceDay Nova – REST API Endpoint Plan

All application endpoints are intended to use an `/api` prefix. Authentication is planned for Part 2 using a secure token-based mechanism. Role checks must be enforced at the API layer.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Create a participant account | Public | name, email, password, mobile | 201 Created / 400 Bad Request |
| POST | `/api/auth/login` | Authenticate a user | Public | email, password | 200 OK with token / 401 Unauthorized |
| GET | `/api/users/me` | Return the logged-in user's profile | Participant/Organiser | None | 200 OK / 401 Unauthorized |
| PUT | `/api/users/me` | Update the logged-in user's profile | Participant/Organiser | profile fields | 200 OK / 400 Bad Request |
| GET | `/api/events` | Browse events, with optional filters | Public | Query parameters | 200 OK with event list |
| GET | `/api/events/{id}` | View one event and its categories | Public | None | 200 OK / 404 Not Found |
| POST | `/api/events` | Create a new event | Organiser | event fields | 201 Created / 403 Forbidden |
| PUT | `/api/events/{id}` | Edit an event owned by organiser | Organiser | event fields | 200 OK / 403 / 404 |
| DELETE | `/api/events/{id}` | Delete/cancel an event | Organiser | None | 204 No Content / 403 / 404 |
| GET | `/api/categories` | List available categories | Public | None | 200 OK |
| POST | `/api/categories` | Create a reusable category | Organiser | category fields | 201 Created / 403 Forbidden |
| PUT | `/api/categories/{id}` | Update a category | Organiser | category fields | 200 OK / 403 / 404 |
| DELETE | `/api/categories/{id}` | Remove an unused category | Organiser | None | 204 / 409 Conflict |
| GET | `/api/events/{id}/categories` | List categories offered by an event | Public | None | 200 OK |
| POST | `/api/events/{id}/categories` | Add a category to an event | Organiser | categoryId, fee, capacity | 201 Created / 403 / 409 |
| DELETE | `/api/events/{id}/categories/{categoryId}` | Remove an event category | Organiser | None | 204 / 403 / 404 |
| POST | `/api/events/{id}/enrolments` | Enter the logged-in participant into a category | Participant | categoryId | 201 Created / 400 / 409 |
| GET | `/api/enrolments/me` | View own enrolments | Participant | None | 200 OK |
| GET | `/api/events/{id}/enrolments` | View all enrolments for an event | Organiser | None | 200 OK / 403 / 404 |
| DELETE | `/api/enrolments/{id}` | Cancel own enrolment | Participant | None | 204 / 403 / 404 |
| POST | `/api/enrolments/{id}/result` | Capture a participant result | Organiser | position, finishTime, pace, status | 201 Created / 403 / 409 |
| PUT | `/api/results/{id}` | Correct an existing result | Organiser | result fields | 200 OK / 403 / 404 |
| GET | `/api/results/me` | View personal performance history | Participant | None | 200 OK |
| GET | `/api/events/{id}/results` | View results for an event | Organiser | None | 200 OK / 403 / 404 |
| GET | `/api/events/{id}/route` | Return route reference/information | Public | None | 200 OK / 404 |
| GET | `/api/events/{id}/weather` | Return current/cached weather for event | Public | None | 200 OK / 404 / 503 |
| GET | `/api/health` | API health check | Public | None | 200 OK |
