Feature: Patient trajectory aggregate

  Scenario: Support rebuilds a unique patient trajectory from historical events
    Given historical events exist for one patient across reception, cashier and consultation
    When support triggers a controlled patient trajectory rebuild
    Then a single chronological trajectory is reconstructed without duplicate active trajectories

  Scenario: Supervisor discovers candidate trajectories from persisted projections
    Given persisted patient trajectory projections already exist for one patient across one or more queues
    When supervisor searches trajectories by patientId and optionally narrows by queueId
    Then the system returns candidate trajectoryIds from the persisted projection without triggering replay in hot path
