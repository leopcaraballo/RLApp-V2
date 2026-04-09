# Read Model Schemas

- `v_waiting_room_monitor`: snapshot por turno visible para `GET /api/v1/waiting-room/{queueId}/monitor`
- `public-display snapshot`: derivado de `v_waiting_room_monitor` para `GET /api/v1/waiting-room/{queueId}/public-display` y sus relays web anonimos
- `v_queue_state`: conteos y tiempos agregados por queue para monitor y dashboard
- `v_next_turn`: capacidad secundaria para discovery del siguiente turno elegible
- `v_recent_history`: historial visible sanitizado cuando aplique
- `v_operations_dashboard`: agregados globales para `GET /api/v1/operations/dashboard`
- `v_patient_trajectory`: vista longitudinal persistida para queries de trayectoria

Los canales realtime no reemplazan estos esquemas. Solo invalidan y fuerzan refetch de snapshots persistidos.
