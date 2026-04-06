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

  Scenario: Generated frontend output stays out of version control
    Given frontend build artifacts exist under apps/frontend/.next
    When the repository hygiene rules are enforced
    Then the generated output remains ignored and is not part of the governed change set

  Scenario: Repository cleanup flows through develop before main
    Given a repository cleanup changes only governance and hygiene assets
    When the pull request strategy is prepared
    Then the change is proposed from a feature branch into develop before any develop to main promotion
