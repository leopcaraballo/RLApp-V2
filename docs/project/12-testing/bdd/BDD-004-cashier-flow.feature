Feature: Cashier flow

  Scenario: Cashier validates payment and advances the turn
    Given the current cashier turn is active
    When the cashier validates payment
    Then the turn moves to consultation waiting state
