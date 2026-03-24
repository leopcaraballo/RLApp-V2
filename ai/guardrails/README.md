# AI Guardrails Layer

Esta capa centraliza la vista operativa de enforcement.

## Guardrails sources

- `docs/project/16-generation-pack/09-AI-RUNTIME-POLICY.md`
- `docs/project/16-generation-pack/10-AI-TESTING-AND-OBSERVABILITY.md`
- `.github/workflows/gitflow-governor.yml`
- `.github/workflows/commit-standards.yml`
- `.github/workflows/ci.yml`
- `.github/workflows/pr-quality-gate.yml`
- `.github/scripts/validate-conventional-commit.sh`
- `.github/scripts/generate-conventional-commit.sh`
- `.github/scripts/git-automation.sh`
- `.githooks/commit-msg`
- `.githooks/pre-push`

## Enforcement focus

- bloqueo de trabajo directo sobre `main` y `develop`
- aceptacion exclusiva de ramas `feature/*`
- Pull Request obligatorio hacia ramas protegidas
- rechazo de Conventional Commits invalidos
- validacion de coherencia entre assets canonicos y mirrors de compatibilidad
