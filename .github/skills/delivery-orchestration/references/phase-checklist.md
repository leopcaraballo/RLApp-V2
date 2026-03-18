# Delivery Phase Checklist

## Phase 1: Bootstrap

- Read bootstrap docs.
- Read the Copilot operating model.
- Confirm the active branch matches `feature/*`.

## Phase 2: Traceability

- Resolve `US-XXX`, `UC-XXX`, `S-XXX`, `BDD-XXX`, `TDD-S-XXX`.
- Identify domain, contracts, security, and architecture dependencies.

## Phase 3: Documentation first

- Update `/docs/project` if the canonical source is insufficient.
- Keep `.github` aligned to the updated docs.

## Phase 4: Execution layer

- Run policy review before editing or executing.
- Update instructions, prompts, agents, and skills only after canonical docs are correct.
- Keep responsibilities separated by concern.

## Phase 5: Validation

- Validate workflow enforcement.
- Validate that `main` and `develop` reject direct pushes or are restored automatically.
- Validate that no legacy `.claude/` or duplicate repo-wide instruction file exists.
- Validate residual risks and next steps.
