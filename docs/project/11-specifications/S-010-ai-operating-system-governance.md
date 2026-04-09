# S-010 AI Operating System Governance

## Purpose

Definir la estructura y reglas del repositorio como sistema de desarrollo gobernado por IA, separando capa canonica de conocimiento, capa operativa AI-first y capa de ejecucion de Copilot sin duplicar fuentes de verdad.

## Traceability

- User stories: `US-017`
- Use cases: `UC-017`
- Tests: `BDD-009`, `TDD-S-010`, mas guardrails remotos en workflows

## Scope

- entrypoint de IA para lectura del repositorio
- estructura `ai/` como capa de navegacion y contexto
- `.github/` como execution layer canonica para Copilot
- `.github/copilot/` como compatibility layer derivada
- enforcement de Git Flow y Conventional Commits sobre la capa AI-first
- higiene repo-wide para outputs generados, caches locales y diffs no canonicos del frontend
- analisis repo-wide para placeholders, contratos legacy retirados, shims obsoletos desconectados y helpers locales reemplazados por utilidades compartidas
- deteccion de tipos mapeados sin consumidor confirmado en runtime y tareas locales versionadas para ejecutar la misma validacion fuera del terminal interactivo

## Required behavior

- Debe existir `/.ai-entrypoint.md` como punto de arranque para cualquier agente o sesion asistida.
- El entrypoint debe declarar orden de lectura y remitir a `/docs/project` como fuente canonica.
- Debe existir `ai/operating-model`, `ai/context`, `ai/orchestration` y `ai/guardrails` para navegar el sistema sin duplicar reglas.
- `.github/copilot/` debe incluir `agents.yaml`, `skills.yaml` y `guardrails.yaml` junto con mirrors de `agents`, `prompts` e `instructions`.
- Los mirrors deben referenciar assets canonicos en `.github/{agents,prompts,instructions,skills}` y no redefinir comportamiento.
- Git Flow debe seguir bloqueando trabajo directo sobre `main` y `develop`, y exigir `feature/*` como rama de trabajo.
- La automatizacion de commit debe seguir validando Conventional Commits antes de `git commit`.
- Los artefactos generados por build local, en especial `apps/frontend/.next/`, deben permanecer fuera del versionado mediante la politica repo-wide de `.gitignore`.
- Si se detectan artefactos generados trackeados, la remediacion debe ejecutarse desde una rama `feature/*` alineada con `develop` y revisarse antes de cualquier promocion de `develop` hacia `main`.
- Un cleanup repo-wide no debe mezclar cambios funcionales ajenos al saneamiento de gobierno y versionado.
- Debe existir al menos una validacion automatizada reutilizable en local, CI y PR que bloquee la reintroduccion de scaffolding placeholder, simbolos legacy retirados por auditoria y helpers locales duplicados cuando ya exista una utilidad compartida aprobada.
- La misma validacion repo-wide debe poder ejecutarse desde una tarea local versionada del workspace, ademas del script y de los workflows oficiales, para evitar dependencia de shells contaminados o prompts interactivos externos.
- Una limpieza automatica solo puede actuar sobre hallazgos deterministicos, no funcionales y reversibles, como placeholders vacios, shims obsoletos desconectados y contratos legacy sin wiring activo en runtime.
- Los tipos mapeados en ORM o proyecciones sin consumidor confirmado fuera de su propia declaracion y configuracion deben retirarse del arbol productivo y quedar bloqueados por la validacion de higiene hasta que exista un caso de uso canonico que los reactive.
- Los contratos y simbolos retirados por no existir en el runtime activo, incluyendo `cancel-payment`, `PatientCancelledByPayment`, `PatientCancelledByAbsence` y `CancelPatientByAbsence`, deben permanecer fuera del arbol productivo y disparar falla de higiene si reaparecen.
- Las reglas de higiene y analisis deben dejar evidencia verificable y no depender de interpretacion manual del diff para detectar la reincidencia.

## Contract dependencies

- `/.ai-entrypoint.md` no reemplaza `.github/copilot-instructions.md`.
- `ai/*` no introduce reglas nuevas; solo agrega navegacion, contexto y agrupacion operativa.
- Los workflows de CI y PR deben validar la coherencia entre manifests, mirrors y assets canonicos.
- `/.gitignore` debe materializar la exclusion de outputs generados y caches locales que no son fuente de verdad del producto.

## State and event impact

- Spec operativa; no introduce estados ni eventos de negocio.

## Validation criteria

- Un agente puede identificar correctamente las capas `docs/project`, `ai/` y `.github/`.
- Un intento de trabajo en `main` o `develop` sigue bloqueado por instrucciones y workflows.
- Un commit con mensaje invalido sigue siendo rechazado local o remotamente.
- Los manifests de `.github/copilot/` quedan alineados con el set completo de agentes, skills y guardrails activos.
- Un build local del frontend no reintroduce `apps/frontend/.next/` al versionado del repositorio.
- Un saneamiento repo-wide puede proponerse por `feature/* -> develop` y dejar `develop -> main` como promocion posterior consolidada.
- El gate repo-wide falla si reaparecen archivos placeholder como `Class1.cs`, shims obsoletos retirados del arbol productivo o simbolos backend declarados como legacy fuera del runtime vigente.
- El gate repo-wide falla si reaparecen tipos mapeados retirados por no tener consumidor confirmado en runtime.
- El gate repo-wide falla si reaparece una capacidad terminal de cancelacion de pago o un evento duplicado de cancelacion por ausencia fuera de la trayectoria canonicamente soportada.
- El lint del frontend falla si reaparecen helpers locales ya consolidados en una utilidad compartida para presentar estado realtime.
- La misma regla de higiene puede ejecutarse con el mismo comportamiento desde scripts del repositorio y desde los workflows oficiales.
- La validacion local versionada del workspace debe ejecutar exactamente la misma regla repo-wide de higiene que CI y PR.
