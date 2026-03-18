# Document Governance

## Purpose

Definir como se gobierna la documentacion para mantener coherencia total y evitar divergencias entre producto, arquitectura, dominio, contratos y pruebas.

## Canonical sources

- backend actual: apps/backend/README.md
- arquitectura backend actual: apps/backend/docs/ARCHITECTURE.md
- API backend actual: apps/backend/docs/API.md
- auditoria backend: docs/reports/AUDIT-ARCH-2026-02-28/RF-AUDIT-002.backend-audit.md

## Governance rules

- cada documento debe declarar su proposito y su alcance
- cada documento debe depender de una fuente de verdad superior
- todo cambio de arquitectura requiere ADR previo o actualizado
- todo cambio de dominio requiere impacto en estados, eventos y pruebas
- toda especificacion debe mapear a uno o mas casos de uso aprobados

## Review gates

- gate 1: consistencia de vocabulario
- gate 2: consistencia de contratos y endpoints
- gate 3: consistencia de estados y eventos
- gate 4: consistencia de seguridad y NFR
- gate 5: consistencia de trazabilidad completa

## Approval roles

- arquitectura: Lead Software Architect
- calidad: QA Manager
- producto: Product Owner o equivalente
- seguridad: Security Reviewer cuando aplique
