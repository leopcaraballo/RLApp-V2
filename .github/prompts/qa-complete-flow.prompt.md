---
name: qa-complete-flow
description: "Execute complete QA automation flow: analyze user story, generate Gherkin features, scaffold project (Screenplay/POM/API), prepare commit, and prepare PR."
argument-hint: "Provide user story description, target platform, and traceability IDs (US-XXX, S-XXX, etc.)"
agent: "rlapp-delivery"
---

# QA Complete Flow Prompt

Use this prompt to execute a full QA automation workflow from user story to PR preparation.

## Inputs Required

- User story description (e.g., "As a user I want to log in to the application to access my account").
- Target platform (web UI, API, mobile).
- Traceability IDs: US-XXX, S-XXX, BDD-XXX (if exists), TDD-S-XXX (if exists).

## Desired Flow

1. **Analyze Story**: Interpret the user story and identify acceptance criteria.
2. **Generate Scenarios**: Create at least 2 Gherkin scenarios (positive and negative cases).
3. **Choose Pattern**: Decide automation pattern (Screenplay recommended for maintainability).
4. **Scaffold Project**: Generate Gradle + Serenity BDD project structure with:
   - Build files and dependencies.
   - Feature files (.feature).
   - Step definitions / Page Objects / Tasks.
   - Test runners and configuration.
5. **Apply Best Practices**: Ensure code follows QA standards, includes traceability comments.
6. **Prepare Commit**: Generate Conventional Commit message for the changes.
7. **Prepare PR**: Create PR description with traceability and validation notes.

## Output Expected

- Generated project scaffold in `qa/automation/` directory.
- Feature files with scenarios.
- Commit message ready to use.
- PR template filled with traceability and summary.
