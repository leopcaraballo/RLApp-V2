Feature: Check-in and queue management

  Scenario: Reception performs check-in without duplication
    Given a valid appointment reference exists for today
    When reception performs check-in twice with the same idempotency key
    Then only one waiting turn exists
