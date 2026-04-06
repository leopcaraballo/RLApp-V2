# Deployment Strategy

Despliegue independiente de API y worker, con versionado compatible de contratos y proyecciones.

## Local Docker profile

- `docker compose --profile backend --profile frontend up` levanta `db`, `rabbitmq`, `backend` y `frontend`.
- El perfil Docker local usa RabbitMQ como broker ejecutable por defecto para validar consumers, sagas y endpoints async en la misma topologia objetivo.
- El fallback a mensajeria en proceso deja de ser baseline del perfil Docker local; queda solo como override puntual de pruebas o diagnostico.
- La sonda del contenedor backend debe usar `GET /health/startup` para destrabar el arranque de la topologia; `GET /health/ready` queda reservado para validacion operativa y puede incluir degradaciones transitorias mientras termina el warmup de proyecciones.
- El arranque del backend recompone `QueueState` y `WaitingRoomMonitor` desde el event store cuando detecta proyecciones atrasadas frente al ultimo evento persistido.
