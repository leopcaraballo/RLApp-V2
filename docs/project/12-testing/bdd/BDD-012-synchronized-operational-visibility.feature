Feature: Synchronized operational visibility

  Scenario: Receptionist opens a persisted waiting room monitor
    Given the waiting room projections are materialized
    When the receptionist queries the waiting room monitor for a queue
    Then the response comes from persisted read models and lists the current turns with their visible status

  Scenario: Supervisor consumes the synchronized operations dashboard
    Given dashboard and queue projections are materialized
    When the supervisor queries the operations dashboard
    Then the response exposes aggregated metrics and projection lag without reading from replay in hot path

  Scenario: Staff browser reconnects without receiving the backend token
    Given a staff member has a valid web session
    When the realtime stream disconnects and reconnects
    Then the browser reuses the signed session only
    And the affected views refetch their persisted snapshots
