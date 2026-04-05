# Aggregates

## WaitingQueue

Responsable de:

- admitir pacientes
- proteger invariantes de caja y consulta
- activar y desactivar consultorios
- decidir elegibilidad para llamada y atencion

## StaffUser

Responsable de:

- credenciales operativas
- rol y estado de acceso

## TrayectoriaPaciente

Responsable de:

- abrir una trayectoria unica por paciente dentro de `Waiting Room`
- registrar hitos longitudinales entre recepcion, caja y consulta
- cerrar la trayectoria por finalizacion o cancelacion
- reconstruir la misma trayectoria desde historial sin duplicar hitos ni side effects
