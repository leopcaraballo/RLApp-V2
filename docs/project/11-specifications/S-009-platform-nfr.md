# S-009 Platform NFR

## Purpose

Definir restricciones transversales de disponibilidad, RPO, RTO, performance, observabilidad, seguridad y recovery aplicables a toda capacidad funcional.

## Traceability

- User stories: aplica a todas las historias implementadas del sistema
- Use cases: aplica a `UC-001` a `UC-022`
- Tests: `TDD-S-009`, `SEC-TEST-001`, `SEC-TEST-002`, `SEC-TEST-003`, `SEC-TEST-004`, `RES-TEST-001`, `RES-TEST-002`, `RES-TEST-003`, `RES-TEST-004`

## Scope

- disponibilidad y continuidad operativa
- performance y latencia razonable para monitor y display
- observabilidad por logs, metricas, tracing y health checks
- backup, restore y recovery
- seguridad transversal y manejo seguro de secretos

## Required behavior

- La plataforma debe exponer health checks para API, base de datos, broker, projection lag y canal realtime.
- Debe existir logging estructurado con `correlationId`, `trajectoryId`, `queueId`, `turnId`, rol y resultado.
- El tracing debe conectar request, trayectoria, evento, publicacion y proyeccion.
- Deben existir metricas para tiempos de espera, throughput, ausencias y lag de proyecciones.
- Backup y restore deben estar definidos y verificables.
- Los secretos deben permanecer fuera del repositorio y gestionarse por entorno.

## Thresholds

- Disponibilidad mensual objetivo de API y worker: `>= 99.5%`.
- RPO maximo: `<= 5 minutos`.
- RTO maximo: `<= 30 minutos`.
- Comandos HTTP protegidos: `p95 <= 1000 ms`, `p99 <= 2000 ms` bajo operacion nominal.
- Queries de monitor, dashboard y timeline: `p95 <= 500 ms`, `p99 <= 1000 ms` bajo operacion nominal.
- Propagacion realtime desde evento persistido hasta display o monitor: `p95 <= 3 segundos`.
- Projection lag operativo aceptable: `<= 30 segundos`; `> 120 segundos` se considera condicion critica.

## Contract dependencies

- Estos thresholds deben reflejarse en health checks, metricas y alertas operativas.

## State and event impact

- Spec transversal; no introduce estados o eventos nuevos.
- Se aplica sobre todas las transiciones y eventos ya catalogados.

## Validation criteria

- Health checks, logs, metricas y trazas deben estar presentes.
- Debe poder validarse recovery y reconnect de canales criticos.
- Ninguna capacidad funcional puede ignorar los requisitos minimos de seguridad y observabilidad.
