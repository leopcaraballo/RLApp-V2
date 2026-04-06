# Invariants

- un paciente no puede hacer check-in dos veces en la misma queue
- un consultorio no puede atender dos pacientes a la vez
- no se puede validar pago para un paciente que no es el actual en caja
- no se puede finalizar consulta sin turno activo en consulta
- la pantalla publica no puede construirse desde el write model en linea
- un paciente no puede tener mas de una trayectoria activa en la misma queue
- una trayectoria cerrada no admite hitos nuevos salvo replay idempotente del historial
- el rebuild de trayectoria no puede duplicar hitos ni reemitir side effects operativos
