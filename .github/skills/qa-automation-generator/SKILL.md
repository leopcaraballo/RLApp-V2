---
name: qa-automation-generator
description: "Generate end-to-end QA automation projects (Serenity BDD, Screenplay, POM) including Gradle build, feature files, runners, and base test packages."
argument-hint: "Describe the QA scenario, target platform (web/api), and preferred pattern (Screenplay, POM, BDD)."
user-invocable: true
---

# QA Automation Generator

Generate a complete QA automation project scaffold using Serenity BDD and Gradle, based on documented requirements.

## Procedure

1. Confirm the requested QA coverage aligns with the repository's documented QA strategy (`docs/project/12-testing`).
2. Choose an automation style:
   - **Screenplay**: for high maintainability and business-readable tasks.
   - **Page Object Model**: for traditional UI automation.
   - **API tests**: for backend contract validation.
3. Generate the project skeleton under a clear directory, e.g. `qa/serenity` or `qa/automation`.
4. Create Gradle build files and add Serenity dependencies:
   - `net.serenity-bdd:serenity-core`
   - `net.serenity-bdd:serenity-screenplay` (if Screenplay)
   - `net.serenity-bdd:serenity-junit5` or `serenity-junit`.
5. Create baseline source structure:
   - `src/test/java/<package>/` for step definitions and page objects.
   - `src/test/resources/features/` for `.feature` files.
   - `serenity.conf` or `serenity.properties` for configuration.
6. Generate example test artifacts based on provided requirements (BDD scenarios or TDD specs).
7. Ensure the generated artifacts reference the source requirements (US-XXX, S-XXX, BDD-XXX, TDD-S-XXX) in comments or metadata.

## Output

- A scaffolding project with build and test sources.
- Sample feature files and test implementations.
- A README describing how to run the generated QA suite.
- Traceability back to the canonical QA docs and requirements.
