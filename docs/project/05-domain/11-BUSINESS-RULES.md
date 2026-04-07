# Business Rules

- prioridad mas alta se atiende primero dentro del mismo flujo elegible
- entre misma prioridad, gana el menor check-in time
- pago pendiente mantiene el turno en caja hasta validacion o ausencia operativa
- una ausencia en caja termina el turno en `CanceladoPorAusencia` y no acumula nuevos intentos de pago
- una ausencia en consulta termina el turno en `CanceladoPorAusencia` y cancela la trayectoria activa
