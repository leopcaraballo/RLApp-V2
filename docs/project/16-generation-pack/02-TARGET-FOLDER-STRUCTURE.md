# Target Folder Structure

Define la estructura objetivo del repositorio como sistema de desarrollo gobernado por IA.

## Layering

- `docs/project/`: Knowledge Layer canonica. Contiene ADRs, dominio, contratos, specs, testing y trazabilidad.
- `ai/`: AI Operating Model Layer. Expone entrypoints, rutas de lectura y vistas de navegacion para agentes sin duplicar reglas.
- `.github/`: Execution Layer canonica para Copilot. Contiene instructions, prompts, agents, skills, scripts y workflows.
- `.github/copilot/`: Compatibility Layer derivada. Expone manifests y mirrors resumidos para consumo complementario, nunca como fuente primaria.
- `.vscode/`: Workspace defaults para experiencia local y validaciones editor-centric.

## Target repository tree

```text
.
|-- .ai-entrypoint.md
|-- ai/
|   |-- context/
|   |   `-- README.md
|   |-- guardrails/
|   |   `-- README.md
|   |-- operating-model/
|   |   `-- README.md
|   `-- orchestration/
|       `-- README.md
|-- docs/
|   `-- project/
|-- .github/
|   |-- copilot-instructions.md
|   |-- instructions/
|   |-- prompts/
|   |-- agents/
|   |-- skills/
|   |-- scripts/
|   |-- workflows/
|   `-- copilot/
|       |-- agents.yaml
|       |-- skills.yaml
|       |-- guardrails.yaml
|       |-- agents/
|       |-- prompts/
|       `-- instructions/
`-- .vscode/
```

## Structural rules

- `ai/` no puede redefinir politicas, contratos ni reglas canonicas.
- `.ai-entrypoint.md` debe enrutar lectura hacia `/docs/project`, `ai/` y `.github` segun la tarea.
- `.github/copilot/*` debe reflejar el contenido canonico de `.github/{instructions,prompts,agents,skills}` y de los workflows de guardrails.
- Ningun workflow o script debe depender de una ruta AI-first no documentada en esta estructura objetivo.
