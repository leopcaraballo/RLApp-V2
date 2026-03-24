---
name: generate-tests
description: "Use when deriving BDD, TDD, integration, security, or resilience tests from a canonical spec in /docs/project."
argument-hint: "<S-XXX> [changed modules or files]"
agent: "rlapp-delivery"
---

Genera pruebas siguiendo `Spec -> BDD -> TDD -> Validation`.

## Inputs requeridos

- Spec `S-XXX`.
- Modulo afectado o archivos impactados.
- Historia `US-XXX` si la spec cubre varias experiencias.

## Procedimiento obligatorio

1. Leer la spec en `/docs/project/11-specifications`.
2. Leer `/docs/project/15-traceability/05-SPECS-TO-TESTS.md`.
3. Leer el BDD aplicable en `/docs/project/12-testing/bdd`.
4. Leer el TDD aplicable en `/docs/project/12-testing/tdd`.
5. Complementar con `/docs/project/12-testing/01-TEST-STRATEGY.md`, `/docs/project/12-testing/02-QUALITY-GATES.md` y `/docs/project/08-security/08-SECURITY-TEST-REQUIREMENTS.md`.

## Reglas

- Escribir o proponer primero pruebas de dominio y aplicacion, despues integracion, seguridad y resiliencia.
- Cada prueba debe citar `S-XXX` y, cuando aplique, `BDD-XXX` o `TDD-S-XXX`.
- No validar contratos o comportamientos no documentados.

## Output esperado

- Lista o implementacion de pruebas unitarias, integracion, seguridad y resiliencia.
- Cobertura esperada por capa.
- Huecos de trazabilidad o de especificacion que bloqueen la automatizacion.
