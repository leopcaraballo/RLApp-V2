# Orquestador de Trayectorias Clinicas Sincronizadas - QA Documentation Pack

## Purpose

Este paquete consolida la documentacion QA de la feature real `Orquestador de Trayectorias Clinicas Sincronizadas` para RLApp, con foco en sistemas distribuidos, Event Sourcing, CQRS, automatizacion avanzada y cumplimiento normativo en salud.

## Source inputs used

- `QA/requisitos.md` y el marco por fases del workspace QA
- `RLApp-V2/newfeature.md`
- contratos reales implementados en `RLApp-V2/docs/project`
- artefactos reales de testing y automatizacion ya existentes en RLApp y sus repos de automatizacion

## Important interpretation rule

El prompt de entrada usa endpoints conceptuales para describir la capacidad. Esta documentacion los conserva como lenguaje de negocio, pero para diseno QA y automatizacion prioriza el contrato realmente implementado hoy en RLApp.

## Conceptual to implemented mapping

| Conceptual capability | Current RLApp implementation used for QA |
| --- | --- |
| `POST /trayectorias` | apertura implicita de trayectoria a partir de eventos operativos como check-in y flujo de atencion |
| `PATCH /trayectorias/{id}/transicion` | transiciones ejecutadas por endpoints operativos de recepcion, caja y consulta |
| `GET /trayectorias/{id}` | `GET /api/patient-trajectories/{trajectoryId}` |
| `GET /trayectorias/{id}/historial` | detalle de trayectoria desde proyeccion persistida + timeline de auditoria por `correlationId` cuando aplique |

## Deliverables

| # | Document | Purpose |
| --- | --- | --- |
| 01 | `01-PRD-QA-PERSPECTIVE.md` | PRD desde perspectiva QA y riesgo distribuido |
| 02 | `02-USER-STORIES-AND-ACCEPTANCE-CRITERIA.md` | HUs expandidas en Gherkin y flujos por historia |
| 03 | `03-TEST-STRATEGY.md` | estrategia de testing por capas y riesgos |
| 04 | `04-TEST-PLAN.md` | plan de pruebas, ambientes, datos y herramientas |
| 05 | `05-TEST-SCENARIOS.md` | catalogo de escenarios E2E, concurrencia y auditoria |
| 06 | `06-TEST-CASES.md` | casos detallados por reglas RN-01 a RN-30 |
| 07 | `07-TRACEABILITY-MATRIX.md` | matriz HU -> RN -> test -> archivos de test reales |
| 08 | `08-AUTOMATION-DESIGN.md` | stack y estrategia de automatizacion real (xUnit, vitest, Testcontainers) |
| 09 | `09-CI-CD-PIPELINE.md` | pipeline QA real: 7 workflows de GitHub Actions |
| 10 | `10-TEST-REPORT-SIMULATED.md` | informe de ejecucion real (273 tests, 100% pass rate) |
| 11 | `11-BUG-REPORT-EXAMPLES.md` | bugs ejemplo de alta criticidad |
| 12 | `12-GO-NO-GO-REPORT.md` | recomendacion de salida a produccion |
| 13 | `13-EXECUTIVE-DEFENSE-PRESENTATION.md` | deck ejecutivo para sustentacion oral |
| 14 | `14-ORAL-DEFENSE-GUIDE.md` | guion de exposicion, preguntas y respuestas |
| 15 | `15-EXTERNAL-AUTOMATION-DESIGN.md` | arquitectura, patrones, evaluacion y recomendaciones de los 3 proyectos Serenity BDD externos (API Screenplay, POM Factory, UI Screenplay) |

## Compatibility artifacts for the QA phases

- `BUSINESS_CONTEXT.md`
- `USER_STORIES_REFINEMENT.md`
- `TEST_CASES_AI.md`

Estos archivos existen para alinearse con el framework del workspace QA y referencian los documentos principales de este paquete.

## Expected usage order

1. `01-PRD-QA-PERSPECTIVE.md`
2. `02-USER-STORIES-AND-ACCEPTANCE-CRITERIA.md`
3. `03-TEST-STRATEGY.md`
4. `04-TEST-PLAN.md`
5. `05-TEST-SCENARIOS.md`
6. `06-TEST-CASES.md`
7. `07-TRACEABILITY-MATRIX.md`
8. `08-10` y `15` para automatizacion, pipeline y reportes
9. `11-13` para decision y sustentacion
10. `14-15` para presentar y defender ejecutivamente el paquete
