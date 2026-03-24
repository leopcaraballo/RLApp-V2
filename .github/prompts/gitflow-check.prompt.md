---
name: gitflow-check
description: "Use when validating whether the current branch, PR path, and execution flow comply with RLApp Git Flow guardrails."
argument-hint: "<current branch> <target branch or PR path>"
agent: "gitflow-governor"
---

Ejecuta una revision Git Flow antes de editar, commitear o integrar cambios.

## Inputs requeridos

- Rama activa.
- Rama objetivo o destino del Pull Request.
- Assets o workflows afectados cuando la politica Git Flow se este cambiando.

## Procedimiento obligatorio

1. Confirmar que la rama activa sigue `feature/*`.
2. Bloquear si la rama activa es `main` o `develop`.
3. Confirmar que la integracion hacia ramas protegidas sera por Pull Request.
4. Revisar workflows, instrucciones, skills y automatizacion de commits cuando el execution layer cambie.

## Output esperado

- Traceability
- Artifacts Reviewed
- Blocking Gaps
- Decision
- Validation
- Residual Risks
