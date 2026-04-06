# Outbox Design

- misma transaccion que la persistencia de eventos
- reintentos seguros
- deduplicacion por message id
- worker con `polling interval` configurable y `batch size` configurable
- señal post-commit para despertar el worker inmediatamente, con polling como fallback
- metricas minimas de backlog, publish duration y propagation delay
- mensajes con tipo desconocido o payload invalido se mueven a dead-letter storage con `correlationId`, causa y payload original para analisis
- en despliegue local o Docker sin broker externo, el outbox puede despachar en proceso para mantener proyecciones operativas
