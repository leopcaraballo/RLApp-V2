Feature: Consultation flow

  Scenario: Doctor completes consultation
    Given a patient was called for consultation
    When the doctor finishes the consultation
    Then the turn becomes completed
