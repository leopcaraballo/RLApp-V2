---
name: delivery-orchestration
description: "Execute RLApp work through the approved docs-first sequence. Use for end-to-end delivery, audit remediation, workflow hardening, and prompt or agent alignment."
argument-hint: "Describe the feature, remediation, or execution-layer task to orchestrate"
user-invocable: true
---

# Delivery Orchestration

Use this skill when work spans documentation, execution-layer assets, workflows, or generated code.

## Procedure

1. Run the [phase checklist](./references/phase-checklist.md).
2. Validate Git Flow context before any edit, commit, or integration path.
3. Start with traceability and documentation sufficiency.
4. Run policy review before editing or executing.
5. Update canonical docs before execution-layer assets.
6. Align prompts, instructions, agents, skills, and workflows to the updated docs.
7. Validate the result against CI, PR quality gates, and protected-branch guardrails.

## Output

- Phase-by-phase execution summary
- Artifacts reviewed and policy decision
- Files changed in canonical order
- Validation status
- Residual risks
