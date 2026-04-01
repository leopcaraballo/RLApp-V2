# RLApp Frontend

**Estado del proyecto:** Fase 0 — Diagnóstico Ejecutivo completado (2026-04-01). Ver: ../../docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md

Frontend operativo en Next.js 16 alineado al contrato HTTP real del backend actual.

## Qué incluye

- App Router con páginas por flujo operativo real:
  - `/login`
  - `/`
  - `/reception`
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

## Hallazgo importante

Intentar levantar Swagger/OpenAPI directamente desde el backend en runtime hoy falla porque la API no arranca por un requisito de licencia de MassTransit. Por eso este frontend usa como contrato el `openapi.yaml` auditado del backend, no el documento publicado por runtime.

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

- No existen endpoints GET de negocio para listar recursos o ver detalle persistido.
- Varios campos requeridos por DTO siguen siendo ignorados por los handlers backend.
- `queueId` es inconsistente: a veces va en body y a veces en query.
- El login recibe `identifier`, pero en la práctica autentica por `username`.
- El hub SignalR existe, pero no tiene contrato estable suficiente para integrarlo de forma segura en esta iteración.

## Enfoque de frontend adoptado

Como el backend actual es command-driven y no CRUD-driven, la UI se implementó como consola operativa, no como dashboard de listas inventadas. Eso evita acoplar el frontend a contratos inexistentes y hace visibles los warnings de integración donde el backend aún no está completamente endurecido.
