import { writeFileSync } from "node:fs";
import { randomUUID } from "node:crypto";

const baseUrl = process.env.RLAPP_BASE_URL ?? "http://127.0.0.1:5094";
const queueId = process.env.RLAPP_QUEUE_ID ?? "MAIN-QUEUE-001";
const cashierStationId = process.env.RLAPP_CASHIER_ID ?? "CASHIER-01";
const requestedPatients = Number(process.env.RLAPP_TOTAL_PATIENTS ?? 100);
const initialRoomCount = Number(process.env.RLAPP_INITIAL_ROOMS ?? 10);
const remainingRoomCount = Number(process.env.RLAPP_REMAINING_ROOMS ?? 5);
const targetRemainingRoomCount = Math.min(initialRoomCount, remainingRoomCount);
const targetDeactivatedRoomCount = Math.max(
  0,
  initialRoomCount - targetRemainingRoomCount,
);
const deactivateAtPatient = Number(
  process.env.RLAPP_DEACTIVATE_AT_PATIENT ?? Math.ceil(requestedPatients / 2),
);
const roomIdPrefix = process.env.RLAPP_ROOM_ID_PREFIX ?? "ROOM";
const roomNamePrefix = process.env.RLAPP_ROOM_NAME_PREFIX ?? "Consultorio";
const reportPath =
  process.env.RLAPP_REPORT_PATH ??
  "/home/lcaraballo/Documentos/Sofka Projects/projects/RLApp-V2/.tmp/rlapp-patient-simulation-report.json";
const patientPrefix = process.env.RLAPP_PATIENT_PREFIX ?? `SIM-${Date.now()}`;
const caseCountEnvVars = {
  completed: "RLAPP_COMPLETED",
  medicallyReviewed: "RLAPP_MEDICALLY_REVIEWED",
  unpaid: "RLAPP_UNPAID",
  abandoned: "RLAPP_ABANDONED",
  cancelled: "RLAPP_CANCELLED",
};
const caseCategories = Object.keys(caseCountEnvVars);
const caseCounts = resolveCaseCounts(requestedPatients);
const roomCatalog = buildRoomCatalog(initialRoomCount);
const activeRooms = [...roomCatalog];
const deactivatedRooms = [];
let roomDeactivationDone = false;

const terminalTrajectoryStates = new Set([
  "TrayectoriaFinalizada",
  "TrayectoriaCancelada",
]);
const terminalMonitorStatuses = new Set(["Completed", "Absent", "Cancelled"]);
const report = {
  startedAt: new Date().toISOString(),
  baseUrl,
  queueId,
  cashierStationId,
  patientPrefix,
  caseCounts,
  requestedPatients,
  roomCatalog: roomCatalog.map((room) => ({
    roomId: room.roomId,
    roomName: room.roomName,
  })),
  cases: [],
};

function buildBalancedCaseCounts(total) {
  const baseCount = Math.floor(total / caseCategories.length);
  const remainder = total % caseCategories.length;

  return caseCategories.reduce((counts, category, index) => {
    counts[category] = baseCount + (index < remainder ? 1 : 0);
    return counts;
  }, {});
}

function buildCountsFromEnvironment(total) {
  const hasExplicitCounts = Object.values(caseCountEnvVars).some(
    (envName) => process.env[envName] !== undefined,
  );

  if (!hasExplicitCounts) {
    return buildBalancedCaseCounts(total);
  }

  return caseCategories.reduce((counts, category) => {
    counts[category] = Number(process.env[caseCountEnvVars[category]] ?? 0);
    return counts;
  }, {});
}

function resolveCaseCounts(total) {
  const rawCounts = process.env.RLAPP_CASE_COUNTS
    ? JSON.parse(process.env.RLAPP_CASE_COUNTS)
    : buildCountsFromEnvironment(total);

  return caseCategories.reduce((counts, category) => {
    const count = Number(rawCounts?.[category] ?? 0);

    if (!Number.isFinite(count) || count < 0) {
      throw new Error(
        `Invalid count ${rawCounts?.[category]} configured for category ${category}`,
      );
    }

    counts[category] = count;
    return counts;
  }, {});
}

function buildRoomCatalog(totalRooms) {
  const width = Math.max(2, String(totalRooms).length);

  return Array.from({ length: totalRooms }, (_, index) => ({
    roomId: `${roomIdPrefix}-${String(index + 1).padStart(width, "0")}`,
    roomName: `${roomNamePrefix} ${index + 1}`,
  }));
}

function requiresConsultingRoom(category) {
  return (
    category === "completed" ||
    category === "medicallyReviewed" ||
    category === "cancelled"
  );
}

function getActiveRoomForCase(caseItem) {
  if (!requiresConsultingRoom(caseItem.category)) {
    return null;
  }

  if (activeRooms.length === 0) {
    throw new Error(
      `No active consulting rooms available for patient ${caseItem.patientId}`,
    );
  }

  return activeRooms[(caseItem.index - 1) % activeRooms.length];
}

let accessToken = "";

function nowIso() {
  return new Date().toISOString();
}

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function buildCorrelationId(label) {
  const compactLabel = label.replace(/[^a-zA-Z0-9]/g, "").slice(0, 18) || "op";
  return `sim-${compactLabel}-${randomUUID().slice(0, 8)}`;
}

function buildIdempotencyKey(label) {
  const compactLabel =
    label.replace(/[^a-zA-Z0-9]/g, "").slice(0, 24) || "idem";
  return `idem-${compactLabel}-${randomUUID().slice(0, 8)}`;
}

function headersFor(
  label,
  { withIdempotency = false, token = accessToken } = {},
) {
  const headers = {
    Accept: "application/json",
    "X-Correlation-Id": buildCorrelationId(label),
  };

  if (withIdempotency) {
    headers["X-Idempotency-Key"] = buildIdempotencyKey(label);
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  return headers;
}

async function request(
  path,
  { method = "GET", body, headers = {}, expectedStatuses = [200] } = {},
) {
  const requestHeaders = { ...headers };
  if (body !== undefined) {
    requestHeaders["Content-Type"] = "application/json";
  }

  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers: requestHeaders,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  const text = await response.text();
  let data = null;

  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  if (!expectedStatuses.includes(response.status)) {
    throw new Error(
      `${method} ${path} failed with status ${response.status}: ${typeof data === "string" ? data : JSON.stringify(data)}`,
    );
  }

  return data;
}

async function login() {
  const response = await request("/api/staff/auth/login", {
    method: "POST",
    headers: headersFor("login"),
    body: {
      identifier: "superadmin",
      password: "SuperAdmin@2026Dev!",
    },
  });

  const token = response?.accessToken ?? response?.AccessToken;
  if (!token) {
    throw new Error(
      `Login did not return an access token: ${JSON.stringify(response)}`,
    );
  }

  accessToken = token;
  report.login = {
    authenticatedAt: nowIso(),
    role: response?.role ?? response?.Role ?? "unknown",
    capabilities: response?.capabilities ?? response?.Capabilities ?? [],
  };
}

async function activateConsultingRoom(room) {
  const response = await request("/api/medical/consulting-room/activate", {
    method: "POST",
    headers: headersFor(`activate-room-${room.roomId}`),
    body: {
      roomId: room.roomId,
      roomName: room.roomName,
    },
  });

  return {
    ...room,
    activatedAt: nowIso(),
    response,
  };
}

async function deactivateConsultingRoom(room) {
  const response = await request("/api/medical/consulting-room/deactivate", {
    method: "POST",
    headers: headersFor(`deactivate-room-${room.roomId}`),
    body: {
      roomId: room.roomId,
    },
  });

  return {
    ...room,
    deactivatedAt: nowIso(),
    response,
  };
}

async function ensureConsultingRooms() {
  const activatedRooms = [];

  for (const room of roomCatalog) {
    activatedRooms.push(await activateConsultingRoom(room));
  }

  report.roomLifecycle = {
    activatedRooms,
    deactivatedRooms: [],
    deactivateAtPatient,
    remainingRoomTarget: targetRemainingRoomCount,
  };
}

async function deactivateRoomsAtHalfway(processedPatients) {
  if (roomDeactivationDone || processedPatients < deactivateAtPatient) {
    return;
  }

  if (targetDeactivatedRoomCount === 0) {
    roomDeactivationDone = true;
    return;
  }

  const roomsToDeactivate = activeRooms.slice(-targetDeactivatedRoomCount);
  const deactivationResults = [];

  for (const room of roomsToDeactivate) {
    deactivationResults.push(await deactivateConsultingRoom(room));
  }

  activeRooms.splice(
    activeRooms.length - roomsToDeactivate.length,
    roomsToDeactivate.length,
  );
  deactivatedRooms.push(...roomsToDeactivate);
  report.roomLifecycle.deactivatedRooms.push(
    ...deactivationResults.map((room) => ({
      ...room,
      processedPatients,
    })),
  );
  roomDeactivationDone = true;
}

function buildCasePlan() {
  const pending = caseCategories.reduce((counts, category) => {
    counts[category] = Number(caseCounts[category] ?? 0);
    return counts;
  }, {});

  const total = Object.values(pending).reduce((sum, value) => sum + value, 0);
  const width = Math.max(4, String(total).length);
  const plan = [];
  let index = 1;
  const safePatientPrefix =
    patientPrefix.replace(/[^A-Za-z0-9]/g, "").slice(0, 18) || "SIM";

  while (Object.values(pending).some((value) => value > 0)) {
    const availableCategories = caseCategories.filter(
      (category) => pending[category] > 0,
    );
    const category =
      availableCategories[
        Math.floor(Math.random() * availableCategories.length)
      ];
    const patientId = `${safePatientPrefix}PAT${String(index).padStart(width, "0")}`;

    plan.push({
      index,
      category,
      patientId,
      patientName: `Paciente ${index}`,
      appointmentReference: `APPT-${patientPrefix}-${String(index).padStart(width, "0")}`,
    });
    pending[category] -= 1;
    index += 1;
  }

  return plan;
}

function expectPatient(callResult, patientId, endpoint) {
  if (!callResult || callResult.patientId !== patientId) {
    throw new Error(
      `${endpoint} returned unexpected patient. Expected ${patientId}, received ${JSON.stringify(callResult)}`,
    );
  }
}

async function registerPatient(caseItem) {
  return request("/api/reception/register", {
    method: "POST",
    headers: headersFor(`register-${caseItem.patientId}`, {
      withIdempotency: true,
    }),
    body: {
      queueId,
      patientId: caseItem.patientId,
      patientName: caseItem.patientName,
      appointmentReference: caseItem.appointmentReference,
      priority: "1",
      notes: `simulation:${caseItem.category}`,
    },
  });
}

async function callCashier(caseItem) {
  return request("/api/cashier/call-next", {
    method: "POST",
    headers: headersFor(`cashier-call-${caseItem.patientId}`, {
      withIdempotency: true,
    }),
    body: {
      queueId,
      cashierStationId,
    },
  });
}

async function validatePayment(caseItem, turnId) {
  return request("/api/cashier/validate-payment", {
    method: "POST",
    headers: headersFor(`validate-payment-${caseItem.patientId}`),
    body: {
      turnId,
      queueId,
      patientId: caseItem.patientId,
      paymentReference: `PAY-${caseItem.patientId}`,
      validatedAmount: 25,
    },
  });
}

async function markPaymentPending(caseItem, turnId) {
  return request("/api/cashier/mark-payment-pending", {
    method: "POST",
    headers: headersFor(`payment-pending-${caseItem.patientId}`),
    body: {
      turnId,
      queueId,
      patientId: caseItem.patientId,
      reason: "Payment abandoned during simulation",
      attemptNumber: 1,
    },
  });
}

async function markCashierAbsent(caseItem, turnId) {
  return request("/api/cashier/mark-absent", {
    method: "POST",
    headers: headersFor(`cashier-absent-${caseItem.patientId}`),
    body: {
      turnId,
      queueId,
      patientId: caseItem.patientId,
      reason: "Patient did not complete cashier step",
    },
  });
}

async function callMedical(caseItem, room) {
  return request("/api/medical/call-next", {
    method: "POST",
    headers: headersFor(`medical-call-${caseItem.patientId}-${room.roomId}`),
    body: {
      queueId,
      consultingRoomId: room.roomId,
    },
  });
}

async function startConsultation(turnId, caseItem, room) {
  return request("/api/medical/start-consultation", {
    method: "POST",
    headers: headersFor(
      `start-consultation-${caseItem.patientId}-${room.roomId}`,
    ),
    body: {
      turnId,
      consultingRoomId: room.roomId,
    },
  });
}

async function completeConsultation(turnId, caseItem, room, outcome) {
  return request("/api/medical/finish-consultation", {
    method: "POST",
    headers: headersFor(
      `complete-consultation-${caseItem.patientId}-${room.roomId}`,
    ),
    body: {
      turnId,
      queueId,
      patientId: caseItem.patientId,
      consultingRoomId: room.roomId,
      outcome,
    },
  });
}

async function markMedicalAbsent(turnId, caseItem, room) {
  return request("/api/medical/mark-absent", {
    method: "POST",
    headers: headersFor(`medical-absent-${caseItem.patientId}-${room.roomId}`),
    body: {
      turnId,
      queueId,
      patientId: caseItem.patientId,
      consultingRoomId: room.roomId,
      reason: "Patient cancelled before medical review",
    },
  });
}

async function discoverTrajectory(patientId) {
  return request(
    `/api/patient-trajectories?patientId=${encodeURIComponent(patientId)}&queueId=${encodeURIComponent(queueId)}`,
    {
      headers: headersFor(`discover-trajectory-${patientId}`),
    },
  );
}

async function getTrajectory(trajectoryId) {
  return request(
    `/api/patient-trajectories/${encodeURIComponent(trajectoryId)}`,
    {
      headers: headersFor(`get-trajectory-${trajectoryId}`),
    },
  );
}

async function pollTrajectory(caseItem, expectedState) {
  const timeoutAt = Date.now() + 45000;
  let lastPayload = null;

  while (Date.now() < timeoutAt) {
    const discovery = await discoverTrajectory(caseItem.patientId);
    const item = discovery?.items?.[0];
    if (item?.trajectoryId) {
      const detail = await getTrajectory(item.trajectoryId);
      lastPayload = detail;
      if (detail?.currentState === expectedState && detail?.closedAt) {
        return detail;
      }
    }

    await delay(250);
  }

  throw new Error(
    `Timed out waiting for trajectory ${caseItem.patientId} to reach ${expectedState}. Last payload: ${JSON.stringify(lastPayload)}`,
  );
}

async function getDashboard() {
  return request("/api/v1/operations/dashboard", {
    headers: headersFor("operations-dashboard"),
  });
}

async function getMonitor() {
  return request(
    `/api/v1/waiting-room/${encodeURIComponent(queueId)}/monitor`,
    {
      headers: headersFor("waiting-room-monitor"),
    },
  );
}

async function pollOperationalReadModels(totalPatients, completedPatients) {
  const timeoutAt = Date.now() + 60000;
  let last = null;

  while (Date.now() < timeoutAt) {
    const dashboard = await getDashboard();
    const monitor = await getMonitor();
    const nonTerminalEntries = (monitor?.entries ?? []).filter(
      (entry) => !terminalMonitorStatuses.has(entry.status),
    );

    last = {
      dashboard,
      monitor,
      nonTerminalEntries,
    };

    const dashboardReady =
      dashboard?.currentWaitingCount === 0 &&
      dashboard?.totalPatientsToday >= totalPatients &&
      dashboard?.totalCompleted === completedPatients;
    const monitorReady =
      monitor?.waitingCount === 0 && nonTerminalEntries.length === 0;

    if (dashboardReady && monitorReady) {
      return last;
    }

    await delay(500);
  }

  throw new Error(
    `Timed out waiting for read models to settle. Last payload: ${JSON.stringify(last)}`,
  );
}

async function runCase(caseItem) {
  const registerResult = await registerPatient(caseItem);
  const cashierCall = await callCashier(caseItem);
  expectPatient(cashierCall, caseItem.patientId, "/api/cashier/call-next");
  let turnId = cashierCall.turnId ?? registerResult.turnId;

  if (!turnId) {
    throw new Error(`No turnId returned for patient ${caseItem.patientId}`);
  }

  if (caseItem.category === "abandoned") {
    await markCashierAbsent(caseItem, turnId);
    const trajectory = await pollTrajectory(caseItem, "TrayectoriaCancelada");
    return {
      ...caseItem,
      finalTrajectoryState: trajectory.currentState,
      trajectoryId: trajectory.trajectoryId,
      closedAt: trajectory.closedAt,
      outcome: "abandoned-at-cashier",
    };
  }

  if (caseItem.category === "unpaid") {
    await markPaymentPending(caseItem, turnId);
    await markCashierAbsent(caseItem, turnId);
    const trajectory = await pollTrajectory(caseItem, "TrayectoriaCancelada");
    return {
      ...caseItem,
      finalTrajectoryState: trajectory.currentState,
      trajectoryId: trajectory.trajectoryId,
      closedAt: trajectory.closedAt,
      outcome: "unpaid-payment-pending",
    };
  }

  const room = getActiveRoomForCase(caseItem);
  if (!room) {
    throw new Error(
      `Category ${caseItem.category} requires a consulting room but none was assigned`,
    );
  }

  await validatePayment(caseItem, turnId);
  const medicalCall = await callMedical(caseItem, room);
  expectPatient(medicalCall, caseItem.patientId, "/api/medical/call-next");
  turnId = medicalCall.turnId ?? turnId;

  if (caseItem.category === "cancelled") {
    await markMedicalAbsent(turnId, caseItem, room);
    const trajectory = await pollTrajectory(caseItem, "TrayectoriaCancelada");
    return {
      ...caseItem,
      roomId: room.roomId,
      finalTrajectoryState: trajectory.currentState,
      trajectoryId: trajectory.trajectoryId,
      closedAt: trajectory.closedAt,
      outcome: "cancelled-at-consultation",
    };
  }

  await startConsultation(turnId, caseItem, room);
  const consultationOutcome =
    caseItem.category === "medicallyReviewed" ? "follow-up" : "completed";
  await completeConsultation(turnId, caseItem, room, consultationOutcome);
  const trajectory = await pollTrajectory(caseItem, "TrayectoriaFinalizada");

  return {
    ...caseItem,
    roomId: room.roomId,
    finalTrajectoryState: trajectory.currentState,
    trajectoryId: trajectory.trajectoryId,
    closedAt: trajectory.closedAt,
    medicalOutcome: consultationOutcome,
    outcome:
      caseItem.category === "medicallyReviewed"
        ? "reviewed-by-doctor"
        : "completed",
  };
}

function buildSummary(results, operational) {
  const finalStates = results.reduce((accumulator, item) => {
    accumulator[item.finalTrajectoryState] =
      (accumulator[item.finalTrajectoryState] ?? 0) + 1;
    return accumulator;
  }, {});

  const outcomes = results.reduce((accumulator, item) => {
    accumulator[item.outcome] = (accumulator[item.outcome] ?? 0) + 1;
    return accumulator;
  }, {});

  return {
    finishedAt: nowIso(),
    totalPatients: results.length,
    finalStates,
    outcomes,
    dashboard: {
      currentWaitingCount: operational.dashboard.currentWaitingCount,
      totalPatientsToday: operational.dashboard.totalPatientsToday,
      totalCompleted: operational.dashboard.totalCompleted,
      activeRooms: operational.dashboard.activeRooms,
      projectionLagSeconds: operational.dashboard.projectionLagSeconds,
      statusBreakdown: operational.dashboard.statusBreakdown,
    },
    monitor: {
      waitingCount: operational.monitor.waitingCount,
      activeConsultationRooms: operational.monitor.activeConsultationRooms,
      statusBreakdown: operational.monitor.statusBreakdown,
      nonTerminalEntries: operational.nonTerminalEntries.map((entry) => ({
        patientId: entry.patientId,
        status: entry.status,
      })),
      totalEntries: operational.monitor.entries.length,
    },
    rooms: {
      configured: roomCatalog.length,
      deactivated: deactivatedRooms.length,
      remainingActive: activeRooms.length,
      deactivateAtPatient,
      activeRoomIds: activeRooms.map((room) => room.roomId),
      deactivatedRoomIds: deactivatedRooms.map((room) => room.roomId),
    },
  };
}

async function main() {
  const casePlan = buildCasePlan();
  const totalPatients = casePlan.length;
  const completedPatients = casePlan.filter(
    (item) =>
      item.category === "completed" || item.category === "medicallyReviewed",
  ).length;

  if (totalPatients === 0) {
    throw new Error("No patients requested for simulation.");
  }

  if (totalPatients !== requestedPatients) {
    throw new Error(
      `Expected ${requestedPatients} patients but generated ${totalPatients}`,
    );
  }

  await login();
  await ensureConsultingRooms();

  for (const caseItem of casePlan) {
    const result = await runCase(caseItem);
    report.cases.push(result);
    await deactivateRoomsAtHalfway(caseItem.index);

    if (caseItem.index % 10 === 0 || caseItem.index === totalPatients) {
      console.log(`[progress] processed ${caseItem.index}/${totalPatients}`);
    }
  }

  const operational = await pollOperationalReadModels(
    totalPatients,
    completedPatients,
  );
  report.summary = buildSummary(report.cases, operational);

  const allTerminal = report.cases.every((item) =>
    terminalTrajectoryStates.has(item.finalTrajectoryState),
  );
  if (!allTerminal) {
    throw new Error(
      `Found non-terminal trajectory states: ${JSON.stringify(report.cases.filter((item) => !terminalTrajectoryStates.has(item.finalTrajectoryState)))}`,
    );
  }

  if (deactivatedRooms.length !== targetDeactivatedRoomCount) {
    throw new Error(
      `Expected ${targetDeactivatedRoomCount} deactivated rooms but registered ${deactivatedRooms.length}`,
    );
  }

  if (activeRooms.length !== targetRemainingRoomCount) {
    throw new Error(
      `Expected ${targetRemainingRoomCount} active rooms after midpoint but found ${activeRooms.length}`,
    );
  }

  if (report.summary.dashboard.currentWaitingCount !== 0) {
    throw new Error(
      `Dashboard currentWaitingCount expected 0 but received ${report.summary.dashboard.currentWaitingCount}`,
    );
  }

  if (report.summary.dashboard.totalCompleted !== completedPatients) {
    throw new Error(
      `Dashboard totalCompleted expected ${completedPatients} but received ${report.summary.dashboard.totalCompleted}`,
    );
  }

  if (report.summary.dashboard.totalPatientsToday < totalPatients) {
    throw new Error(
      `Dashboard totalPatientsToday expected at least ${totalPatients} but received ${report.summary.dashboard.totalPatientsToday}`,
    );
  }

  if (report.summary.monitor.waitingCount !== 0) {
    throw new Error(
      `Monitor waitingCount expected 0 but received ${report.summary.monitor.waitingCount}`,
    );
  }

  if (report.summary.monitor.nonTerminalEntries.length !== 0) {
    throw new Error(
      `Monitor still contains non-terminal entries: ${JSON.stringify(report.summary.monitor.nonTerminalEntries)}`,
    );
  }

  writeFileSync(reportPath, JSON.stringify(report, null, 2));
  console.log(
    JSON.stringify(
      {
        reportPath,
        summary: report.summary,
        sampleCases: report.cases.slice(0, 5),
      },
      null,
      2,
    ),
  );
}

main().catch((error) => {
  report.failedAt = nowIso();
  report.error = error instanceof Error ? error.message : String(error);
  writeFileSync(reportPath, JSON.stringify(report, null, 2));
  console.error(report.error);
  process.exit(1);
});
