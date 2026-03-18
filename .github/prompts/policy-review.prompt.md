---
name: policy-review
description: "Use when you need a read-only compliance review of prompts, agents, skills, workflows, runtime policy, or AI governance before applying changes."
argument-hint: "<scope or changed assets>"
agent: "policy-reviewer"
---

Ejecuta una revision de compliance y gobernanza antes de editar o ejecutar.

## Inputs requeridos

- Alcance o assets afectados.
- IDs `US-XXX`, `UC-XXX`, `S-XXX`, `BDD-XXX`, `TDD-S-XXX` cuando existan.

## Procedimiento obligatorio

1. Leer `/.ai-entrypoint.md` cuando el cambio afecte la capa AI-first o el execution layer.
2. Validar operating model, runtime policy, testing/observability de IA y politica Git Flow.
3. Verificar trazabilidad y suficiencia documental.
4. Verificar quality gates, riesgos de seguridad y forma de salida esperada.
5. Bloquear si hay violaciones operativas o documentales.

## Output esperado

- Traceability
- Artifacts Reviewed
- Blocking Gaps
- Decision
- Validation
- Residual Risks
