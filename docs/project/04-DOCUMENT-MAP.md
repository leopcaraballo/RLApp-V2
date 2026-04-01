# Document Map

## Purpose

Proveer una vista maestra de la estructura documental y de la dependencia entre secciones.

## Sections

- 01-foundation: reglas fundacionales
- 02-as-is-audit: evidencia objetiva del sistema actual
 	- 02-as-is-audit/10-FASE-0-DIAGNOSTICO.md: Fase 0 — Diagnóstico Ejecutivo (2026-04-01)
	- 02-as-is-audit/13-FASE-1-KICKOFF.md: Fase 1 — inicio de estabilizacion de propagacion y observabilidad
- 03-target-architecture: arquitectura objetivo del proyecto nuevo
- 04-adr: decisiones formales
- 05-domain: modelo de dominio, estados y eventos
- 06-application: casos de uso y puertos
- 07-interfaces-and-contracts: APIs y contratos internos
- 08-security: baseline de seguridad
- 09-data-and-messaging: persistencia, eventos y mensajeria
- 10-product: actores, journeys e historias
- 11-specifications: requisitos funcionales y tecnicos
- 12-testing: estrategia y artefactos de prueba
- 13-operations: operacion y observabilidad
- 14-diagrams: soporte visual
- 15-traceability: matrices formales
- 16-generation-pack: reglas para generacion con IA

## Dependency flow

foundation -> as-is-audit -> target-architecture -> adr -> domain -> application -> contracts -> security/data -> product/specs -> testing -> operations -> traceability -> generation-pack
