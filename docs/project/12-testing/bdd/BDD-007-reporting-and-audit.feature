Feature: Reporting and audit

  Scenario: Supervisor reconstructs a turn timeline
    Given a correlationId is known
    When the supervisor queries audit
    Then the timeline is reconstructed consistently
