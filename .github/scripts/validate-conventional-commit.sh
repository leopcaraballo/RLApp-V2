#!/usr/bin/env bash

set -euo pipefail

message="${1:-}"

if [[ -z "$message" ]]; then
  echo "Missing commit message subject."
  exit 1
fi

if [[ "$message" =~ ^Merge[[:space:]] ]]; then
  printf 'Valid Conventional Commit subject (Merge commit bypassed): %s\n' "$message"
  exit 0
fi

valid_types='feat|fix|refactor|docs|test|chore|ci'
pattern="^(${valid_types})\(([a-z0-9][a-z0-9-]*)\): ([[:lower:]][[:lower:][:digit:] /,_-]*)$"
extract_pattern="^(${valid_types})\(([^)]*)\):[[:space:]]*(.*)$"
strict_extract_pattern="^(${valid_types})\(([a-z0-9][a-z0-9-]*)\):[[:space:]]*(.*)$"

normalize_message() {
  local raw="$1"
  local trimmed
  trimmed="$(printf '%s' "$raw" | sed 's/[[:space:]]\+$//')"
  trimmed="${trimmed%.}"
  printf '%s' "$trimmed" | tr '[:upper:]' '[:lower:]'
}

suggest_correction() {
  local raw="$1"
  local normalized type scope description
  normalized="$(normalize_message "$raw")"

  if [[ "$normalized" =~ $extract_pattern ]]; then
    type="${BASH_REMATCH[1]}"
    scope="${BASH_REMATCH[2]}"
    description="${BASH_REMATCH[3]}"
  else
    type="chore"
    scope="repo"
    description="$normalized"
  fi

  description="$(printf '%s' "$description" | sed 's/^[[:space:]]\+//; s/[[:space:]]\+$//')"
  description="${description%.}"
  description="$(printf '%s' "$description" | tr '[:upper:]' '[:lower:]')"
  scope="$(printf '%s' "$scope" | tr '[:upper:]' '[:lower:]' | tr ' ' '-' | sed 's/[^a-z0-9-]//g')"

  if [[ -z "$scope" ]]; then
    scope="repo"
  fi

  if [[ -z "$description" ]]; then
    description="update repository automation"
  fi

  local candidate="${type}(${scope}): ${description}"
  if (( ${#candidate} > 72 )); then
    local overflow=$(( ${#candidate} - 72 ))
    local target_length=$(( ${#description} - overflow ))
    if (( target_length < 10 )); then
      target_length=10
    fi
    description="${description:0:${target_length}}"
    description="$(printf '%s' "$description" | sed 's/[[:space:][:punct:]]\+$//')"
    candidate="${type}(${scope}): ${description}"
  fi

  printf '%s\n' "$candidate"
}

errors=()

if ! [[ "$message" =~ $pattern ]]; then
  errors+=("Message must match <type>(scope): <description> with allowed types: feat, fix, refactor, docs, test, chore, ci.")
fi

if (( ${#message} > 72 )); then
  errors+=("Message must be 72 characters or fewer.")
fi

description=""
if [[ "$message" =~ $strict_extract_pattern ]]; then
  description="${BASH_REMATCH[3]}"
fi

if [[ -n "$description" && "$description" =~ [A-Z] ]]; then
  errors+=("Description must be lowercase.")
fi

if [[ "$message" == *. ]]; then
  errors+=("Message must not end with a period.")
fi

if (( ${#errors[@]} > 0 )); then
  printf 'Invalid Conventional Commit subject:\n' >&2
  for error in "${errors[@]}"; do
    printf -- '- %s\n' "$error" >&2
  done
  printf 'Suggested message: %s\n' "$(suggest_correction "$message")" >&2
  exit 1
fi

printf 'Valid Conventional Commit subject: %s\n' "$message"
