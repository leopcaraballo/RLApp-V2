Feature: Consulting room lifecycle

  Scenario: Supervisor activates and deactivates a consulting room
    Given a room is inactive
    When the supervisor activates it
    Then the room becomes available
