# Deployment Strategy

Despliegue independiente de API y worker, con versionado compatible de contratos y proyecciones.

## Local Docker profile

- `docker compose --profile backend --profile frontend up` levanta `db`, `backend` y `frontend`.
- En perfil Docker local, si la mensajeria externa no esta disponible o requiere licencia, el procesamiento del outbox puede usar despacho en proceso para sostener read models y validacion funcional.
- La integracion con broker externo sigue siendo la ruta objetivo para entornos no locales.
