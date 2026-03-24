# Copilot Operating Model

## Purpose

Definir la capa operativa oficial para GitHub Copilot en este repositorio sin crear fuentes paralelas fuera de `/docs/project`.

## Layer model

- Knowledge Layer: `/docs/project` mantiene toda regla canonica, trazabilidad y especificacion aprobada.
- AI Operating Layer: `/.ai-entrypoint.md` y `ai/*` organizan orden de lectura, contexto y navegacion para agentes sin redefinir reglas.
- Execution Layer: `.github/*` contiene las customizations, scripts y workflows que ejecutan la gobernanza aprobada.
- Compatibility Layer: `.github/copilot/*` refleja manifests y mirrors resumidos para consumo complementario.

## Supported customization primitives

- AI entrypoint: `/.ai-entrypoint.md` como guia de arranque y lectura, no como politica canonica
- AI navigation layer: `ai/operating-model/*`, `ai/context/*`, `ai/orchestration/*`, `ai/guardrails/*`
- Repository-wide instructions: `.github/copilot-instructions.md`
- Path-specific instructions: `.github/instructions/*.instructions.md`
- Reusable prompts: `.github/prompts/*.prompt.md`
- Custom agents: `.github/agents/*.agent.md`
- Skills: `.github/skills/<name>/SKILL.md`
- Compatibility manifests: `.github/copilot/agents.yaml`, `.github/copilot/skills.yaml`, `.github/copilot/guardrails.yaml`
- Compatibility mirrors: `.github/copilot/agents/*.md`, `.github/copilot/prompts/*.md`, `.github/copilot/instructions/*.md`

## Required governance docs

- `01-AI-GENERATION-RULES.md`
- `06-DEFINITION-OF-DONE-FOR-AI.md`
- `07-VALIDATION-CHECKLIST-FOR-GENERATED-CODE.md`
- `09-AI-RUNTIME-POLICY.md`
- `10-AI-TESTING-AND-OBSERVABILITY.md`

## Operating rule

- Este repositorio usa `copilot-instructions.md` como unico primitivo repo-wide.
- No agregar `AGENTS.md` mientras exista `.github/copilot-instructions.md`.
- `/.ai-entrypoint.md` es un entrypoint de navegacion; no reemplaza ni compite con `.github/copilot-instructions.md`.
- `ai/*` existe para reducir friccion de consumo y agrupar contexto, no para introducir reglas paralelas.
- Los agentes custom viven en `.github/agents` y no reemplazan la instruccion repo-wide.
- `.github/` solo orquesta lectura, validacion y ejecucion; las reglas canonicas viven en `/docs/project`.
- Si existe `.github/copilot/*`, su contenido debe reflejar la configuracion canonica existente en `.github/agents`, `.github/prompts`, `.github/instructions`, `.github/skills`, `.github/scripts` y `.github/workflows`; no puede divergir ni redefinir reglas.

## Required agent stack

- `rlapp-delivery`: orquestador principal para trabajo end-to-end.
- `traceability-auditor`: subagente de solo lectura para resolver `US-XXX`, `UC-XXX`, `S-XXX`, `BDD-XXX` y `TDD-S-XXX`.
- `docs-gap-detector`: subagente de solo lectura para detectar vacios que bloquean implementacion o pruebas.
- `policy-reviewer`: subagente de solo lectura para validar runtime policy, quality gates, seguridad operativa y forma de salida antes de editar o ejecutar.
- `gitflow-governor`: subagente de solo lectura para validar que el trabajo se prepare desde `feature/*`, sin editar `main` o `develop` directamente.
- `commit-agent`: subagente para preparar commits automáticos con Conventional Commits, agrupacion minima y push seguro desde `feature/*`.

## Required skills

- `traceability-gate`: valida cadena documental antes de implementar.
- `delivery-orchestration`: ejecuta el flujo documental y tecnico aprobado.
- `gitflow-enforcement`: valida y refuerza la politica Git Flow del repositorio.
- `git-automation`: automatiza `git add`, `git commit` y `git push` con Conventional Commits y validaciones de rama.

## Execution sequence

1. Leer `/.ai-entrypoint.md` para determinar la ruta de consumo.
2. Leer bootstrap obligatorio.
3. Validar contexto Git Flow: rama activa `feature/*`, sin trabajo directo sobre `main` o `develop`.
4. Resolver trazabilidad minima `US -> UC -> S -> BDD -> TDD`.
5. Validar dominio, contratos, seguridad y arquitectura afectados.
6. Si falta precision documental, actualizar primero `/docs/project`.
7. Ejecutar policy review antes de cualquier edicion o ejecucion.
8. Solo despues modificar `.github`, `.github/copilot`, `ai/` o codigo ejecutable.
9. Validar workflows, manifests, quality gates, politica Git Flow y trazabilidad remanente.

## Validation contract

- Ningun prompt, agente o skill puede inventar logica fuera de `/docs/project`.
- Ningun archivo dentro de `ai/` puede convertirse en fuente de verdad o duplicar reglas sin referencia explicita a `/docs/project`.
- Ningun cambio de `.github` debe contradecir o duplicar reglas canonicas sin referencia explicita.
- Ningun manifest en `.github/copilot` puede quedar incompleto respecto a los agentes, skills, prompts, instructions o guardrails activos.
- Ninguna interaccion de IA debe editar, commitear ni empujar cambios directamente sobre `main` o `develop`.
- Toda ruta hacia `main` o `develop` debe pasar por Pull Request desde `feature/*`.
- Toda automatizacion de IA debe poder mapearse a artefactos reales de trazabilidad y testing.
- Toda automatizacion de commit debe usar Conventional Commits y bloquear mensajes invalidos antes del commit.
- La politica de sesion por defecto es `Local + Default Approvals`; cualquier excepcion debe ser explicita y acotada.
- Los agentes read-only no deben tener herramientas de edicion o ejecucion.
- Los outputs de orquestacion, QA y auditoria deben ser verificables y exponer decision, validacion y riesgos.
