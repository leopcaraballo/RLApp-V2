# Invariants

- un paciente no puede hacer check-in dos veces en la misma queue
- un consultorio no puede atender dos pacientes a la vez
- no se puede validar pago para un paciente que no es el actual en caja
- no se puede finalizar consulta sin turno activo en consulta
- la pantalla publica no puede construirse desde el write model en linea
