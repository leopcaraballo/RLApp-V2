# Copilot-First Execution Layer

`/docs/project` es la unica fuente de verdad. `.github/` existe solo para orquestar como GitHub Copilot lee, valida y ejecuta el trabajo del repositorio.

## Repository facts

- El repositorio actual sigue siendo docs-first: `/docs/project` mantiene la fuente canonica, mientras `apps/backend` y `apps/frontend` contienen implementaciones ejecutables que deben permanecer alineadas con esa documentacion.
- `/.ai-entrypoint.md` existe como punto de arranque para la IA y solo enruta lectura hacia capas canonicas.
- La capa operativa oficial de Copilot esta definida en `/docs/project/16-generation-pack/08-COPILOT-OPERATING-MODEL.md`.
- Este repositorio usa un unico primitivo repo-wide: `.github/copilot-instructions.md`. No agregar `AGENTS.md` mientras esta estrategia siga vigente.

## Bootstrap obligatorio

Antes de proponer cambios, leer siempre:

1. `/.ai-entrypoint.md`
1. `/docs/project/00-README.md`
1. `/docs/project/03-TRACEABILITY-MODEL.md`
1. `/docs/project/04-DOCUMENT-MAP.md`
1. `/docs/project/16-generation-pack/08-COPILOT-OPERATING-MODEL.md`
1. `/docs/project/16-generation-pack/09-AI-RUNTIME-POLICY.md`
1. `/docs/project/16-generation-pack/10-AI-TESTING-AND-OBSERVABILITY.md`
1. `/docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md`

## Flujo por defecto

1. Confirmar que la rama activa cumple `feature/*`; si la rama es `main` o `develop`, detener la ejecucion.
2. Resolver `US-XXX`, `UC-XXX` y `S-XXX`.
3. Validar `BDD-XXX` y `TDD-S-XXX` aplicables.
4. Leer dominio, contratos, arquitectura y seguridad afectados.
5. Si la documentacion no alcanza para ejecutar, actualizar primero `/docs/project`.
6. Ejecutar policy review antes de editar o ejecutar.
7. Solo despues modificar `.github` o codigo ejecutable.
8. Validar quality gates en `.github/workflows`.

## Reglas no negociables

- Nunca inventar logica, contratos, estados, eventos o reglas fuera de `/docs/project`.
- Todo cambio debe mapear como minimo a `Spec + Use Case + User Story`.
- Toda prueba debe mapear a `Spec` y, cuando aplique, a `BDD` o `TDD` existente.
- Respetar arquitectura hexagonal estricta y los ADR aprobados.
- Aplicar `TDD primero, luego implementacion, luego validacion`.
- Si falta trazabilidad o la documentacion es inconsistente, actualizar primero `/docs/project`; no compensar el vacio con suposiciones.
- Operar por defecto en `Local + Default Approvals`; no usar bypass approvals o autopilot como baseline del repo.
- Nunca editar, commitear ni empujar cambios directamente sobre `main` o `develop`.
- Toda integracion hacia `main` o `develop` debe venir desde `feature/*` por Pull Request.
- Todo commit automatizado o manual debe usar Conventional Commits con el formato `<type>(scope): <description>`.
- Si un mensaje de commit es invalido, debe bloquearse antes de ejecutar `git commit`.
- Toda salida de orquestacion, QA o auditoria debe incluir `Traceability`, `Artifacts Reviewed`, `Blocking Gaps`, `Decision`, `Validation` y `Residual Risks`.

## Validation guidance

- Usa `dotnet build`, `dotnet test`, `npm`, `pnpm` o `yarn` solo cuando el slice afectado ya exista como proyecto ejecutable real y la trazabilidad documental este cerrada.
- Usa `ai/` solo para navegar operating model, contexto, orquestacion y guardrails; nunca como fuente primaria.
- Valida cambios del execution layer contra `.github/workflows/ci.yml` y `.github/workflows/pr-quality-gate.yml`.
- Usa `.github/instructions`, `.github/prompts`, `.github/agents` y `.github/skills` como capas complementarias, no como nuevas fuentes de verdad.
- Si existen mirrors de compatibilidad en `.github/copilot/*`, mantenlos alineados con la configuracion canonica y nunca los uses como fuente primaria.
