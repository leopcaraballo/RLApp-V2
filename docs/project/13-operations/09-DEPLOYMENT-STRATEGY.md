# Deployment Strategy

Despliegue independiente de API y worker, con versionado compatible de contratos y proyecciones.

## Local Docker profile

- `docker compose --profile backend --profile frontend up` levanta `db`, `rabbitmq`, `backend` y `frontend`.
- El perfil Docker local usa RabbitMQ como broker ejecutable por defecto para validar consumers, sagas y endpoints async en la misma topologia objetivo.
- El fallback a mensajeria en proceso deja de ser baseline del perfil Docker local; queda solo como override puntual de pruebas o diagnostico.
