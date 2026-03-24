# AI Orchestration Layer

Esta carpeta resume donde viven los assets ejecutables de Copilot.

## Canonical assets

- `.github/instructions/`
- `.github/prompts/`
- `.github/agents/`
- `.github/skills/`
- `.github/scripts/`
- `.github/workflows/`

## Compatibility assets

- `.github/copilot/agents.yaml`
- `.github/copilot/skills.yaml`
- `.github/copilot/guardrails.yaml`
- `.github/copilot/agents/`
- `.github/copilot/prompts/`
- `.github/copilot/instructions/`

## Rule

- Cambiar primero los assets canonicos en `.github/`.
- Regenerar o alinear despues los manifests y mirrors de `.github/copilot/`.
