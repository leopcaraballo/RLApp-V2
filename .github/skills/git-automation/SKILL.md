---
name: git-automation
description: "Automate git add, Conventional Commit generation, commit validation, and push for RLApp feature branches under existing Git Flow guardrails."
argument-hint: "Describe the commit intent or provide an optional scope hint"
user-invocable: true
---

# Git Automation

Use this skill when the workspace is ready to be committed from a valid feature branch.

## Procedure

1. Open the [git automation checklist](./references/checklist.md).
2. Confirm the active branch matches `feature/*`.
3. Detect workspace changes and stop if there are none.
4. Generate or refine a Conventional Commit subject.
5. Validate the message and block on any violation.
6. Run `git add .`, `git commit`, and `git push origin <feature-branch>` only after all checks pass.

## Output

- Branch validation
- Changed assets reviewed
- Generated or validated commit message
- Automation result
- Residual risks
