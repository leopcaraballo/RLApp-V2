Feature: Cashier flow

  Scenario: Cashier validates payment and advances the turn
    Given the current cashier turn is active
    When the cashier validates payment
    Then the turn moves to consultation waiting state

  Scenario: Cashier marks the current turn as absent
    Given the current cashier turn is active or pending
    When the cashier marks the patient as absent
    Then the turn leaves the cashier flow as absent
