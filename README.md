# Test design: "Create booking"

Black-box test case derivation for `BookingManager.CreateBooking` (Mini Project, part 2).
Techniques used: Equivalence Class Testing (ECT), Boundary Value Testing (BVT) and Decision Table-Based Testing.

## 1. Requirements

A booking is described by a start date and an end date. The hotel state consists of its rooms and the existing active bookings.

- The start date must be after today.
- The start date must be on or before the end date.
- A room must be free for the whole period (start date to end date).
- Inactive (cancelled) bookings do not block a room.

| Situation | Expected outcome |
|---|---|
| Invalid dates (start not after today, or start after end) | `ArgumentException` |
| Valid dates, but no room is free | `false` (no booking created) |
| Valid dates and a room is free | `true` (booking created, room assigned, booking active) |

## 2. Equivalence classes

| Input | Valid class | Invalid class |
|---|---|---|
| Start date compared with today | start > today | start <= today |
| Start date compared with end date | start <= end | start > end |
| Room availability | at least one room is free for the period | all rooms are occupied in the period |
| Existing booking state | active booking (blocks its room) | inactive booking (does not block) |

## 3. Boundary values

| Boundary | Values to test |
|---|---|
| Start date vs. today | today (invalid), tomorrow (valid) |
| Start date vs. end date | start = end (valid), start = end + 1 day (invalid) |
| New period vs. an existing booking | ends the day before it starts, starts the day after it ends |

## 4. Decision table

| Condition | R1 | R2 | R3 | R4 |
|---|---|---|---|---|
| Start date > today? | N | Y | Y | Y |
| Start date <= end date? | - | N | Y | Y |
| A room is free in the period? | - | - | N | Y |
| **Outcome** | ArgumentException | ArgumentException | `false` | `true` |

## 5. Test cases (9 in total)

Dates are written as days relative to today (`+1` = tomorrow).
"Booked [10,20]" means an active booking from day +10 to day +20.

### A. Date validity (3 test cases)

| # | Start | End | Hotel state | Expected | Covers |
|---|---|---|---|---|---|
| T1 | 0 (today) | +3 | 1 empty room | ArgumentException | R1, boundary: today |
| T2 | +1 (tomorrow) | +3 | 1 empty room | `true` | R4, boundary: tomorrow |
| T3 | +5 | +4 | 1 empty room | ArgumentException | R2, boundary: start = end + 1 |

### B. Availability (3 test cases, 1 room booked [10,20])

| # | New period | Case | Expected | Covers |
|---|---|---|---|---|
| T4 | [5,9] | ends the day before the booking starts | `true` | R4, boundary |
| T5 | [12,15] | inside the existing booking | `false` | R3, overlap class |
| T6 | [5,25] | encloses the existing booking | `false` | R3, overlap class (neither date is inside the booking) |

### C. Multiple rooms and booking state (3 test cases)

| # | Hotel state | New period | Expected | Covers |
|---|---|---|---|---|
| T7 | 2 rooms, only room 1 booked [10,20] | [12,15] | `true`, assigned to room 2 | R4, second room is used |
| T8 | 2 rooms, both booked [10,20] | [12,15] | `false` | R3, all rooms occupied |
| T9 | 1 room, inactive booking [10,20] | [12,15] | `true` | R4, inactive booking class |

