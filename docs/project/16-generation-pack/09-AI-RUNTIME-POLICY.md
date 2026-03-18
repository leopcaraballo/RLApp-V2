# AI Runtime Policy

## Purpose

Definir como debe operarse GitHub Copilot en sesiones locales, background, cloud o CLI para mantener reproducibilidad, gobernanza y seguridad operativa.

## Default operating mode

- modo por defecto: `Local`
- nivel de permisos por defecto: `Default Approvals`
- uso preferente: explorar, validar trazabilidad, actualizar docs y ejecutar cambios iterativos con revision humana

## Allowed session types

- `Local`: usar para discovery, auditoria, cambios incrementales y validaciones interactivas
- `Background`: usar solo para tareas largas, bien delimitadas y con criterio de aceptacion ya definido
- `Cloud`: usar solo cuando el trabajo sea repetible, con alcance estable y listo para PR automatizado
- `CLI`: usar para automatizacion acotada, no para saltar controles del IDE o del repo

## Disallowed default behaviors

- no usar `Bypass Approvals` como configuracion normal del equipo
- no usar `Autopilot` como modo por defecto
- no aprobar acceso a URLs, comandos o paths de forma global si no existe justificacion documental explicita
- no mezclar multiples asistentes AI como baseline del proyecto

## Git Flow operating policy

- ramas protegidas: `main` y `develop`
- rama de trabajo obligatoria para cambios nuevos: `feature/*`
- prohibido editar, commitear o empujar directamente sobre `main` o `develop`
- todo cambio destinado a `main` o `develop` debe llegar por Pull Request desde `feature/*`
- los prompts, agentes, skills y workflows deben bloquear o revertir pushes directos a ramas protegidas
- cuando la plataforma remota permita branch protection o rulesets, deben configurarse sobre `main` y `develop`; si no existe ese control, el fallback minimo del repo es rechazo por workflow y restauracion automatica del branch protegido

## Commit automation policy

- automatizacion permitida solo desde ramas `feature/*`
- secuencia autorizada: detectar cambios, `git add .`, generar mensaje, validar mensaje, `git commit`, `git push origin <feature-branch>`
- formato obligatorio del subject: `<type>(scope): <description>`
- tipos permitidos: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`
- la descripcion debe permanecer en minusculas, sin punto final y con longitud maxima total de 72 caracteres
- si el mensaje no cumple, el commit debe bloquearse y sugerirse una version corregida
- nunca automatizar push hacia `main` o `develop`

## Model and context policy

- usar el modelo mas capaz disponible para tareas de orquestacion, trazabilidad y auditoria
- comenzar la sesion por `/.ai-entrypoint.md` para resolver ruta de lectura y capa responsable
- usar prompts pequenos, con IDs y alcance explicito, en lugar de instrucciones largas y ambiguas
- adjuntar solo el contexto relevante; el resto debe venir de `docs/project` y de las instructions del repo

## Pre-flight checklist

- confirmar lectura de `/.ai-entrypoint.md`
- confirmar agente activo
- confirmar tipo de sesion
- confirmar nivel de permisos
- confirmar que la rama activa coincide con `feature/*`
- confirmar que `main` y `develop` no se usaran como ramas de trabajo directo
- confirmar si la automatizacion de commit usara mensaje generado o mensaje manual validado
- confirmar que `copilot-instructions.md` y las instructions relevantes estan siendo aplicadas
- confirmar que `ai/*` solo se esta usando como capa de navegacion y no como fuente canonica
- confirmar artefactos de trazabilidad antes de pedir implementacion

## Required output shape

- `Traceability`
- `Artifacts Reviewed`
- `Blocking Gaps`
- `Decision`
- `Validation`
- `Residual Risks`
