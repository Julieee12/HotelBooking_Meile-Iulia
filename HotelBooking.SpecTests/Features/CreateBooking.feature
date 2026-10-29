Feature: Create booking
  As a hotel receptionist
  I want to create a booking for a guest with a start and end date
  So that the guest gets a room only if the dates are valid and a room is available for the whole period

  # Days are relative to today: day 0 = today, day 1 = tomorrow.
  # The "test" column refers to the test case numbers in README.md.

  Scenario Outline: A. The dates of the booking are validated
    Given the hotel has 1 room
    When I create a booking from day <start> to day <end>
    Then the outcome should be "<outcome>"

    Examples:
      | test | start | end | outcome | note                       |
      | T1   | 0     | 3   | invalid | start date is today        |
      | T2   | 1     | 3   | created | start date is tomorrow     |
      | T3   | 5     | 4   | invalid | start date is after end    |

  Scenario Outline: B. A booking is checked against an existing booking
    Given the hotel has 1 room
    And room 1 has an active booking from day 10 to day 20
    When I create a booking from day <start> to day <end>
    Then the outcome should be "<outcome>"

    Examples:
      | test | start | end | outcome | note                          |
      | T4   | 5     | 9   | created | ends the day before it starts |
      | T5   | 12    | 15  | refused | inside the existing booking   |
      | T6   | 5     | 25  | refused | encloses the existing booking |

  Scenario: C1. A free second room is used when the first room is booked (T7)
    Given the hotel has 2 rooms
    And room 1 has an active booking from day 10 to day 20
    When I create a booking from day 12 to day 15
    Then the outcome should be "created"
    And the booking is assigned to room 2

  Scenario: C2. A booking is refused when all rooms are occupied (T8)
    Given the hotel has 2 rooms
    And room 1 has an active booking from day 10 to day 20
    And room 2 has an active booking from day 10 to day 20
    When I create a booking from day 12 to day 15
    Then the outcome should be "refused"

  Scenario: C3. An inactive booking does not block the room (T9)
    Given the hotel has 1 room
    And room 1 has an inactive booking from day 10 to day 20
    When I create a booking from day 12 to day 15
    Then the outcome should be "created"
