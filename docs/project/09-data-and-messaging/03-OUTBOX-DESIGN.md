# Outbox Design

- misma transaccion que la persistencia de eventos
- reintentos seguros
- deduplicacion por message id
- worker con `polling interval` configurable y `batch size` configurable
- señal post-commit para despertar el worker inmediatamente, con polling como fallback
- metricas minimas de backlog, publish duration y propagation delay
- en despliegue local o Docker sin broker externo, el outbox puede despachar en proceso para mantener proyecciones operativas
