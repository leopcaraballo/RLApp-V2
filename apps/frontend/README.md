# RLApp Frontend

**Estado del proyecto:** Fase 5 — discovery operacional de trayectoria en curso. Ver: ../../docs/project/02-as-is-audit/21-FASE-5-KICKOFF.md

Frontend operativo en Next.js 16 alineado al contrato HTTP real del backend actual.

## Qué incluye

- App Router con páginas por flujo operativo real:
  - `/login`
  - `/`
  - `/reception`
  - `/trajectory`
  - `/waiting-room`
  - `/cashier`
  - `/medical`
  - `/staff`
  - `/health`
- BFF/proxy en `src/app/api/proxy/[...path]/route.ts` para no exponer el JWT directamente al navegador.
- Sesión con cookie firmada y protección de rutas vía `src/proxy.ts`.
- Tipos TypeScript generados desde OpenAPI en `src/generated/backend-api.ts`.
- React Query, formularios con `react-hook-form` + `zod`, y journaling local de operaciones para soporte QA.

## Contrato backend usado

Source of truth actual:

- `../backend/docs/api/openapi.yaml`

Generación de tipos:

```bash
npm run generate:api-types
```

## Docker local

El flujo de referencia para validar la app completa sigue siendo Docker Compose:

```bash
docker compose --profile backend --profile frontend up --build
```

Usuarios seeded para el perfil local:

- `superadmin` (`Supervisor`)
- `support` (`Support`)

Passwords del seed local:

- configurar `RLAPP_SEED_SUPERVISOR_PASSWORD` para `superadmin`
- configurar `RLAPP_SEED_SUPPORT_PASSWORD` para `support`
- si no se configuran, el runtime usa defaults locales pensados para smoke y QA

El frontend sigue usando como contrato auditado `../backend/docs/api/openapi.yaml`, aunque el backend también arranca correctamente en el compose local.

## Variables de entorno

Copiar `.env.example` y ajustar:

```env
BACKEND_API_BASE_URL=http://127.0.0.1:5094
NEXT_PUBLIC_APP_URL=http://localhost:3000
FRONTEND_SESSION_SECRET=replace-this-with-a-long-random-secret
NODE_ENV=development
```

## Comandos

```bash
npm install
npm run generate:api-types
npm run dev
```

Validación:

```bash
npm run lint
npm run typecheck
npm run build
```

## Arquitectura

```bash
src/
  app/
    (workspace)/
    api/
    login/
  components/
  features/
  generated/
  hooks/
  lib/
  services/
  types/
  proxy.ts
```

## Limitaciones reales del backend reflejadas en la UI

- Los GET de negocio siguen siendo limitados; hoy el backend expone discovery y detalle de trayectoria como lecturas operativas auditadas.
- La consola de trayectoria ya soporta discovery por `patientId` y `queueId` opcional, pero el backend no expone búsquedas genéricas equivalentes para otros módulos.
- Varios campos requeridos por DTO siguen siendo ignorados por los handlers backend.
- `queueId` es inconsistente: a veces va en body y a veces en query.
- El login recibe `identifier`, pero en la práctica autentica por `username`.
- El hub SignalR existe, pero no tiene contrato estable suficiente para integrarlo de forma segura en esta iteración.

## Enfoque de frontend adoptado

Como el backend actual es command-driven y no CRUD-driven, la UI se implementó como consola operativa, no como dashboard de listas inventadas. Eso evita acoplar el frontend a contratos inexistentes y hace visibles los warnings de integración donde el backend aún no está completamente endurecido.
