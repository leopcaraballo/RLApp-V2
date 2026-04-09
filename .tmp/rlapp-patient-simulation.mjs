import { writeFileSync } from "node:fs";
import { randomUUID } from "node:crypto";

const baseUrl = process.env.RLAPP_BASE_URL ?? "http://127.0.0.1:5094";
const queueId = process.env.RLAPP_QUEUE_ID ?? "CONSULTA-EXTERNA-PRINCIPAL";
const cashierStationId = process.env.RLAPP_CASHIER_ID ?? "CAJA-PRINCIPAL";
const requestedPatients = Number(process.env.RLAPP_TOTAL_PATIENTS ?? 100);
const initialRoomCount = Number(process.env.RLAPP_INITIAL_ROOMS ?? 10);
const remainingRoomCount = Number(process.env.RLAPP_REMAINING_ROOMS ?? 6);
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
const patientIdStart = readNonNegativeIntegerEnv(
  "RLAPP_PATIENT_ID_START",
  52300000,
);
const appointmentPrefix = process.env.RLAPP_APPOINTMENT_PREFIX ?? "CITA-CE";
const progressInterval = Math.max(
  10,
  readNonNegativeIntegerEnv("RLAPP_PROGRESS_INTERVAL", 50),
);
const registrationBatchSize = Math.max(
  1,
  readNonNegativeIntegerEnv("RLAPP_REGISTRATION_BATCH_SIZE", 50),
);
const timingProfile = {
  interPatientWait: resolveDelayRange("INTER_PATIENT_WAIT", 0, 0),
  cashierDwell: resolveDelayRange("CASHIER_DWELL", 0, 0),
  medicalCallDwell: resolveDelayRange("MEDICAL_CALL_DWELL", 0, 0),
  consultationDwell: resolveDelayRange("CONSULTATION_DWELL", 0, 0),
};
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
const assignableRoomIds = new Set(roomCatalog.map((room) => room.roomId));
const availableRooms = [...roomCatalog];
const waitingRoomResolvers = [];
let roomDeactivationDone = false;
let roomDeactivationRequested = false;
let processedPatientCount = 0;
let resultCommitChain = Promise.resolve();
let operationalMutationChain = Promise.resolve();
const simulationDayCode = new Date()
  .toISOString()
  .slice(0, 10)
  .replace(/-/g, "");

const primaryNames = [
  "Maria",
  "Juan",
  "Ana",
  "Carlos",
  "Luisa",
  "Andres",
  "Paula",
  "Miguel",
  "Valentina",
  "Santiago",
  "Camila",
  "Daniel",
  "Laura",
  "Sebastian",
  "Juliana",
  "Mateo",
  "Carolina",
  "Felipe",
  "Isabella",
  "Nicolas",
];
const secondaryNames = [
  "Fernanda",
  "Alejandro",
  "Patricia",
  "Jose",
  "Catalina",
  "Esteban",
  "Andrea",
  "Javier",
  "Tatiana",
  "Ricardo",
  "Gabriela",
  "David",
  "Lorena",
  "Alejandra",
  "Rafael",
  "Natalia",
  "Cristian",
  "Adriana",
  "Oscar",
  "Claudia",
];
const surnames = [
  "Gomez",
  "Rodriguez",
  "Martinez",
  "Lopez",
  "Gonzalez",
  "Perez",
  "Ramirez",
  "Torres",
  "Sanchez",
  "Vargas",
  "Moreno",
  "Castro",
  "Romero",
  "Diaz",
  "Herrera",
  "Rojas",
  "Jimenez",
  "Restrepo",
  "Navarro",
  "Ortega",
  "Mendoza",
  "Suarez",
  "Cortes",
  "Pineda",
];

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
  timingProfile,
  registrationBatchSize,
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

function buildSyntheticPatientProfile(index, width) {
  const primaryName = primaryNames[(index - 1) % primaryNames.length];
  const secondaryName = secondaryNames[(index * 3 - 1) % secondaryNames.length];
  const firstSurname = surnames[(index * 5 - 1) % surnames.length];
  const secondSurname = surnames[(index * 7 - 1) % surnames.length];
  const includeSecondName = index % 3 !== 0;
  const patientId = String(patientIdStart + index - 1);
  const patientName = [
    primaryName,
    includeSecondName ? secondaryName : null,
    firstSurname,
    secondSurname,
  ]
    .filter(Boolean)
    .join(" ");

  return {
    patientId,
    patientName,
    appointmentReference: `${appointmentPrefix}-${simulationDayCode}-${String(index).padStart(width, "0")}`,
  };
}

function describeCaseCategory(category) {
  switch (category) {
    case "completed":
      return "flujo-completo";
    case "medicallyReviewed":
      return "seguimiento-medico";
    case "unpaid":
      return "pago-pendiente";
    case "abandoned":
      return "ausencia-en-caja";
    case "cancelled":
      return "ausencia-en-consulta";
    default:
      return category;
  }
}

function readNonNegativeIntegerEnv(name, defaultValue) {
  const value = Number(process.env[name] ?? defaultValue);

  if (!Number.isInteger(value) || value < 0) {
    throw new Error(
      `Invalid non-negative integer configured for ${name}: ${process.env[name]}`,
    );
  }

  return value;
}

function resolveDelayRange(prefix, defaultMin, defaultMax) {
  const minMs = readNonNegativeIntegerEnv(`RLAPP_MIN_${prefix}_MS`, defaultMin);
  const maxMs = readNonNegativeIntegerEnv(`RLAPP_MAX_${prefix}_MS`, defaultMax);

  if (maxMs < minMs) {
    throw new Error(
      `Invalid delay range for ${prefix}: max ${maxMs} cannot be lower than min ${minMs}`,
    );
  }

  return { minMs, maxMs };
}

function requiresConsultingRoom(category) {
  return (
    category === "completed" ||
    category === "medicallyReviewed" ||
    category === "cancelled"
  );
}

function removeAvailableRoom(roomId) {
  const roomIndex = availableRooms.findIndex((room) => room.roomId === roomId);

  if (roomIndex < 0) {
    return null;
  }

  return availableRooms.splice(roomIndex, 1)[0];
}

function roomIsDeactivated(roomId) {
  return deactivatedRooms.some((room) => room.roomId === roomId);
}

async function deactivateRoomAndRecord(room, processedPatients) {
  if (roomIsDeactivated(room.roomId)) {
    return;
  }

  const deactivationResult = await deactivateConsultingRoom(room);
  const activeRoomIndex = activeRooms.findIndex(
    (candidate) => candidate.roomId === room.roomId,
  );

  if (activeRoomIndex >= 0) {
    activeRooms.splice(activeRoomIndex, 1);
  }

  deactivatedRooms.push(room);
  report.roomLifecycle.deactivatedRooms.push({
    ...deactivationResult,
    processedPatients,
  });
  roomDeactivationDone = deactivatedRooms.length === targetDeactivatedRoomCount;
}

function acquireConsultingRoom() {
  const availableRoomIndex = availableRooms.findIndex((room) =>
    assignableRoomIds.has(room.roomId),
  );

  if (availableRoomIndex >= 0) {
    return Promise.resolve(availableRooms.splice(availableRoomIndex, 1)[0]);
  }

  return new Promise((resolve) => {
    waitingRoomResolvers.push(resolve);
  });
}

async function releaseConsultingRoom(room) {
  if (!assignableRoomIds.has(room.roomId)) {
    await deactivateRoomAndRecord(room, processedPatientCount);
    return;
  }

  const nextResolver = waitingRoomResolvers.shift();
  if (nextResolver) {
    nextResolver(room);
    return;
  }

  availableRooms.push(room);
}

let accessToken = "";

function nowIso() {
  return new Date().toISOString();
}

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function pickRandomDelay(range) {
  if (!range || range.maxMs <= range.minMs) {
    return range?.minMs ?? 0;
  }

  return (
    range.minMs + Math.floor(Math.random() * (range.maxMs - range.minMs + 1))
  );
}

async function waitForRandomDelay(range) {
  const waitMs = pickRandomDelay(range);

  if (waitMs > 0) {
    await delay(waitMs);
  }

  return waitMs;
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

function isConcurrencyConflictError(error) {
  const message = error instanceof Error ? error.message : String(error);
  const normalizedMessage = message.toLowerCase();

  return (
    normalizedMessage.includes("concurrency_conflict") ||
    normalizedMessage.includes("concurrent modification detected")
  );
}

async function runWithConcurrencyRetry(operation, maxAttempts = 5) {
  let attempt = 0;

  while (attempt < maxAttempts) {
    try {
      return await operation();
    } catch (error) {
      attempt += 1;

      if (!isConcurrencyConflictError(error) || attempt >= maxAttempts) {
        throw error;
      }

      await delay(25 * attempt);
    }
  }

  throw new Error("Operation exceeded concurrency retry budget.");
}

function runSerializedOperationalMutation(operation) {
  const pendingMutation = operationalMutationChain.then(
    () => runWithConcurrencyRetry(operation),
    () => runWithConcurrencyRetry(operation),
  );

  operationalMutationChain = pendingMutation.catch(() => {});
  return pendingMutation;
}

function readErrorText(payload) {
  if (!payload || typeof payload !== "object") {
    return typeof payload === "string" ? payload : "";
  }

  return (
    payload.error ?? payload.message ?? payload.detail ?? payload.title ?? ""
  );
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
    expectedStatuses: [200, 400],
    body: {
      roomId: room.roomId,
      roomName: room.roomName,
    },
  });

  if (response?.success === true) {
    return {
      ...room,
      activatedAt: nowIso(),
      response,
    };
  }

  const errorText = String(readErrorText(response));
  if (
    errorText.length > 0 &&
    !errorText.toLowerCase().includes("already active")
  ) {
    throw new Error(
      `Unable to activate consulting room ${room.roomId}: ${JSON.stringify(response)}`,
    );
  }

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
    expectedStatuses: [200, 400],
    body: {
      roomId: room.roomId,
    },
  });

  if (response?.success === true) {
    return {
      ...room,
      deactivatedAt: nowIso(),
      response,
    };
  }

  const errorText = String(readErrorText(response));
  if (
    errorText.length > 0 &&
    !errorText.toLowerCase().includes("already inactive")
  ) {
    throw new Error(
      `Unable to deactivate consulting room ${room.roomId}: ${JSON.stringify(response)}`,
    );
  }

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
  if (
    roomDeactivationDone ||
    roomDeactivationRequested ||
    processedPatients < deactivateAtPatient
  ) {
    return;
  }

  roomDeactivationRequested = true;

  if (targetDeactivatedRoomCount === 0) {
    roomDeactivationDone = true;
    return;
  }

  const roomsToDeactivate = activeRooms.slice(-targetDeactivatedRoomCount);

  for (const room of roomsToDeactivate) {
    assignableRoomIds.delete(room.roomId);
  }

  for (const room of roomsToDeactivate) {
    const availableRoom = removeAvailableRoom(room.roomId);

    if (availableRoom) {
      await deactivateRoomAndRecord(availableRoom, processedPatients);
    }
  }

  roomDeactivationDone = deactivatedRooms.length === targetDeactivatedRoomCount;
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
    const patientProfile = buildSyntheticPatientProfile(index, width);

    plan.push({
      index,
      category,
      patientId: patientProfile.patientId,
      patientName: patientProfile.patientName,
      appointmentReference: patientProfile.appointmentReference,
      caseLabel: describeCaseCategory(category),
      patientReference: `${safePatientPrefix}${String(index).padStart(width, "0")}`,
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
  return runSerializedOperationalMutation(() =>
    request("/api/reception/register", {
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
        notes: `simulacion:${caseItem.caseLabel};referencia:${caseItem.patientReference}`,
      },
    }),
  );
}

async function registerCasePlan(casePlan) {
  const registeredCases = [];

  for (const caseItem of casePlan) {
    const arrivalGapMs =
      registeredCases.length === 0
        ? 0
        : await waitForRandomDelay(timingProfile.interPatientWait);
    const registerResult = await registerPatient(caseItem);

    registeredCases.push({
      ...caseItem,
      registerResult,
      timings: {
        arrivalGapMs,
        cashierDwellMs: 0,
        medicalCallDwellMs: 0,
        consultationDwellMs: 0,
      },
    });

    if (
      caseItem.index % progressInterval === 0 ||
      caseItem.index === casePlan.length
    ) {
      console.log(
        `[registration] registered ${caseItem.index}/${casePlan.length}`,
      );
    }
  }

  return registeredCases;
}

async function callCashier(caseItem) {
  return runSerializedOperationalMutation(() =>
    request("/api/cashier/call-next", {
      method: "POST",
      headers: headersFor(`cashier-call-${caseItem.patientId}`, {
        withIdempotency: true,
      }),
      body: {
        queueId,
        cashierStationId,
      },
    }),
  );
}

async function validatePayment(caseItem, turnId) {
  return runSerializedOperationalMutation(() =>
    request("/api/cashier/validate-payment", {
      method: "POST",
      headers: headersFor(`validate-payment-${caseItem.patientId}`),
      body: {
        turnId,
        queueId,
        patientId: caseItem.patientId,
        paymentReference: `PAY-${caseItem.patientId}`,
        validatedAmount: 25,
      },
    }),
  );
}

async function markPaymentPending(caseItem, turnId) {
  return runSerializedOperationalMutation(() =>
    request("/api/cashier/mark-payment-pending", {
      method: "POST",
      headers: headersFor(`payment-pending-${caseItem.patientId}`),
      body: {
        turnId,
        queueId,
        patientId: caseItem.patientId,
        reason: "Payment abandoned during simulation",
        attemptNumber: 1,
      },
    }),
  );
}

async function markCashierAbsent(caseItem, turnId) {
  return runSerializedOperationalMutation(() =>
    request("/api/cashier/mark-absent", {
      method: "POST",
      headers: headersFor(`cashier-absent-${caseItem.patientId}`),
      body: {
        turnId,
        queueId,
        patientId: caseItem.patientId,
        reason: "Patient did not complete cashier step",
      },
    }),
  );
}

async function callMedical(caseItem, room) {
  return runSerializedOperationalMutation(() =>
    request("/api/medical/call-next", {
      method: "POST",
      headers: headersFor(`medical-call-${caseItem.patientId}-${room.roomId}`),
      body: {
        queueId,
        consultingRoomId: room.roomId,
      },
    }),
  );
}

async function startConsultation(turnId, caseItem, room) {
  return runSerializedOperationalMutation(() =>
    request("/api/medical/start-consultation", {
      method: "POST",
      headers: headersFor(
        `start-consultation-${caseItem.patientId}-${room.roomId}`,
      ),
      body: {
        turnId,
        consultingRoomId: room.roomId,
      },
    }),
  );
}

async function completeConsultation(turnId, caseItem, room, outcome) {
  return runSerializedOperationalMutation(() =>
    request("/api/medical/finish-consultation", {
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
    }),
  );
}

async function markMedicalAbsent(turnId, caseItem, room) {
  return runSerializedOperationalMutation(() =>
    request("/api/medical/mark-absent", {
      method: "POST",
      headers: headersFor(
        `medical-absent-${caseItem.patientId}-${room.roomId}`,
      ),
      body: {
        turnId,
        queueId,
        patientId: caseItem.patientId,
        consultingRoomId: room.roomId,
        reason: "Patient cancelled before medical review",
      },
    }),
  );
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

function extractDashboardCounters(dashboard) {
  return {
    currentWaitingCount: Number(dashboard?.currentWaitingCount ?? 0),
    totalPatientsToday: Number(dashboard?.totalPatientsToday ?? 0),
    totalCompleted: Number(dashboard?.totalCompleted ?? 0),
  };
}

function dashboardCountersEqual(left, right) {
  return (
    left.currentWaitingCount === right.currentWaitingCount &&
    left.totalPatientsToday === right.totalPatientsToday &&
    left.totalCompleted === right.totalCompleted
  );
}

async function captureOperationalBaseline() {
  const timeoutAt = Date.now() + 15000;
  let lastSnapshot = null;
  let stableSince = 0;

  while (Date.now() < timeoutAt) {
    const currentSnapshot = extractDashboardCounters(await getDashboard());

    if (lastSnapshot && dashboardCountersEqual(lastSnapshot, currentSnapshot)) {
      if (stableSince === 0) {
        stableSince = Date.now();
      }

      if (Date.now() - stableSince >= 1000) {
        return { dashboard: currentSnapshot };
      }
    } else {
      stableSince = 0;
      lastSnapshot = currentSnapshot;
    }

    await delay(250);
  }

  return {
    dashboard: lastSnapshot ?? extractDashboardCounters(await getDashboard()),
  };
}

async function pollOperationalReadModels(
  totalPatients,
  completedPatients,
  baseline,
) {
  const timeoutAt = Date.now() + 60000;
  let last = null;

  while (Date.now() < timeoutAt) {
    const dashboard = await getDashboard();
    const monitor = await getMonitor();
    const dashboardCounters = extractDashboardCounters(dashboard);
    const nonTerminalEntries = (monitor?.entries ?? []).filter(
      (entry) => !terminalMonitorStatuses.has(entry.status),
    );

    last = {
      dashboard,
      monitor,
      nonTerminalEntries,
    };

    const dashboardReady =
      dashboardCounters.currentWaitingCount ===
        baseline.dashboard.currentWaitingCount &&
      dashboardCounters.totalPatientsToday ===
        baseline.dashboard.totalPatientsToday + totalPatients &&
      dashboardCounters.totalCompleted ===
        baseline.dashboard.totalCompleted + completedPatients;
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

function queueCaseResultCommit(result) {
  const commitResult = async () => {
    report.cases.push(result);
    processedPatientCount += 1;
    await deactivateRoomsAtHalfway(processedPatientCount);

    if (
      processedPatientCount % progressInterval === 0 ||
      processedPatientCount === requestedPatients
    ) {
      console.log(
        `[progress] processed ${processedPatientCount}/${requestedPatients}`,
      );
    }
  };

  const commitPromise = resultCommitChain.then(commitResult, commitResult);
  resultCommitChain = commitPromise.catch(() => {});
  return commitPromise;
}

async function processMedicalCase(caseItem, initialTurnId, baseTimings) {
  const room = await acquireConsultingRoom();

  try {
    const timings = { ...baseTimings };
    const medicalCall = await callMedical(caseItem, room);
    expectPatient(medicalCall, caseItem.patientId, "/api/medical/call-next");
    const turnId = medicalCall.turnId ?? initialTurnId;

    timings.medicalCallDwellMs = await waitForRandomDelay(
      timingProfile.medicalCallDwell,
    );

    if (caseItem.category === "cancelled") {
      await markMedicalAbsent(turnId, caseItem, room);
      const trajectory = await pollTrajectory(caseItem, "TrayectoriaCancelada");

      return {
        ...caseItem,
        roomId: room.roomId,
        timings,
        finalTrajectoryState: trajectory.currentState,
        trajectoryId: trajectory.trajectoryId,
        closedAt: trajectory.closedAt,
        outcome: "cancelled-at-consultation",
      };
    }

    await startConsultation(turnId, caseItem, room);
    timings.consultationDwellMs = await waitForRandomDelay(
      timingProfile.consultationDwell,
    );

    const consultationOutcome =
      caseItem.category === "medicallyReviewed" ? "follow-up" : "completed";

    await completeConsultation(turnId, caseItem, room, consultationOutcome);
    const trajectory = await pollTrajectory(caseItem, "TrayectoriaFinalizada");

    return {
      ...caseItem,
      roomId: room.roomId,
      timings,
      finalTrajectoryState: trajectory.currentState,
      trajectoryId: trajectory.trajectoryId,
      closedAt: trajectory.closedAt,
      medicalOutcome: consultationOutcome,
      outcome:
        caseItem.category === "medicallyReviewed"
          ? "reviewed-by-doctor"
          : "completed",
    };
  } finally {
    await releaseConsultingRoom(room);
  }
}

async function runCashierStage(caseItem) {
  const timings = {
    arrivalGapMs: caseItem.timings?.arrivalGapMs ?? 0,
    cashierDwellMs: 0,
    medicalCallDwellMs: 0,
    consultationDwellMs: 0,
  };
  const registerResult = caseItem.registerResult;
  const cashierCall = await callCashier(caseItem);
  expectPatient(cashierCall, caseItem.patientId, "/api/cashier/call-next");
  let turnId = cashierCall.turnId ?? registerResult.turnId;

  if (!turnId) {
    throw new Error(`No turnId returned for patient ${caseItem.patientId}`);
  }

  timings.cashierDwellMs = await waitForRandomDelay(timingProfile.cashierDwell);

  if (caseItem.category === "abandoned") {
    await markCashierAbsent(caseItem, turnId);

    return {
      resultPromise: pollTrajectory(caseItem, "TrayectoriaCancelada").then(
        (trajectory) => ({
          ...caseItem,
          timings,
          finalTrajectoryState: trajectory.currentState,
          trajectoryId: trajectory.trajectoryId,
          closedAt: trajectory.closedAt,
          outcome: "abandoned-at-cashier",
        }),
      ),
    };
  }

  if (caseItem.category === "unpaid") {
    await markPaymentPending(caseItem, turnId);
    await markCashierAbsent(caseItem, turnId);

    return {
      resultPromise: pollTrajectory(caseItem, "TrayectoriaCancelada").then(
        (trajectory) => ({
          ...caseItem,
          timings,
          finalTrajectoryState: trajectory.currentState,
          trajectoryId: trajectory.trajectoryId,
          closedAt: trajectory.closedAt,
          outcome: "unpaid-payment-pending",
        }),
      ),
    };
  }

  await validatePayment(caseItem, turnId);

  return {
    resultPromise: processMedicalCase(caseItem, turnId, timings),
  };
}

function buildSummary(results, operational, baseline) {
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
      baseline: baseline.dashboard,
      currentWaitingCount: operational.dashboard.currentWaitingCount,
      currentWaitingCountDelta:
        operational.dashboard.currentWaitingCount -
        baseline.dashboard.currentWaitingCount,
      totalPatientsToday: operational.dashboard.totalPatientsToday,
      totalPatientsTodayDelta:
        operational.dashboard.totalPatientsToday -
        baseline.dashboard.totalPatientsToday,
      totalCompleted: operational.dashboard.totalCompleted,
      totalCompletedDelta:
        operational.dashboard.totalCompleted -
        baseline.dashboard.totalCompleted,
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
  const operationalBaseline = await captureOperationalBaseline();
  const caseTasks = [];
  let cashierPipeline = Promise.resolve();

  for (
    let batchStart = 0;
    batchStart < casePlan.length;
    batchStart += registrationBatchSize
  ) {
    const batch = casePlan.slice(
      batchStart,
      batchStart + registrationBatchSize,
    );
    const registeredCases = await registerCasePlan(batch);

    for (const caseItem of registeredCases) {
      cashierPipeline = cashierPipeline.then(async () => {
        const { resultPromise } = await runCashierStage(caseItem);
        caseTasks.push(
          resultPromise.then((result) => queueCaseResultCommit(result)),
        );
      });
    }
  }

  await cashierPipeline;
  await Promise.all(caseTasks);
  await resultCommitChain;
  report.cases.sort((left, right) => left.index - right.index);

  const operational = await pollOperationalReadModels(
    totalPatients,
    completedPatients,
    operationalBaseline,
  );
  report.summary = buildSummary(report.cases, operational, operationalBaseline);

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

  if (report.summary.dashboard.currentWaitingCountDelta !== 0) {
    throw new Error(
      `Dashboard currentWaitingCount delta expected 0 but received ${report.summary.dashboard.currentWaitingCountDelta} (baseline ${report.summary.dashboard.baseline.currentWaitingCount}, current ${report.summary.dashboard.currentWaitingCount})`,
    );
  }

  if (report.summary.dashboard.totalCompletedDelta !== completedPatients) {
    throw new Error(
      `Dashboard totalCompleted delta expected ${completedPatients} but received ${report.summary.dashboard.totalCompletedDelta} (baseline ${report.summary.dashboard.baseline.totalCompleted}, current ${report.summary.dashboard.totalCompleted})`,
    );
  }

  if (report.summary.dashboard.totalPatientsTodayDelta !== totalPatients) {
    throw new Error(
      `Dashboard totalPatientsToday delta expected ${totalPatients} but received ${report.summary.dashboard.totalPatientsTodayDelta} (baseline ${report.summary.dashboard.baseline.totalPatientsToday}, current ${report.summary.dashboard.totalPatientsToday})`,
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
