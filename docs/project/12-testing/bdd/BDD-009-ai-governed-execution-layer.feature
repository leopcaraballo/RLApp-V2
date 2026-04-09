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

  Scenario: Execution layer security workflows stay operable on pull requests
    Given the governed repository exposes security workflows in the execution layer
    When a pull request triggers the security scan
    Then CodeQL, dependency audit and secret detection execute without requiring secrets embedded in the repository

  Scenario: Performance baseline remains manual and auditable
    Given the execution layer exposes a manual performance baseline workflow
    When an operator dispatches the workflow with a runtime duration parameter
    Then the benchmark runs and publishes an artifact with the measured results

  Scenario: Generated frontend output stays out of version control
    Given frontend build artifacts exist under apps/frontend/.next
    When the repository hygiene rules are enforced
    Then the generated output remains ignored and is not part of the governed change set

  Scenario: Repository cleanup flows through develop before main
    Given a repository cleanup changes only governance and hygiene assets
    When the pull request strategy is prepared
    Then the change is proposed from a feature branch into develop before any develop to main promotion

  Scenario: Repository hygiene analysis rejects placeholder or legacy backend residue
    Given the governed repository has already retired placeholder scaffolding and legacy backend query contracts
    When the hygiene analysis runs on a feature branch
    Then the validation fails if files like Class1.cs or retired symbols reappear in production source

  Scenario: Frontend lint rejects duplicated realtime presentation helpers
    Given the frontend already exposes a shared helper for realtime connection tone and label
    When a product-facing view declares those helpers locally again
    Then the lint gate fails and instructs the change to use the shared utility

  Scenario: Safe cleanup automation stays limited to deterministic hygiene findings
    Given a repository cleanup run evaluates high-confidence findings
    When the remediation is applied automatically
    Then only non-functional and reversible residues are removed from production source

  Scenario: Repository hygiene rejects mapped types without confirmed consumers
    Given the runtime no longer consumes certain ORM mapped views outside their own declaration and configuration
    When the hygiene validation runs over production backend code
    Then those mapped types must be absent from the active model and blocked if they reappear

  Scenario: Repository hygiene rejects retired cancellation flow residue
    Given the governed repository retired unsupported payment-cancellation and duplicate absence-cancellation symbols from the active runtime
    When the hygiene validation runs over production source
    Then the validation fails if cancel-payment or its retired backend and frontend symbols reappear

  Scenario: Local workspace task runs the same hygiene rule as CI and PR
    Given the repository exposes a versioned local task for hygiene validation
    When an operator runs the task from the workspace
    Then the same repo-wide analysis executes without depending on an interactive terminal session
