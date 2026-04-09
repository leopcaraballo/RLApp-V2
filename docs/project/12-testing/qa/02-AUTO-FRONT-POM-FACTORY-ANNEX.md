# AUTO_FRONT_POM_FACTORY Annex

## Purpose

Documentar la cobertura de trayectoria sincronizada implementada en `AUTO_FRONT_POM_FACTORY` para la consola protegida de staff.

## Traceability

- Specs: `S-011`, `S-013`, `S-009`
- BDD base: `BDD-010`, `BDD-012`
- TDD base: `TDD-S-011`, `TDD-S-013`, `TDD-S-009`
- Seguridad y resiliencia: `SEC-TEST-003`
- Casos reutilizados: `VIS-05` negativo, `S-011` + `S-013` acceso autorizado

## Automation assets

- feature: `src/test/resources/features/trajectory.feature`
- step definitions: `src/test/java/co/com/sofka/stepdefinitions/TrajectoryStepDefinitions.java`
- page object: `src/test/java/co/com/sofka/pages/TrajectoryPage.java`
- step library: `src/test/java/co/com/sofka/steps/TrajectorySteps.java`

## Implemented coverage

| Canonical anchor | Escenario automatizado | Resultado esperado |
| --- | --- | --- |
| `S-011` + `S-013` | `Acceder a la consola de trayectorias como Supervisor` | carga de la seccion de consulta y badges de estado |
| `VIS-05` negativo | `Buscar trayectoria de paciente inexistente muestra vacio` | mensaje visual de sin resultados |

## Execution and evidence

- comando base: `./gradlew clean test --no-daemon`
- task VS Code reutilizable: `clean-validate-pom-automation`
- resultados XML: `build/test-results/test`
- reporte Serenity: `target/site/serenity/index.html`
- evidencia minima esperada: ejecucion exitosa del feature, carga de la consola protegida y asercion del estado vacio

## Latest revalidation - 2026-04-08

- comando ejecutado: `env RLAPP_FRONTEND_BASE_URL=http://localhost:3000 RLAPP_VALID_USERNAME=superadmin RLAPP_VALID_PASSWORD=<seeded-supervisor-password> ./gradlew clean test --no-daemon`
- resultado de build: `BUILD SUCCESSFUL in 33s`
- evidencia verificada: `TEST-co.com.sofka.runners.TestRunner.xml` con `4` tests, `0` fallos y `0` errores
- lectura funcional: el rerun limpio mantuvo en verde los `2` escenarios de login reutilizados por el repo y los `2` escenarios de trayectoria del slice sin exigir cambios en page object, steps o step definitions

## Current gaps

- falta camino positivo de discovery visual con resultados reales
- falta carga de detalle por `trajectoryId` desde la consola
- falta cobertura explicita de `401` y `403` para sesion y rol invalido
- falta evidencia automatizada de no exposicion de `accessToken` en la superficie de trayectoria
