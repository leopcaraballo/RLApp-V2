# Module Boundaries

## Modules

- Identity
- Reception
- Cashier
- Consultation
- Display
- Audit
- Reporting

## Core ownership note

`Waiting Room Core` es el bounded context compartido por Reception, Cashier y Consultation. Dentro de ese core viven `WaitingQueue` y `TrayectoriaPaciente` como agregados canonicos del write-side.

- Reception, Cashier y Consultation disparan casos de uso y reaccionan a eventos del core; no crean una fuente write-side paralela para la trayectoria.
- Audit y Reporting consumen proyecciones longitudinales y store de auditoria; nunca mutan storage interno del core.

## Boundary rule

Cada modulo expone casos de uso y eventos. Ningun modulo accede al storage interno de otro modulo.
