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
