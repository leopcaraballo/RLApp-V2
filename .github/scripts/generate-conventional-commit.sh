#!/usr/bin/env bash

set -euo pipefail

scope_hint="${1:-}"

changed_files="$(git status --porcelain | awk '{print $2}')"

if [[ -z "$changed_files" ]]; then
  echo "No changes detected in workspace." >&2
  exit 1
fi

infer_scope() {
  local files="$1"
  if [[ -n "$scope_hint" ]]; then
    printf '%s\n' "$scope_hint" | tr '[:upper:]' '[:lower:]' | tr ' ' '-' | sed 's/[^a-z0-9-]//g'
    return
  fi

  if echo "$files" | grep -Eq '^\.ai-entrypoint\.md$|^ai/'; then
    echo "ai"
  elif echo "$files" | grep -Eq '^docs/project/'; then
    echo "docs"
  elif echo "$files" | grep -Eq '^\.github/workflows/|^\.github/scripts/|^\.githooks/'; then
    echo "git"
  elif echo "$files" | grep -Eq '^\.github/(agents|prompts|skills|instructions|copilot-instructions\.md)'; then
    echo "copilot"
  elif echo "$files" | grep -Eq '^\.vscode/'; then
    echo "devex"
  else
    echo "repo"
  fi
}

infer_type() {
  local files="$1"
  if echo "$files" | grep -Eq '^\.ai-entrypoint\.md$|^ai/'; then
    echo "docs"
  elif echo "$files" | grep -Eq '^docs/'; then
    echo "docs"
  elif echo "$files" | grep -Eq '(^|/)(tests|Tests|__tests__)/|\.feature$|TDD-S-'; then
    echo "test"
  elif echo "$files" | grep -Eq '^\.github/workflows/|^\.github/scripts/|^\.githooks/|^\.vscode/'; then
    echo "chore"
  else
    echo "feat"
  fi
}

infer_description() {
  local files="$1"
  if echo "$files" | grep -Eq '^\.ai-entrypoint\.md$|^ai/'; then
    echo "govern ai operating system"
  elif echo "$files" | grep -Eq '^\.github/scripts/git-automation\.sh|^\.github/scripts/validate-conventional-commit\.sh|^\.github/scripts/generate-conventional-commit\.sh'; then
    echo "automate conventional commits"
  elif echo "$files" | grep -Eq '^\.githooks/'; then
    echo "enforce local git hooks"
  elif echo "$files" | grep -Eq '^\.github/workflows/'; then
    echo "enforce commit standards"
  elif echo "$files" | grep -Eq '^docs/project/'; then
    echo "update ai governance docs"
  elif echo "$files" | grep -Eq '^\.github/(agents|prompts|skills|copilot-instructions\.md|instructions/)'; then
    echo "extend copilot git automation"
  else
    echo "update repository automation"
  fi
}

scope="$(infer_scope "$changed_files")"
type="$(infer_type "$changed_files")"
description="$(infer_description "$changed_files")"
message="${type}(${scope}): ${description}"

if (( ${#message} > 72 )); then
  description="${description:0:$((72 - ${#type} - ${#scope} - 4))}"
  description="$(printf '%s' "$description" | sed 's/[[:space:][:punct:]]\+$//')"
  message="${type}(${scope}): ${description}"
fi

printf '%s\n' "$message"
