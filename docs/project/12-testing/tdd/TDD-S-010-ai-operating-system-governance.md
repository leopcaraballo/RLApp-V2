# TDD-S-010 AI Operating System Governance

- validar existencia de `/.ai-entrypoint.md` y estructura `ai/*`
- validar manifests `agents.yaml`, `skills.yaml` y `guardrails.yaml`
- validar mirrors de `agents`, `prompts` e `instructions` contra assets canonicos
- validar bloqueo de `main` y `develop`
- validar aceptacion exclusiva de ramas `feature/*`
- validar rechazo de Conventional Commits invalidos
- validar que la revision de subjects en Pull Request use el `merge-base` para no arrastrar commits historicos ajenos al alcance real del PR
- validar que `/.gitignore` excluya outputs generados y caches locales sin ocultar fuentes canonicas del repo
- validar remocion de `apps/frontend/.next/**` del versionado cuando aparezca trackeado por builds locales
- validar que un cleanup repo-wide siga el flujo `feature/* -> develop` antes de cualquier promocion `develop -> main`
