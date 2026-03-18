# AI Generation Rules

## Source of truth

- generar solo a partir de `/docs/project`
- usar `/.ai-entrypoint.md` solo como punto de entrada y orden de lectura, nunca como fuente canonica
- no compensar vacios documentales con supuestos del agente
- detener la ejecucion si falta trazabilidad `US -> UC -> S -> BDD/TDD`

## Execution policy

- comenzar por `/.ai-entrypoint.md`, luego continuar con el bootstrap obligatorio y solo despues consumir prompts, agentes, skills o workflows
- usar sesiones locales por defecto para trabajo exploratorio o iterativo
- usar background o cloud solo para tareas bien acotadas, repetibles y con salida esperada definida
- usar aprobaciones por defecto; no habilitar bypass approvals o autopilot como modo normal de trabajo
- cualquier tarea con edicion o ejecucion debe pasar primero por trazabilidad, suficiencia documental y policy review

## Design constraints

- generar solo dentro de la arquitectura aprobada
- no saltar capas
- no introducir patrones fuera del catalogo
- no crear contratos, eventos, estados o comandos fuera de especificaciones canonicas

## Output contract

- toda respuesta de trabajo end-to-end debe incluir como minimo: `Traceability`, `Artifacts Reviewed`, `Blocking Gaps`, `Decision`, `Validation`, `Residual Risks`
- toda auditoria o QA debe priorizar hallazgos por severidad antes de resumenes cosmeticos
- toda recomendacion debe indicar el documento, prompt, skill, agente o workflow que debe cambiarse
