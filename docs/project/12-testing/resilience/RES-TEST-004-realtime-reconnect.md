# RES-TEST-004 Realtime Reconnect

## Purpose

Validar que el display publico y el cliente de staff recuperen sincronizacion tras una desconexion transitoria, incluso durante la corrida operativa `REL-06` con volumen mixto y cambios de topologia en consultorios.

## Traceability

- Specs: `S-006`, `S-009`, `S-013`
- BDD base: `BDD-006`, `BDD-012`
- TDD base: `TDD-S-006`, `TDD-S-013`

## Scenario

- iniciar el display publico y el stream same-origin de staff sobre snapshots persistidos
- ejecutar `REL-06` con 100 pacientes, tiempos de espera aleatorios por etapa y reduccion de 10 a 6 consultorios activos a mitad de la prueba
- provocar una desconexion transitoria del canal realtime mientras la corrida sigue procesando pacientes
- verificar que ambos clientes se reconectan y fuerzan refetch de snapshots persistidos sin ampliar permisos ni exponer PII adicional

## Expected evidence

- respuesta `200` y `content-type` valido del stream antes y despues de la reconexion
- evento o secuencia observable de resincronizacion seguida por refetch del snapshot afectado
- snapshots coherentes de monitor, dashboard, trayectoria o display despues de la reconexion
- ausencia de drift bloqueante al terminar la corrida y ausencia de payloads realtime con datos prohibidos
