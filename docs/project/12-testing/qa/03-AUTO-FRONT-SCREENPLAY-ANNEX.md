# AUTO_FRONT_SCREENPLAY Annex

## Purpose

Documentar la cobertura de trayectoria sincronizada implementada en `AUTO_FRONT_SCREENPLAY` para la consola protegida de staff usando el patron Screenplay.

## Traceability

- Specs: `S-011`, `S-013`, `S-009`
- BDD base: `BDD-010`, `BDD-012`
- TDD base: `TDD-S-011`, `TDD-S-013`, `TDD-S-009`
- Seguridad y resiliencia: `SEC-TEST-003`
- Casos reutilizados: `VIS-05` negativo, `S-011` + `S-013` acceso autorizado

## Automation assets

- feature: `src/test/resources/features/trajectory.feature`
- runner: `src/test/java/co/com/sofka/runners/TrajectoryTest.java`
- step definitions: `src/test/java/co/com/sofka/stepdefinitions/TrajectoryStepDefinitions.java`
- tasks y questions: `src/main/java/co/com/sofka/tasks/OpenTrajectoryPage.java`, `SearchTrajectory.java`, `src/main/java/co/com/sofka/questions/TrajectoryConsoleVisible.java`, `TrajectorySearchResult.java`
- user interface targets: `src/main/java/co/com/sofka/userinterface/TrajectoryPage.java`

## Implemented coverage

| Canonical anchor | Escenario automatizado | Resultado esperado |
| --- | --- | --- |
| `S-011` + `S-013` | `Acceder a la consola de trayectorias como Supervisor` | seccion de consulta visible y badges presentes |
| `VIS-05` negativo | `Buscar trayectoria de paciente inexistente muestra vacio` | no se encuentran trayectorias para el paciente consultado |

## Execution and evidence

- comando base: `./gradlew clean test --no-daemon`
- task VS Code reutilizable: `clean-validate-ui-screenplay-automation`
- resultados XML: `build/test-results/test`
- reporte Serenity: `target/site/serenity/index.html`
- evidencia minima esperada: runner en verde, escenarios de disponibilidad y vacio validados, artefactos de Screenplay presentes

## Latest revalidation - 2026-04-08

- comando ejecutado: `env RLAPP_FRONTEND_BASE_URL=http://localhost:3000 RLAPP_VALID_USERNAME=superadmin RLAPP_VALID_PASSWORD=SuperAdmin@2026Dev! ./gradlew clean test --no-daemon`
- resultado de build: `BUILD SUCCESSFUL in 40s`
- evidencia verificada: `TEST-co.com.sofka.runners.RegistrationTest.xml` con `2` tests, `0` fallos y `0` errores; `TEST-co.com.sofka.runners.TrajectoryTest.xml` con `2` tests, `0` fallos y `0` errores
- lectura funcional: el repo se mantuvo estable en la rerun limpia y no requirio cambios en tasks, questions, targets ni step definitions para sostener la cobertura actual del slice
- observacion tecnica: la corrida reporto advertencias de CDP para Chrome `146`, pero no bloquearon Selenium ni generaron fallos funcionales en esta ejecucion

## Current gaps

- falta cobertura del discovery positivo con resultados reales
- falta carga del detalle longitudinal y de su cronologia desde la UI
- falta cobertura negativa explicita de `401` y `403` para la consola
- falta convergencia `VIS-08` con realtime e invalidacion de snapshots persistidos
