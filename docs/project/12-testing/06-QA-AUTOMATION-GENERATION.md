# QA Automation Generation

Este documento describe la estrategia para generar automatización de pruebas (QA) en el repositorio siguiendo las prácticas de Serenity BDD, Screenplay y Page Object Model (POM).

## Objetivo

Proveer una capacidad de generación automática de proyectos de prueba que:

- Alineen con la estrategia de pruebas y la trazabilidad del repositorio.
- Faciliten la creación de suites de pruebas end-to-end reproducibles.
- Permitan extender rápidamente casos de prueba a partir de requisitos (US, S, BDD, TDD).

## Patrones soportados

1. **Screenplay**: recomendado para casos de prueba de alta mantenibilidad y legibilidad empresarial.
2. **Page Object Model (POM)**: recomendado para pruebas de UI tradicionales y equipos que prefieren separación clara entre páginas y tests.
3. **API tests**: para validar contratos y flujos backend sin UI.

## Artefactos generados

- Estructura de proyecto Gradle con configuración de Serenity.
- `serenity.conf` / `serenity.properties` para configurar ejecución y reportes.
- Clases base de test y runners.
- Page Objects / Tasks / Step Definitions.
- Feature files (`.feature`) mapeados a escenarios BDD.
- Documentación de ejecución y cómo añadir nuevos escenarios.

## Integración con el repositorio

- El generador se expone a través de un agente Copilot (`qa-automation-agent`) y una skill (`qa-automation-generator`).
- Los prompts para generar labs de automatización se encuentran en `.github/prompts/qa-automation-lab.prompt.md`.
- Todos los artefactos generados deben referenciar los IDs de trazabilidad (`US-XXX`, `S-XXX`, `BDD-XXX`, `TDD-S-XXX`).

## Cómo ejecutar

1. Usar la skill `qa-automation-generator` o el prompt `qa-automation-lab`.
2. Confirmar que se trabaja en una rama `feature/*`.
3. Revisar y ajustar los artefactos generados para que cumplan con los requisitos y la arquitectura.

## Validación

- El código generado debe ser revisado siguiendo las reglas del repositorio (CI, calidad, trazabilidad).
- Los artefactos deben pasar las revisiones de QA y ser consistentes con los criterios de aceptación documentados.
