---
name: traceability-gate
description: "Validate whether an RLApp request has a complete docs-first chain before implementation. Use for feature kickoff, audits, QA, and change readiness checks."
argument-hint: "Describe the requested capability, feature, or change to validate"
user-invocable: true
---

# Traceability Gate

Use this skill before any implementation, remediation, or approval decision.

## Procedure

1. Resolve `US-XXX`, `UC-XXX`, `S-XXX`, `BDD-XXX`, and `TDD-S-XXX`.
2. Open the [traceability checklist](./references/checklist.md).
3. Confirm supporting domain, contracts, security, and architecture files.
4. Stop if any link is missing, implicit, or only nominal.
5. Report the exact files that must be updated first.

## Output

- Exact traceability chain
- Artifacts reviewed
- Pass or fail verdict
- Blocking gaps
- Next canonical documents to update
