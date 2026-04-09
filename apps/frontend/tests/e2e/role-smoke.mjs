const DEFAULT_BASE_URL = process.env.RLAPP_SMOKE_BASE_URL ?? 'http://127.0.0.1:3000';
const POLL_ATTEMPTS = 30;
const POLL_INTERVAL_MS = 500;

const SUPERVISOR_CREDENTIALS = {
  identifier: 'superadmin',
  password: 'SuperAdmin@2026Dev!',
};

const SUPPORT_CREDENTIALS = {
  identifier: 'support',
  password: 'Support@2026Dev!',
};

function log(status, message) {
  console.log(`${status} | ${message}`);
}

function createCorrelationId(scope) {
  return `smoke-${scope}-${Date.now()}-${Math.random().toString(16).slice(2, 8)}`;
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function safeJsonParse(text) {
  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

function extractMessage(body, text) {
  if (body && typeof body === 'object') {
    if (typeof body.message === 'string' && body.message) {
      return body.message;
    }

    if (typeof body.detail === 'string' && body.detail) {
      return body.detail;
    }

    if (typeof body.error === 'string' && body.error) {
      return body.error;
    }
  }

  return text ? text.slice(0, 200) : 'no detail available';
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

class SessionClient {
  constructor(name, baseUrl) {
    this.name = name;
    this.baseUrl = baseUrl.replace(/\/$/, '');
    this.cookies = new Map();
  }

  getCookieHeader() {
    return Array.from(this.cookies.entries())
      .map(([key, value]) => `${key}=${value}`)
      .join('; ');
  }

  updateCookies(response) {
    const setCookieHeaders =
      typeof response.headers.getSetCookie === 'function' ? response.headers.getSetCookie() : [];

    const fallbackCookie = response.headers.get('set-cookie');
    const cookieHeaders =
      setCookieHeaders.length > 0 ? setCookieHeaders : fallbackCookie ? [fallbackCookie] : [];

    for (const cookieHeader of cookieHeaders) {
      const [cookiePair] = cookieHeader.split(';');
      const separatorIndex = cookiePair.indexOf('=');
      if (separatorIndex <= 0) {
        continue;
      }

      const name = cookiePair.slice(0, separatorIndex).trim();
      const value = cookiePair.slice(separatorIndex + 1).trim();

      if (!name) {
        continue;
      }

      if (value) {
        this.cookies.set(name, value);
      } else {
        this.cookies.delete(name);
      }
    }
  }

  async request(path, options = {}) {
    const headers = new Headers(options.headers ?? {});
    headers.set('X-Correlation-Id', createCorrelationId(this.name));

    const cookieHeader = this.getCookieHeader();
    if (cookieHeader) {
      headers.set('Cookie', cookieHeader);
    }

    let body;
    if (options.json !== undefined) {
      headers.set('Content-Type', 'application/json');
      body = JSON.stringify(options.json);
    }

    const response = await fetch(`${this.baseUrl}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body,
      redirect: options.redirect ?? 'follow',
      signal: options.signal,
    });

    this.updateCookies(response);
    return response;
  }

  async requestJson(path, options = {}) {
    const response = await this.request(path, options);
    const text = await response.text();
    return {
      response,
      text,
      body: safeJsonParse(text),
    };
  }

  async login(credentials) {
    const { response, body, text } = await this.requestJson('/api/session/login', {
      method: 'POST',
      json: credentials,
    });

    assert(response.ok, `Login failed for ${this.name}: ${extractMessage(body, text)}`);
    assert(body?.session, `Login did not return a session for ${this.name}`);

    log('PASS', `${this.name} authenticated as ${body.session.role}`);
    return body.session;
  }

  async getSession() {
    const { response, body, text } = await this.requestJson('/api/session/me');
    assert(response.ok, `Session lookup failed for ${this.name}: ${extractMessage(body, text)}`);
    return body;
  }
}

async function expectOkJson(client, path, options, description) {
  const { response, body, text } = await client.requestJson(path, options);
  assert(response.ok, `${description} returned ${response.status}: ${extractMessage(body, text)}`);
  return body;
}

function assertOperationSucceeded(body, description) {
  if (body && typeof body === 'object' && Object.hasOwn(body, 'success')) {
    assert(
      body.success === true,
      `${description} failed: ${extractMessage(body, JSON.stringify(body))}`
    );
  }
}

function unwrapPayload(body) {
  if (body && typeof body === 'object' && body.data && typeof body.data === 'object') {
    return body.data;
  }

  return body;
}

async function poll(description, operation, attempts = POLL_ATTEMPTS) {
  let lastError;

  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      const result = await operation(attempt);
      if (result) {
        return result;
      }
    } catch (error) {
      lastError = error;
    }

    await sleep(POLL_INTERVAL_MS);
  }

  if (lastError) {
    throw lastError;
  }

  throw new Error(`${description} did not stabilize after ${attempts} attempts`);
}

async function assertPageContains(client, path, expectedText) {
  const response = await client.request(path);
  const html = await response.text();

  assert(response.ok, `GET ${path} returned ${response.status}`);
  assert(html.includes(expectedText), `Expected "${expectedText}" in ${path}`);

  log('PASS', `${client.name} can open ${path}`);
}

async function assertFrontendHealth(baseUrl) {
  const response = await fetch(`${baseUrl}/api/health`);
  const body = await response.json();

  assert(response.ok, `Frontend health returned ${response.status}`);
  assert(body?.status === 'ok', 'Frontend health endpoint did not report ok');

  log('PASS', 'frontend health endpoint is healthy');
}

async function assertDashboardRealtime(client) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 4000);
  let response;

  try {
    response = await client.request('/api/realtime/operations?dashboard=1', {
      signal: controller.signal,
    });

    assert(response.ok, `Dashboard realtime returned ${response.status}`);
    assert(
      (response.headers.get('content-type') ?? '').includes('text/event-stream'),
      'Dashboard realtime did not expose an SSE stream'
    );

    log('PASS', `${client.name} opened dashboard realtime stream`);
  } finally {
    controller.abort();
    clearTimeout(timeout);

    try {
      await response?.body?.cancel();
    } catch {
      // Ignore stream shutdown failures during smoke cleanup.
    }
  }
}

async function switchSupportRole(supervisorClient, supportClient, supportStaffId, newRole) {
  const changeRoleBody = await expectOkJson(
    supervisorClient,
    '/api/proxy/staff/users/change-role',
    {
      method: 'POST',
      json: {
        staffUserId: supportStaffId,
        newRole,
        reason: `Smoke validation switch to ${newRole}`,
      },
    },
    `Change support role to ${newRole}`
  );

  assertOperationSucceeded(changeRoleBody, `Change support role to ${newRole}`);

  const session = await poll(
    `support role ${newRole}`,
    async () => {
      const currentSession = await supportClient.login(SUPPORT_CREDENTIALS);
      return currentSession.role === newRole ? currentSession : null;
    },
    10
  );

  log('PASS', `support user now has role ${newRole}`);
  return session;
}

async function pollMonitorStatus(client, queueId, patientId, expectedStatus) {
  const encodedQueueId = encodeURIComponent(queueId);
  let lastFailure = 'monitor did not respond';

  const payload = await poll(`monitor status ${expectedStatus}`, async () => {
    const { response, body, text } = await client.requestJson(
      `/api/proxy/v1/waiting-room/${encodedQueueId}/monitor`
    );

    if (!response.ok) {
      lastFailure = `${response.status}: ${extractMessage(body, text)}`;
      return null;
    }

    const serialized = JSON.stringify(body);
    if (serialized.includes(patientId) && serialized.includes(expectedStatus)) {
      return body;
    }

    lastFailure = `last successful payload did not contain ${patientId} with ${expectedStatus}`;
    return null;
  }).catch((error) => {
    throw new Error(
      `Waiting room monitor failed while expecting ${expectedStatus}: ${lastFailure}. ${error.message}`
    );
  });

  log('PASS', `monitor includes ${patientId} with ${expectedStatus}`);
  return payload;
}

function hasStage(stages, sourceEvent, sourceState) {
  return (
    Array.isArray(stages) &&
    stages.some((stage) => stage.sourceEvent === sourceEvent && stage.sourceState === sourceState)
  );
}

async function pollTrajectory(client, patientId, queueId) {
  const discovery = await poll('trajectory discovery', async () => {
    const { response, body, text } = await client.requestJson(
      `/api/proxy/patient-trajectories?patientId=${encodeURIComponent(patientId)}&queueId=${encodeURIComponent(queueId)}`
    );

    assert(response.ok, `Trajectory discovery failed: ${extractMessage(body, text)}`);

    const item = body?.items?.find(
      (entry) => entry.patientId === patientId && entry.queueId === queueId
    );

    return item ?? null;
  });

  const trajectory = await poll(`trajectory ${discovery.trajectoryId}`, async () => {
    const body = await expectOkJson(
      client,
      `/api/proxy/patient-trajectories/${encodeURIComponent(discovery.trajectoryId)}`,
      undefined,
      'Get patient trajectory'
    );

    if (
      body?.currentState === 'TrayectoriaFinalizada' &&
      hasStage(body.stages, 'PatientCheckedIn', 'EnEsperaTaquilla') &&
      hasStage(body.stages, 'PatientPaymentValidated', 'EnEsperaConsulta') &&
      hasStage(body.stages, 'PatientCalled', 'LlamadoConsulta') &&
      hasStage(body.stages, 'PatientClaimedForAttention', 'EnConsulta')
    ) {
      return body;
    }

    return null;
  });

  log(
    'PASS',
    `trajectory ${trajectory.trajectoryId} finalized with cashier and consultation milestones`
  );
  return trajectory;
}

async function safeDeactivateRoom(supervisorClient, roomId) {
  if (!roomId) {
    return;
  }

  try {
    const body = await expectOkJson(
      supervisorClient,
      '/api/proxy/medical/consulting-room/deactivate',
      {
        method: 'POST',
        json: { roomId },
      },
      `Deactivate room ${roomId}`
    );

    assertOperationSucceeded(body, `Deactivate room ${roomId}`);

    if (!Object.hasOwn(body ?? {}, 'success') || body?.success === true) {
      log('PASS', `room ${roomId} deactivated`);
    }
  } catch (error) {
    log('WARN', `room cleanup skipped: ${error.message}`);
  }
}

async function safeRestoreSupportRole(supervisorClient, supportStaffId) {
  try {
    const body = await expectOkJson(
      supervisorClient,
      '/api/proxy/staff/users/change-role',
      {
        method: 'POST',
        json: {
          staffUserId: supportStaffId,
          newRole: 'Support',
          reason: 'Restore support role after smoke validation',
        },
      },
      'Restore support role'
    );

    assertOperationSucceeded(body, 'Restore support role');

    if (!Object.hasOwn(body ?? {}, 'success') || body?.success === true) {
      log('PASS', 'support role restored to Support');
    }
  } catch (error) {
    log('WARN', `role restoration skipped: ${error.message}`);
  }
}

async function main() {
  const baseUrl = DEFAULT_BASE_URL.replace(/\/$/, '');
  const smokeToken = new Date().toISOString().replace(/\D/g, '').slice(0, 14);
  const queueId = process.env.RLAPP_SMOKE_QUEUE_ID ?? `Q-SMOKE-${smokeToken}`;
  const patientId = `PAT-SMOKE-${smokeToken}`;
  const patientName = `Paciente Smoke ${smokeToken.slice(-4)}`;
  const roomId = `ROOM-SMOKE-${smokeToken.slice(-8)}`;
  const turnId = `${queueId}-${patientId}`;
  const roomName = `Consultorio Smoke ${smokeToken.slice(-4)}`;

  log('INFO', `Base URL: ${baseUrl}`);
  log('INFO', `Queue: ${queueId} | Patient: ${patientId} | Room: ${roomId}`);

  await assertFrontendHealth(baseUrl);

  const supervisorClient = new SessionClient('supervisor', baseUrl);
  const supportClient = new SessionClient('support', baseUrl);

  const supervisorSession = await supervisorClient.login(SUPERVISOR_CREDENTIALS);
  const supportSession = await supportClient.login(SUPPORT_CREDENTIALS);
  const supportStaffId = supportSession.staffId;

  assert(
    supervisorSession.role === 'Supervisor',
    'Supervisor seed user does not have Supervisor role'
  );
  assert(supportStaffId, 'Support seed user did not expose a staffId');

  await assertPageContains(supervisorClient, '/', 'Visibilidad sincronizada');
  await assertPageContains(supervisorClient, '/staff', 'Cambiar rol interno');
  await assertDashboardRealtime(supervisorClient);

  try {
    await switchSupportRole(supervisorClient, supportClient, supportStaffId, 'Receptionist');
    await assertPageContains(supportClient, '/reception', 'Registrar llegada del paciente');

    const registerBody = await expectOkJson(
      supportClient,
      '/api/proxy/reception/register',
      {
        method: 'POST',
        json: {
          queueId,
          patientId,
          patientName,
          appointmentReference: `APT-${smokeToken}`,
          priority: 'Standard',
          notes: 'Smoke role validation',
        },
      },
      'Register patient arrival'
    );
    assertOperationSucceeded(registerBody, 'Register patient arrival');
    await pollMonitorStatus(supervisorClient, queueId, patientId, 'Waiting');

    await switchSupportRole(supervisorClient, supportClient, supportStaffId, 'Cashier');
    await assertPageContains(supportClient, '/cashier', 'Pagos y ausencias en caja');

    const cashierCallBody = await expectOkJson(
      supportClient,
      '/api/proxy/cashier/call-next',
      {
        method: 'POST',
        json: {
          queueId,
          cashierStationId: 'CASH-SMOKE-01',
        },
      },
      'Call next patient at cashier'
    );
    assertOperationSucceeded(cashierCallBody, 'Call next patient at cashier');
    const cashierCallPayload = unwrapPayload(cashierCallBody);
    assert(
      cashierCallPayload?.patientId === patientId,
      `Cashier call-next returned ${cashierCallPayload?.patientId ?? 'unknown'} instead of ${patientId}`
    );
    await pollMonitorStatus(supervisorClient, queueId, patientId, 'AtCashier');

    const validateBody = await expectOkJson(
      supportClient,
      '/api/proxy/cashier/validate-payment',
      {
        method: 'POST',
        json: {
          turnId,
          queueId,
          patientId,
          paymentReference: `PAY-${smokeToken}`,
          validatedAmount: 45,
        },
      },
      'Validate payment'
    );
    assertOperationSucceeded(validateBody, 'Validate payment');
    await pollMonitorStatus(supervisorClient, queueId, patientId, 'WaitingForConsultation');

    const activateRoomBody = await expectOkJson(
      supervisorClient,
      '/api/proxy/medical/consulting-room/activate',
      {
        method: 'POST',
        json: {
          roomId,
          roomName,
        },
      },
      'Activate consulting room'
    );
    assertOperationSucceeded(activateRoomBody, 'Activate consulting room');

    await switchSupportRole(supervisorClient, supportClient, supportStaffId, 'Doctor');
    await assertPageContains(supportClient, '/medical', 'Consultorios y flujo de consulta');

    const medicalCallBody = await expectOkJson(
      supportClient,
      '/api/proxy/medical/call-next',
      {
        method: 'POST',
        json: {
          queueId,
          consultingRoomId: roomId,
        },
      },
      'Medical call-next'
    );
    assertOperationSucceeded(medicalCallBody, 'Medical call-next');
    const medicalCallPayload = unwrapPayload(medicalCallBody);
    assert(
      medicalCallPayload?.patientId === patientId,
      `Medical call-next returned ${medicalCallPayload?.patientId ?? 'unknown'} instead of ${patientId}`
    );
    await pollMonitorStatus(supervisorClient, queueId, patientId, 'Called');

    const startBody = await expectOkJson(
      supportClient,
      '/api/proxy/medical/start-consultation',
      {
        method: 'POST',
        json: {
          turnId,
          consultingRoomId: roomId,
        },
      },
      'Start consultation'
    );
    assertOperationSucceeded(startBody, 'Start consultation');
    await pollMonitorStatus(supervisorClient, queueId, patientId, 'InConsultation');

    const finishBody = await expectOkJson(
      supportClient,
      '/api/proxy/waiting-room/complete-attention',
      {
        method: 'POST',
        json: {
          turnId,
          queueId,
          patientId,
          consultingRoomId: roomId,
          outcome: 'completed',
        },
      },
      'Complete consultation'
    );
    assertOperationSucceeded(finishBody, 'Complete consultation');

    await switchSupportRole(supervisorClient, supportClient, supportStaffId, 'Support');
    await assertPageContains(supportClient, '/trajectory', 'RLApp Clinical Orchestrator');
    await pollTrajectory(supportClient, patientId, queueId);
    await assertDashboardRealtime(supportClient);

    log('PASS', 'role-based smoke completed successfully');
  } finally {
    await safeRestoreSupportRole(supervisorClient, supportStaffId);
    await safeDeactivateRoom(supervisorClient, roomId);
  }
}

main().catch((error) => {
  console.error(`FAIL | ${error.message}`);
  process.exitCode = 1;
});
