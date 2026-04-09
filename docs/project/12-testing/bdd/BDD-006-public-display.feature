Feature: Public display

  Scenario: Public display shows simultaneous sanitized destinations
    Given multiple patients are active across cashier and consultation destinations
    When the display receives an update
    Then it shows only sanitized visible data for each visible destination
    And it keeps simultaneous active destinations synchronized with the persisted monitor snapshot
