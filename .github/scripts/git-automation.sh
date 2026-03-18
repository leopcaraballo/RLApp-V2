#!/usr/bin/env bash

set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
validator="$repo_root/.github/scripts/validate-conventional-commit.sh"
generator="$repo_root/.github/scripts/generate-conventional-commit.sh"

message=""
scope_hint=""
dry_run="false"

while (( $# > 0 )); do
  case "$1" in
    --message)
      message="${2:-}"
      shift 2
      ;;
    --scope)
      scope_hint="${2:-}"
      shift 2
      ;;
    --dry-run)
      dry_run="true"
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

branch="$(git branch --show-current)"

if [[ -z "$branch" || ! "$branch" =~ ^feature/.+ ]]; then
  echo "Git automation is blocked: active branch must match feature/*" >&2
  exit 1
fi

changes="$(git status --porcelain)"
if [[ -z "$changes" ]]; then
  echo "No changes detected in workspace. Nothing to commit."
  exit 0
fi

if [[ -z "$message" ]]; then
  message="$("$generator" "$scope_hint")"
fi

"$validator" "$message"

echo "Detected branch: $branch"
echo "Commit subject: $message"

if [[ "$dry_run" == "true" ]]; then
  echo "DRY RUN"
  echo "git add ."
  echo "git commit -m \"$message\""
  echo "git push origin $branch"
  exit 0
fi

git add .
git commit -m "$message"
git push origin "$branch"
