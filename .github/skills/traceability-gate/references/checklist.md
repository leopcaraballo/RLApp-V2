# Traceability Gate Checklist

## Minimum chain

- `US-XXX` exists and matches the requested capability.
- `UC-XXX` is explicit in product or application docs.
- `S-XXX` is explicit and sufficient.
- `BDD-XXX` exists for the behavior under review.
- `TDD-S-XXX` exists for the affected spec.

## Support files

- Domain invariants, rules, states, and events are resolvable.
- Contracts exist for commands, queries, realtime, and errors when applicable.
- Security requirements exist when auth, RBAC, display, or sensitive data are involved.
- Architecture and module boundaries are clear enough for implementation.

## Fail conditions

- Missing IDs
- Placeholder mappings
- Specs without actionable detail
- Tests without canonical linkage
- Contracts or transitions not documented
