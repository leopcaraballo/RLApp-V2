Feature: Saga correlation and state machines

  Scenario: Consultation saga reuses a single trajectory correlation
    Given a patient trajectory already exists for the consultation flow
    And consultation messages carry the same trajectoryId with operational correlationIds
    When PatientCalled and PatientAttentionCompleted are processed by the saga
    Then the state machine reuses a single saga instance correlated by trajectoryId

  Scenario: Legacy consultation event resolves trajectory correlation without mutating history
    Given a historical consultation event lacks trajectoryId but can be mapped deterministically to an existing trajectory
    When the saga recovery or replay processes the event
    Then the correlation bridge preserves the original historical payload and links the transition to the resolved trajectoryId
