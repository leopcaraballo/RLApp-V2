# Business Rules

- prioridad mas alta se atiende primero dentro del mismo flujo elegible
- entre misma prioridad, gana el menor check-in time
- maximo tres intentos de pago
- una ausencia en caja termina el turno en `CanceladoPorAusencia` y no acumula nuevos intentos de pago
- maximo una ausencia en consulta antes de cancelacion por ausencia
