Feature: Authentication and access control

  Scenario: Staff user authenticates successfully
    Given a valid staff account exists
    When the user authenticates with valid credentials
    Then access is granted according to the assigned role
