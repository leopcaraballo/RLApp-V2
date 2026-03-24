# Git Flow Checklist

## Protected branches

- `main` is protected.
- `develop` is protected.
- Direct work from protected branches is rejected.

## Working branches

- New work starts from `feature/*`.
- Pull Requests into protected branches come from `feature/*`.
- Non-compliant source branches are rejected.

## Execution layer

- `copilot-instructions.md` blocks work from `main` and `develop`.
- Orchestration instructions validate branch context before edits.
- Git Flow prompts, agents, and skills exist and align with canonical docs.

## Workflows

- Pull Requests validate source and target branch policy.
- Direct pushes to `main` or `develop` are rejected or reverted automatically.
- Residual platform gaps are explicit when host-level branch protection is outside repo control.
