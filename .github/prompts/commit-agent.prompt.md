---
name: commit-agent
description: "Use when you want to automate git add, Conventional Commit generation, commit validation, and push from a valid feature branch."
argument-hint: "[optional conventional commit message or scope hint]"
agent: "commit-agent"
---

Automatiza el commit del workspace sin romper Git Flow ni Conventional Commits.

## Inputs requeridos

- Rama activa.
- Hint opcional de scope o mensaje preferido.
- Confirmacion de que el workspace contiene cambios relevantes.

## Procedimiento obligatorio

1. Confirmar que la rama activa sigue `feature/*`.
2. Detectar cambios reales en el workspace.
3. Generar o mejorar un mensaje Conventional Commit.
4. Bloquear si el mensaje no cumple el formato o si la rama no es valida.
5. Ejecutar `git add .`, `git commit` y `git push origin <feature-branch>` solo si las validaciones pasan.

## Output esperado

- Traceability
- Artifacts Reviewed
- Blocking Gaps
- Decision
- Validation
- Residual Risks
