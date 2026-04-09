# AUTO_API_SCREENPLAY Annex

## Purpose

Mapear la cobertura de trayectoria sincronizada implementada en `AUTO_API_SCREENPLAY` contra los artefactos canonicos de `docs/project`.

## Traceability

- Specs: `S-011`, `S-009`
- BDD base: `BDD-010`
- TDD base: `TDD-S-011`, `TDD-S-009`
- Seguridad y resiliencia: `SEC-TEST-001`, `SEC-TEST-003`, `RES-TEST-002`
- Casos reutilizados: `PAT-01`, `VIS-05`, `VIS-06`, `REL-01`

## Automation assets

- feature: `src/test/resources/features/trayectoria_paciente.feature`
- runner: `src/test/java/com/sofka/runners/TrayectoriaRunner.java`
- step definitions: `src/test/java/com/sofka/stepdefinitions/TrayectoriaPacienteStepDefinitions.java`
- tasks principales: `src/main/java/com/sofka/tasks/BuscarTrayectorias.java`, `ConsultarTrayectoria.java`, `ReconstruirTrayectorias.java`

## Implemented coverage

| Canonical anchor | Escenario automatizado | Resultado esperado |
| --- | --- | --- |
| `PAT-01` | `Consultar trayectoria despues del ciclo completo de atencion` | discovery exitoso y estado finalizado |
| `VIS-06` | `Consultar trayectoria por identificador directo` | detalle por `trajectoryId` con hitos y metadata fuente |
| `VIS-05` negativo | `Buscar trayectoria de paciente inexistente retorna vacio` | `total = 0` e `items = []` |
| `REL-01` | `Reconstruir trayectorias en modo simulacion` | `dryRun = true`, `jobId` y contadores presentes |

## Execution and evidence

- comando base: `./gradlew clean test --no-daemon`
- task VS Code reutilizable: `clean-validate-api-screenplay-automation`
- resultados XML: `build/test-results/test`
- reporte Serenity: `target/site/serenity/index.html`
- evidencia minima esperada: conteo de tests, escenarios en verde y rutas de feature/runner/step definitions

## Latest revalidation - 2026-04-08

- comando ejecutado: `env RLAPP_API_BASE_URL=http://localhost:5094 RLAPP_VALID_USERNAME=superadmin RLAPP_VALID_PASSWORD=SuperAdmin@2026Dev! ./gradlew clean test --no-daemon`
- resultado de build: `BUILD SUCCESSFUL in 43s`
- evidencia verificada: `TEST-Flujo de Atencion de Paciente en RLApp-V2.xml` con `1` test, `0` fallos y `0` errores; `TEST-Trayectoria Clinica del Paciente en RLApp-V2.xml` con `4` tests, `0` fallos y `0` errores
- lectura funcional: la suite volvio a validar el flujo nominal base y los cuatro escenarios de trayectoria sin requerir cambios en feature, runner, step definitions ni tasks de dominio

## Current gaps

- falta automatizar negativos explicitos de `401` y `403` para discovery, detalle y rebuild
- falta cobertura de idempotencia explicita para rebuild mas alla del dry-run exitoso
- falta cobertura de convergencia `VIS-08` con superficie sincronizada del frontend
