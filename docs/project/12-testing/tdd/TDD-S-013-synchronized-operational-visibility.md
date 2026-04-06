# TDD-S-013 Synchronized Operational Visibility

- query handler de monitor lee `v_queue_state` y `v_waiting_room_monitor` sin tocar write-side ni replay
- query handler de dashboard agrega `v_operations_dashboard`, `v_queue_state` y `v_waiting_room_monitor` desde persistencia
- controladores protegidos devuelven `401` sin autenticacion y `403` con rol no autorizado
- endpoints de sesion del frontend no exponen `accessToken` al navegador
- stream realtime same-origin reconecta y obliga refetch del monitor, dashboard o trayectoria afectados
- payload realtime contiene solo metadata de invalidacion y no PII adicional
