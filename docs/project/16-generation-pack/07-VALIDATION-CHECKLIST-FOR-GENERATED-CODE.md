# Validation Checklist For Generated Code

## Traceability

- resuelve `US-XXX`, `UC-XXX`, `S-XXX`, `BDD-XXX` y `TDD-S-XXX` aplicables
- cita los artefactos revisados y los bloqueos detectados

## Runtime and governance

- usa la politica de runtime de IA aprobada
- no usa bypass approvals o autopilot como camino por defecto
- aplica policy review antes de editar o ejecutar

## Technical correctness

- compila cuando existe proyecto ejecutable
- respeta capas
- respeta estados y eventos
- respeta endpoints y contratos

## Testing and security

- incluye pruebas minimas
- cubre seguridad minima cuando aplica
- no valida comportamiento que no este documentado

## Observability

- deja evidencia de validacion y riesgos remanentes
- mantiene formato de salida verificable para auditoria humana
