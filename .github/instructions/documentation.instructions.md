---
name: "Documentation And Execution Layer"
description: "Use when editing docs/project, traceability matrices, generation-pack files, or GitHub Copilot customization files in .github. Covers source-of-truth protection, AI execution layer alignment, and documentation-first delivery."
applyTo: "docs/project/**/*.md,.github/**/*.md"
---

# Documentation And Execution Layer Instructions

**Estado del proyecto:** Fase 0 — Diagnóstico Ejecutivo completado (2026-04-01). Ver: /docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md

Lee primero:

1. `/.ai-entrypoint.md`
1. `/docs/project/00-README.md`
1. `/docs/project/03-TRACEABILITY-MODEL.md`
1. `/docs/project/04-DOCUMENT-MAP.md`
1. `/docs/project/16-generation-pack/08-COPILOT-OPERATING-MODEL.md`
1. `/docs/project/16-generation-pack/09-AI-RUNTIME-POLICY.md`
1. `/docs/project/16-generation-pack/10-AI-TESTING-AND-OBSERVABILITY.md`

## Reglas

- `/docs/project` sigue siendo la unica fuente de verdad.
- `/.ai-entrypoint.md` y `ai/*` son capas de navegacion, no fuentes canonicas.
- `.github` puede orquestar comportamiento de Copilot, pero no redefinir reglas canonicas sin referencia explicita.
- `.github` no debe introducir una estructura paralela como `.github/copilot/*` como nueva fuente de verdad.
- `ai/*` no debe duplicar politicas; debe apuntar a `/docs/project` y `.github`.
- Si existen archivos en `.github/copilot/*`, deben ser mirrors de compatibilidad sincronizados desde la configuracion canonica basada en `.github/copilot-instructions.md`, `.github/instructions`, `.github/prompts`, `.github/agents` y `.github/skills`.
- Si actualizas instrucciones, prompts, agentes o skills, valida que el cambio siga el operating model oficial.
- Si cambias trazabilidad, specs, testing o arquitectura, actualiza primero los documentos canonicos y luego el execution layer.
- Mantener consistencia entre `US-XXX`, `UC-XXX`, `S-XXX`, `BDD-XXX` y `TDD-S-XXX`.
- Mantener consistencia con la politica de runtime, testing y observabilidad de IA.
- Los agentes read-only no deben recibir herramientas de edicion o ejecucion.
- Mantener consistencia con la politica Git Flow: `feature/*` como rama de trabajo, `main` y `develop` protegidas.

## Rechazar si

- una instruccion de `.github` contradice `/docs/project`
- una matriz de trazabilidad queda nominal o incompleta
- un prompt, agente o skill introduce comportamiento no soportado por artefactos reales
