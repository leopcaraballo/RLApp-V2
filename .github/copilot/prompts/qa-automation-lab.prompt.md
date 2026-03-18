---
name: qa-automation-lab
description: "Generate a full QA automation lab (Serenity BDD + Gradle) or individual automation artifacts based on requirements."
argument-hint: "Provide a short description of the QA scenario, target platform, and preferred design pattern (Screenplay, POM, API)."
agent: "qa-automation-agent"
skill: "qa-automation-generator"
---

# QA Automation Lab Prompt

Use this prompt to generate or extend a QA automation project scaffold.

## Inputs

- Requirements summary (e.g., "Login flow validation for staff portal").
- Target platform (web UI, REST API, mobile, etc.).
- Preferred pattern: Screenplay, POM, or API tests.
- Traceability IDs: US-XXX, S-XXX, BDD-XXX (or TDD-S-XXX).

## Desired output

- A generated Gradle project structure for Serenity BDD.
- Example feature file(s) and runner/test class(es).
- Page Objects / Screenplay tasks and UI mapping.
- A short README describing how to run the suite and where to add new scenarios.
- Metadata in generated artifacts linking back to the source requirements.
