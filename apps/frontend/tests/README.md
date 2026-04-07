# Tests

Suite de pruebas que espeja la estructura de src/.

Estructura:

- components/: Pruebas de componentes (unit y integration)
- hooks/: Pruebas de custom hooks
- services/: Pruebas de servicios
- utils/: Pruebas de funciones utilitarias
- e2e/: Pruebas end-to-end (opcional)

Convenciones:

- Archivo de test: `componente.test.tsx` o `componente.spec.ts`
- Framework: Jest + React Testing Library
- Cobertura mínima: 80% según `/docs/project/12-testing/03-COVERAGE-MODEL.md`

Smoke E2E por rol:

- Ejecuta `npm run smoke:roles` desde `apps/frontend`
- Tarea local versionada: `frontend role smoke routine`
- Workflow remoto por push: job `frontend-role-smoke` dentro de `.github/workflows/ci.yml`
- Gate principal de PR: `.github/workflows/pr-quality-gate.yml`, que ejecuta este smoke cuando el PR toca slices runtime relevantes
- Requiere el stack local arriba en `http://127.0.0.1:3000`
- Por defecto cada corrida usa una cola aislada `Q-SMOKE-*` para no heredar turnos pendientes de ejecuciones anteriores
- Variables opcionales: `RLAPP_SMOKE_BASE_URL`, `RLAPP_SMOKE_QUEUE_ID`
