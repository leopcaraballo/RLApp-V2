# Use Cases To Specs

| Use case | Primary spec | Secondary spec | User stories |
| --- | --- | --- | --- |
| UC-001 Authenticate Staff | S-001 | S-009 | US-001 |
| UC-002 Manage Internal Roles | S-001 | S-009 | US-002 |
| UC-003 Activate Consulting Room | S-002 | S-009 | US-003 |
| UC-004 Deactivate Consulting Room | S-002 | S-009 | US-004 |
| UC-005 Register Patient Arrival | S-003 | S-009 | US-005 |
| UC-006 View Queue Monitor | S-003 | S-006, S-009 | US-006 |
| UC-007 Call Next At Cashier | S-004 | S-009 | US-007 |
| UC-008 Validate Payment | S-004 | S-009 | US-013 |
| UC-009 Mark Payment Pending | S-004 | S-009 | US-014 |
| UC-010 Mark Absence At Cashier | S-004 | S-009 | US-008 |
| UC-011 Claim Next Patient For Consultation | S-005 | S-009 | US-007, US-009 |
| UC-012 Call Patient To Consultation | S-005 | S-006, S-009 | US-007, US-008 |
| UC-013 Finish Consultation | S-005 | S-006, S-009 | US-010 |
| UC-014 Mark Absence In Consultation | S-005 | S-009 | US-008 |
| UC-015 View Operations Dashboard | S-007 | S-009 | US-011, US-012 |
| UC-016 Rebuild Projections | S-008 | S-009 | US-016 |
| UC-017 Govern AI Operating System | S-010 | S-009 | US-017 |
| UC-018 Reconstruct Patient Trajectory | S-011 | S-008, S-009 | US-012, US-018 |
| UC-019 Protect Aggregate Writes With Optimistic Concurrency | S-008 | S-009 | US-019 |
| UC-020 Correlate Sagas With Patient Trajectory | S-012 | S-007, S-009, S-011 | US-020 |
| UC-021 Consume Synchronized Operational Views | S-013 | S-007, S-009, S-011 | US-021 |

## Notes

- `S-009` es transversal y aplica a todos los casos de uso.
- `S-006` depende de flujos de visibilidad y realtime derivados de monitor y consulta; no introduce un caso de uso independiente en el catalogo actual.
- `S-013` sincroniza vistas operativas de staff y la mediacion de sesion/realtime sobre casos de uso ya existentes de monitor, dashboard y trayectoria.
