# AI Testing And Observability

## Purpose

Definir como probar y observar el comportamiento de prompts, agentes, skills y workflows del execution layer.

## Minimum test suites

- consumo correcto del `/.ai-entrypoint.md` y del orden de lectura asociado
- happy path con trazabilidad completa
- edge cases con enlaces nominales o faltantes
- inputs ambiguos
- inputs incompletos
- inputs maliciosos orientados a saltar reglas, pruebas o docs
- validaciones de QA con hallazgos por severidad
- drift entre manifests de `.github/copilot` y assets canonicos en `.github`
- intento de trabajo o push directo sobre `main`
- intento de trabajo o push directo sobre `develop`
- Pull Request valido desde `feature/*` hacia `develop` o `main`
- commit automatico valido desde `feature/*` con Conventional Commit bien formado
- intento de commit con mensaje invalido y sugerencia correctiva

## Test contract per scenario

- input exacto
- artefactos que deben revisarse
- output esperado
- criterio de aceptacion
- criterio de rechazo

## Required observability

- usar Agent Logs cuando la ejecucion no siga el flujo esperado
- usar Chat Debug para confirmar contexto, instructions, prompts y tool calls aplicados
- revisar `/.ai-entrypoint.md` y el manifiesto `.github/copilot/guardrails.yaml` cuando cambie la capa AI-first
- revisar referencias efectivas de `copilot-instructions.md` y archivos `.instructions.md`
- registrar riesgos residuales cuando el agente quede bloqueado por vacios documentales
- observar eventos de workflow sobre pushes a `main` o `develop` para confirmar rechazo, restauracion o bloqueo
- observar validacion local y remota de mensajes Conventional Commits

## Acceptance signals

- el agente no inventa IDs, contratos ni reglas
- el agente detiene implementacion cuando faltan `BDD` o `TDD`
- el agente cita artefactos canonicos reales
- el agente produce salida con decision y riesgos
- un push directo a `main` o `develop` queda bloqueado o revertido automaticamente
- un PR desde una rama fuera de `feature/*` es rechazado por workflow
- un commit automatico genera subject valido y lo empuja solo a la rama `feature/*` activa

## Failure signals

- implementa sin trazabilidad suficiente
- responde sin citar artefactos revisados
- mezcla AS-IS con TO-BE sin separacion explicita
- omite pruebas, seguridad o quality gates en cambios que los requieren
- permite trabajo directo sobre `main` o `develop`
- acepta PRs cuyo origen no siga `feature/*`
- permite commits con mensaje fuera del patron Conventional Commits
