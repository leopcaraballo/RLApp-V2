# QA Annex Index For Synchronized Trajectory

## Purpose

Agrupar los anexos de implementacion del slice de trayectoria sincronizada sin convertir los repositorios de automatizacion en fuente primaria de verdad.

## Canonical navigation

- plan compartido: `12-testing/07-SYNCHRONIZED-TRAJECTORY-QA-TEST-PLAN.md`
- catalogo compartido de casos: `12-testing/08-SYNCHRONIZED-TRAJECTORY-QA-TEST-CASES.md`

## Implementation annexes

- `01-AUTO-API-SCREENPLAY-ANNEX.md`
- `02-AUTO-FRONT-POM-FACTORY-ANNEX.md`
- `03-AUTO-FRONT-SCREENPLAY-ANNEX.md`

## Latest revalidation

- fecha: `2026-04-08`
- entorno: `docker compose` local con `backend` saludable en `http://localhost:5094` y `frontend` saludable en `http://localhost:3000`
- resultado consolidado: `AUTO_API_SCREENPLAY` en verde con `5` tests (`1` de flujo base + `4` de trayectoria), `AUTO_FRONT_POM_FACTORY` en verde con `4` tests (`2` de login + `2` de trayectoria) y `AUTO_FRONT_SCREENPLAY` en verde con `4` tests (`2` de registro + `2` de trayectoria)
- decision: no fue necesario ajustar la logica de automatizacion para cerrar esta rerun desde cero; la actualizacion documental se limita a registrar evidencia fresca y a mantener visibles los gaps ya planificados

## Usage rule

- estos anexos documentan como se materializa la cobertura por implementacion
- ninguna regla funcional, de seguridad o de trazabilidad nace aqui; toda regla canonica sigue en `docs/project`
- cualquier caso nuevo debe agregarse primero al catalogo compartido y luego reflejarse en el anexo correspondiente
