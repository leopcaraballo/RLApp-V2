# Git Automation Checklist

## Branch policy

- The active branch matches `feature/*`.
- The active branch is not `main` or `develop`.

## Workspace state

- The workspace contains changes worth committing.
- Empty commits are rejected.

## Commit message policy

- Subject matches `<type>(scope): <description>`.
- Type is one of `feat`, `fix`, `refactor`, `docs`, `test`, `chore`.
- Description is lowercase.
- Subject length is 72 characters or fewer.
- Subject does not end with a period.

## Push policy

- Push target is `origin` and the active `feature/*` branch.
- Push to `main` or `develop` is always rejected.
