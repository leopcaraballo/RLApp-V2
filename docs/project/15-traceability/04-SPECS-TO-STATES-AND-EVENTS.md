# Specs To States And Events

| Spec | States | Events | Notes |
| --- | --- | --- | --- |
| S-001 | N/A | N/A | Seguridad y auditoria de acceso fuera del catalogo operativo de turnos |
| S-002 | N/A | EV-008, EV-009 | Controla disponibilidad de consultorios; su ocupacion efectiva se materializa en S-005 |
| S-003 | ST-001 | EV-001, EV-002 | Apertura de queue y admision sin duplicados |
| S-004 | ST-002, ST-003, ST-004, ST-005 | EV-003, EV-004, EV-005, EV-006, EV-007 | Caja mueve el turno entre atencion, pendiente, cancelacion y espera de consulta |
| S-005 | ST-005, ST-006, ST-007, ST-008, ST-009 | EV-010, EV-011, EV-012, EV-013, EV-014 | Consulta reclama, llama, atiende, finaliza o cancela por ausencia |
| S-006 | ST-001, ST-002, ST-005, ST-006, ST-007, ST-008, ST-009 | EV-002, EV-003, EV-004, EV-010, EV-011, EV-012, EV-013, EV-014 | Consume estados y eventos proyectados para monitor y display |
| S-007 | Observa ST-001 a ST-009 | Observa EV-001 a EV-014 | Reporting y auditoria reconstruyen historial; no crean transiciones nuevas |
| S-008 | Soporta ST-001 a ST-012 | Soporta EV-001 a EV-019 | Garantiza persistencia, outbox, proyecciones y rebuild |
| S-009 | N/A | N/A | Restriccion transversal de plataforma, seguridad, recovery y observabilidad |
| S-011 | ST-010, ST-011, ST-012; observa ST-001 a ST-009 | EV-015, EV-016, EV-017, EV-018, EV-019; observa EV-001 a EV-014 | TrayectoriaPaciente consolida hitos longitudinales y replay controlado |
| S-013 | Observa ST-001 a ST-012 | Observa EV-001 a EV-019 | Sincroniza visibilidad de staff sobre snapshots persistidos y realtime mediado por BFF |
