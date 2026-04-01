Feature: AI governed execution layer

  Scenario: Agent starts from the repository entrypoint
    Given the repository exposes .ai-entrypoint.md
    When an agent begins a governed task
    Then the agent reads the entrypoint before editing assets

  Scenario: Protected branches remain blocked
    Given the active branch is main
    When an agent attempts to execute repository changes
    Then the workflow and instructions block the execution path

  Scenario: Feature branches remain valid
    Given the active branch matches feature/restructure-ai-layer
    When an agent follows the documented operating model
    Then the execution flow may continue after policy review

  Scenario: Invalid commit subjects are rejected
    Given repository changes are ready on a feature branch
    When the generated commit subject does not match Conventional Commits
    Then the commit is blocked and a corrected suggestion is returned

  Scenario: Pull request commit validation ignores historical ancestry before divergence
    Given a feature branch contains older commits before the merge-base with the target branch
    When the workflow validates Conventional Commit subjects for the pull request
    Then only the commits after the merge-base are evaluated
