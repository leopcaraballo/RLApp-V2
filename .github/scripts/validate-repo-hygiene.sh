#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

errors=()

while IFS= read -r file_path; do
  [[ -n "$file_path" ]] || continue
  errors+=("Forbidden backend placeholder file detected: ${file_path}")
done < <(find apps/backend/src -name 'Class1.cs' -print | sort)

for forbidden_file in \
  "apps/backend/src/RLApp.Adapters.Persistence/Data/DbSeeder.cs" \
  "apps/backend/src/RLApp.Application/Handlers/QueryHandlers.cs" \
  "apps/backend/src/RLApp.Domain/Common/Specification.cs"
do
  if [[ -e "$forbidden_file" ]]; then
    errors+=("Forbidden legacy backend file detected: ${forbidden_file}")
  fi
done

backend_symbols=(
  "GetQueueMonitorQuery"
  "GetOperationsDashboardQuery"
  "GetQueueMonitorHandler"
  "GetOperationsDashboardHandler"
  "QueueMonitorDto"
  "PatientInQueueDto"
  "OperationsDashboardDto"
  "NextTurnView"
  "RecentHistoryView"
  "PatientCancelledByPayment"
  "PatientCancelledByAbsence"
  "CancelPatientByAbsence"
)

for symbol in "${backend_symbols[@]}"; do
  if grep -RIn \
    --include='*.cs' \
    --exclude='*Designer.cs' \
    --exclude='AppDbContextModelSnapshot.cs' \
    -- "$symbol" apps/backend/src > /dev/null; then
    errors+=("Forbidden legacy backend symbol detected: ${symbol}")
  fi
done

unsupported_runtime_patterns=(
  "cancel-payment"
  "OperationalVisibleStatuses.Cancelled"
)

for pattern in "${unsupported_runtime_patterns[@]}"; do
  if grep -RIn \
    --include='*.cs' \
    --include='*.ts' \
    --include='*.tsx' \
    -- "$pattern" apps/backend/src apps/frontend/src > /dev/null; then
    errors+=("Unsupported retired runtime pattern detected: ${pattern}")
  fi
done

if grep -RInE \
  --include='*.ts' \
  --include='*.tsx' \
  --exclude='realtime-status.ts' \
  'function[[:space:]]+realtimeTone\(|function[[:space:]]+realtimeLabel\(' \
  apps/frontend/src > /dev/null; then
  errors+=("Local realtimeTone/realtimeLabel helpers are forbidden outside apps/frontend/src/lib/realtime-status.ts.")
fi

if (( ${#errors[@]} > 0 )); then
  printf 'Repository hygiene validation failed:\n' >&2
  for error in "${errors[@]}"; do
    printf -- '- %s\n' "$error" >&2
  done
  exit 1
fi

echo "Repository hygiene validation passed."
