# Final Traceability Matrix

| Spec | ADRs | Design anchors | Use cases | User stories | States and events | Tests |
| --- | --- | --- | --- | --- | --- | --- |
| S-001 | ADR-008, ADR-010 | 03-target-architecture/08-SECURITY-ARCHITECTURE.md, 08-security/*, 07-interfaces-and-contracts/11-STAFF-IDENTITY-CONTRACTS.md | UC-001, UC-002 | US-001, US-002 | N/A operativo | BDD-001, TDD-S-001, SEC-TEST-001, SEC-TEST-003 |
| S-002 | ADR-001, ADR-009, ADR-010 | 03-target-architecture/05-MODULE-BOUNDARIES.md, 05-domain/13-ROOM-AND-QUEUE-POLICIES.md | UC-003, UC-004 | US-003, US-004 | EV-008, EV-009 | BDD-002, TDD-S-002 |
| S-003 | ADR-001, ADR-003, ADR-010 | 05-domain/06-INVARIANTS.md, 05-domain/11-BUSINESS-RULES.md, 07-interfaces-and-contracts/02-COMMAND-ENDPOINTS.md | UC-005, UC-006 | US-005, US-006 | ST-001; EV-001, EV-002 | BDD-003, TDD-S-003 |
| S-004 | ADR-001, ADR-003, ADR-010 | 05-domain/09-STATE-TRANSITION-MATRIX.md, 05-domain/11-BUSINESS-RULES.md | UC-007, UC-008, UC-009, UC-010 | US-007, US-008, US-013, US-014, US-015 | ST-002, ST-003, ST-004, ST-005; EV-003..EV-007 | BDD-004, TDD-S-004 |
| S-005 | ADR-001, ADR-003, ADR-009, ADR-010 | 05-domain/09-STATE-TRANSITION-MATRIX.md, 03-target-architecture/05-MODULE-BOUNDARIES.md | UC-011, UC-012, UC-013, UC-014 | US-007, US-008, US-009, US-010 | ST-005..ST-009; EV-010..EV-014 | BDD-005, TDD-S-005 |
| S-006 | ADR-006, ADR-007, ADR-010 | 07-interfaces-and-contracts/10-PUBLIC-DISPLAY-CONTRACT.md, 08-security/04-PUBLIC-DISPLAY-SECURITY.md | UC-006, UC-012, UC-013 | US-006, US-007, US-010 | Consume ST-001, ST-002, ST-005..ST-009; EV-002, EV-003, EV-004, EV-010..EV-014 | BDD-006, TDD-S-006, SEC-TEST-002, RES-TEST-004 |
| S-007 | ADR-003, ADR-006, ADR-010 | 06-application/08-AUDIT-AND-CORRELATION.md, 13-operations/03-METRICS-CATALOG.md, 07-interfaces-and-contracts/12-REPORTING-AND-AUDIT-CONTRACTS.md | UC-015 | US-011, US-012 | Observa ST-001..ST-009 y EV-001..EV-014 | BDD-007, TDD-S-007 |
| S-008 | ADR-003, ADR-004, ADR-005, ADR-006, ADR-010 | 09-data-and-messaging/*, 07-interfaces-and-contracts/09-INTERNAL-EVENT-CONTRACTS.md | UC-016 | US-011, US-016 | Soporta ST-001..ST-009 y EV-001..EV-014 | BDD-008, TDD-S-008, RES-TEST-001, RES-TEST-002, RES-TEST-003 |
| S-009 | ADR-001, ADR-004, ADR-005, ADR-006, ADR-008, ADR-010 | 03-target-architecture/07-OBSERVABILITY-ARCHITECTURE.md, 08-security/*, 13-operations/* | UC-001..UC-017 | Todas las historias implementadas | Transversal | TDD-S-009, SEC-TEST-001..SEC-TEST-004, RES-TEST-001..RES-TEST-004 |
| S-010 | ADR-009, ADR-010 | 16-generation-pack/01-AI-GENERATION-RULES.md, 16-generation-pack/02-TARGET-FOLDER-STRUCTURE.md, 16-generation-pack/08-COPILOT-OPERATING-MODEL.md, 16-generation-pack/09-AI-RUNTIME-POLICY.md, 16-generation-pack/10-AI-TESTING-AND-OBSERVABILITY.md | UC-017 | US-017 | N/A operativo | BDD-009, TDD-S-010 |

## Remaining explicit gaps

- Los thresholds de `S-009` ya quedaron cuantificados, pero todavia no existe pipeline runtime que los mida porque el repositorio sigue sin codigo ejecutable.
- El siguiente hueco material ya no es de contratos sino de generacion ejecutable: OpenAPI/AsyncAPI machine-readable o codigo real que implemente y verifique estos contratos.
