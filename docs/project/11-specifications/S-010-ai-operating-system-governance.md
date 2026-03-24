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

## Required behavior

- Debe existir `/.ai-entrypoint.md` como punto de arranque para cualquier agente o sesion asistida.
- El entrypoint debe declarar orden de lectura y remitir a `/docs/project` como fuente canonica.
- Debe existir `ai/operating-model`, `ai/context`, `ai/orchestration` y `ai/guardrails` para navegar el sistema sin duplicar reglas.
- `.github/copilot/` debe incluir `agents.yaml`, `skills.yaml` y `guardrails.yaml` junto con mirrors de `agents`, `prompts` e `instructions`.
- Los mirrors deben referenciar assets canonicos en `.github/{agents,prompts,instructions,skills}` y no redefinir comportamiento.
- Git Flow debe seguir bloqueando trabajo directo sobre `main` y `develop`, y exigir `feature/*` como rama de trabajo.
- La automatizacion de commit debe seguir validando Conventional Commits antes de `git commit`.

## Contract dependencies

- `/.ai-entrypoint.md` no reemplaza `.github/copilot-instructions.md`.
- `ai/*` no introduce reglas nuevas; solo agrega navegacion, contexto y agrupacion operativa.
- Los workflows de CI y PR deben validar la coherencia entre manifests, mirrors y assets canonicos.

## State and event impact

- Spec operativa; no introduce estados ni eventos de negocio.

## Validation criteria

- Un agente puede identificar correctamente las capas `docs/project`, `ai/` y `.github/`.
- Un intento de trabajo en `main` o `develop` sigue bloqueado por instrucciones y workflows.
- Un commit con mensaje invalido sigue siendo rechazado local o remotamente.
- Los manifests de `.github/copilot/` quedan alineados con el set completo de agentes, skills y guardrails activos.
