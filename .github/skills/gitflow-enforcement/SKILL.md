---
name: gitflow-enforcement
description: "Validate and reinforce RLApp Git Flow guardrails across instructions, prompts, agents, workflows, and collaborator defaults."
argument-hint: "Describe the branch policy change, workflow hardening, or validation scenario"
user-invocable: true
---

# GitFlow Enforcement

Use this skill when the task affects branch governance, PR enforcement, or protected branch safety.

## Procedure

1. Open the [Git Flow checklist](./references/checklist.md).
2. Confirm `main` and `develop` are treated as protected branches.
3. Confirm work branches use `feature/*`.
4. Confirm prompts, agents, skills, and instructions block edits from protected branches.
5. Confirm workflows reject non-compliant PRs and direct pushes.

## Output

- Branch policy verdict
- Artifacts reviewed
- Blocking gaps
- Validation status
- Residual risks
