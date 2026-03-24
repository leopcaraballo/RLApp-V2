Feature: Public display

  Scenario: Public display shows sanitized current turn
    Given a patient is called
    When the display receives an update
    Then it shows only sanitized visible data
