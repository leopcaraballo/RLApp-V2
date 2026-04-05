Feature: Patient trajectory aggregate

  Scenario: Support rebuilds a unique patient trajectory from historical events
    Given historical events exist for one patient across reception, cashier and consultation
    When support triggers a controlled patient trajectory rebuild
    Then a single chronological trajectory is reconstructed without duplicate active trajectories
