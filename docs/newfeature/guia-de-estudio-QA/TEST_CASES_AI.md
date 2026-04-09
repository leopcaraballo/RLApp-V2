# TEST_CASES_AI

Este artefacto existe para alinearse con la Fase 4 del workspace QA.

## Mapping to the main documents

- catalogo de escenarios iniciales y cobertura distribuida: `05-TEST-SCENARIOS.md`
- casos detallados por reglas de negocio: `06-TEST-CASES.md`

## Human adjustment summary

| Typical AI omission | Human QA adjustment |
| --- | --- |
| solo happy path | inclusion de negativos, replay, redelivery y reconnect |
| poco foco en concurrencia | inclusion de `TC-TRJ-21` y `TC-TRJ-22` |
| poca trazabilidad entre regla y automatizacion | adicion de `07-TRACEABILITY-MATRIX.md` |
