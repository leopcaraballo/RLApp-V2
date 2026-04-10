# External QA Automation Design — Serenity BDD Projects

> **Actualizada**: 2026-04-09 — Basada en analisis exhaustivo de los 3 proyectos de automatizacion externa

## Purpose

Documentar la arquitectura, patrones, estructura, interacciones y evaluacion de calidad de los tres proyectos de automatizacion externa Java/Serenity BDD que validan el comportamiento end-to-end de la feature `Orquestador de Trayectorias Clinicas Sincronizadas` desde la perspectiva del usuario y del consumidor de API.

Estos proyectos complementan los tests internos (backend xUnit + frontend vitest) documentados en `08-AUTOMATION-DESIGN.md`, cubriendo las capas de API funcional (E2E, contract, security, boundary) y UI funcional (POM + Screenplay) que el stack interno no alcanza.

---

## 1. Vision General

### 1.1 Proyectos y Responsabilidades

| Proyecto | Patron Principal | Capa Validada | Tests | Escenarios |
|---|---|---|---|---|
| `AUTO_API_SCREENPLAY` | Screenplay (API) | REST endpoints: E2E, contrato, seguridad, limites | 22 | 18 |
| `AUTO_FRONT_POM_FACTORY` | Page Object Model + Step Library | UI: login, registro, trayectoria | 12 | 7 |
| `AUTO_FRONT_SCREENPLAY` | Screenplay (Web) | UI: landing, registro, trayectoria | 8 | 6 |
| **Total** | | | **42** | **31** |

### 1.2 Stack Tecnologico Compartido

| Componente | Version | Proposito |
|---|---|---|
| Java | 21 (OpenJDK) | Runtime |
| Gradle | 8.12.1 / 9.4.1 | Build system |
| Serenity BDD | 4.0.15 | Framework de reporte y orquestacion |
| Cucumber | 7.x (via Serenity) | Motor BDD / Gherkin |
| JUnit 4 + Vintage | 4.13.2 + 5.x bridge | Runner de tests |
| REST Assured | 5.x (via Serenity) | Cliente HTTP para API tests |
| Selenium WebDriver | 4.14.1 | Automatizacion de navegador |
| Chrome Headless | 146.x | Navegador de ejecucion |

### 1.3 Infraestructura de Ejecucion

```
┌──────────────────────────────────────────────────────────────────┐
│                    Docker Compose Stack                          │
│                                                                  │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────────┐ │
│  │ PostgreSQL   │  │ RabbitMQ     │  │ Backend (.NET 10)       │ │
│  │ :5432        │  │ :5672/:15672 │  │ :5094 → :8080           │ │
│  └─────────────┘  └──────────────┘  └─────────────────────────┘ │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Frontend (Next.js 16)  :3000                                │ │
│  └─────────────────────────────────────────────────────────────┘ │
└────────────────────────────────┬─────────────────────────────────┘
                                 │
        ┌────────────────────────┼────────────────────────┐
        │                        │                        │
        ▼                        ▼                        ▼
┌───────────────┐  ┌──────────────────┐  ┌───────────────────────┐
│ AUTO_API_     │  │ AUTO_FRONT_      │  │ AUTO_FRONT_           │
│ SCREENPLAY    │  │ POM_FACTORY      │  │ SCREENPLAY            │
│               │  │                  │  │                       │
│ REST Assured  │  │ Selenium/Chrome  │  │ Selenium/Chrome       │
│ → :5094 API   │  │ → :3000 UI       │  │ → :3000 UI            │
└───────────────┘  └──────────────────┘  └───────────────────────┘
```

### 1.4 Configuracion Externalizada (3-Tier)

Los tres proyectos siguen una estrategia identica de resolucion de configuracion:

```
System Property  →  Environment Variable  →  serenity.conf (default)
```

| Variable | Proposito | Default |
|---|---|---|
| `RLAPP_API_BASE_URL` | URL base del backend API | `http://localhost:5094` |
| `RLAPP_FRONTEND_BASE_URL` | URL base del frontend | `http://localhost:3000` |
| `RLAPP_VALID_USERNAME` | Usuario Supervisor | `superadmin` |
| `RLAPP_VALID_PASSWORD` | Password Supervisor | `superadmin` |
| `RLAPP_SUPPORT_USERNAME` | Usuario Support | `support` |
| `RLAPP_SUPPORT_PASSWORD` | Password Support | `support` |

---

## 2. Arquitectura por Proyecto

### 2.1 AUTO_API_SCREENPLAY — API Testing con Screenplay

#### Estructura

```
src/
├── main/java/co/com/sofka/
│   ├── config/
│   │   └── AutomationEnvironment.java          # Config 3-tier
│   ├── interactions/
│   │   └── ExecuteRequest.java                  # Interaccion HTTP generica
│   ├── models/
│   │   ├── Patient.java                         # Entidad paciente
│   │   ├── PatientFactory.java                  # Factory con datos aleatorios
│   │   ├── ContractValidator.java               # Validador de contratos JSON
│   │   ├── ErrorResponseValidator.java          # Validador de errores canonicos
│   │   ├── TrajectoryContractFields.java        # Campos esperados en trayectoria
│   │   ├── DiscoveryContractFields.java         # Campos esperados en discovery
│   │   └── ... (9 archivos modelo)
│   ├── questions/
│   │   ├── ResponseStatusCode.java              # Verifica status HTTP
│   │   ├── ResponseBodyField.java               # Verifica campo en body
│   │   ├── RecursoConsultado.java               # (codigo muerto)
│   │   └── ApiResponseContains.java             # Contenido en respuesta
│   ├── tasks/
│   │   ├── Authenticate.java                    # Login y obtencion de token
│   │   ├── RegisterPatientAtReception.java      # POST recepcion
│   │   ├── AssignPatientToQueue.java            # Asignacion a cola
│   │   ├── ValidatePayment.java                 # Validacion de pago
│   │   ├── StartConsultation.java               # Inicio de consulta
│   │   ├── CompleteConsultation.java            # Fin de consulta
│   │   ├── DiscoverTrajectories.java            # GET discovery
│   │   ├── QueryTrajectoryDetail.java           # GET detalle
│   │   ├── RebuildProjection.java               # POST rebuild
│   │   ├── RegisterPatientBoundary.java         # Registro con datos limite
│   │   ├── ValidateContractStructure.java       # Validacion de contrato
│   │   └── ... (12 archivos task)
│   └── utils/
│       ├── ApiEndpoints.java                    # Centralizacion de rutas
│       ├── AuthTokenHolder.java                 # Singleton token JWT
│       ├── WaitHelper.java                      # Awaitility eventual consistency
│       └── TestDataCleaner.java                 # Limpieza de datos
└── test/
    ├── java/co/com/sofka/
    │   ├── runners/
    │   │   ├── FlujoAtencionTest.java           # Runner E2E
    │   │   ├── TrayectoriaPacienteTest.java     # Runner trayectoria
    │   │   ├── ContratoSeguridadApiTest.java    # Runner seguridad
    │   │   └── DatosLimitePacienteTest.java     # Runner datos limite
    │   └── stepdefinitions/
    │       ├── FlujoAtencionSteps.java           # Steps E2E
    │       ├── TrayectoriaPacienteSteps.java     # Steps trayectoria
    │       ├── ContratoSeguridadApiSteps.java    # Steps seguridad
    │       └── DatosLimitePacienteSteps.java     # Steps datos limite
    └── resources/
        ├── features/
        │   ├── flujo_atencion_paciente.feature   # E2E completo
        │   ├── trayectoria_paciente.feature      # Discovery + detalle
        │   ├── contrato_seguridad_api.feature    # 401/403/RBAC
        │   └── datos_limite_paciente.feature     # BVA + DDT
        └── serenity.conf
```

#### Diagrama de Interaccion Screenplay API

```
StepDefinitions
  ├─ theActorCalled("Supervisor") / theActorCalled("Support")
  │    via OnStage.theActorCalled()
  │
  ├─ attemptsTo(Task...)
  │    ├── Authenticate.as(role)
  │    │     └── POST /api/auth/login → AuthTokenHolder.store(token)
  │    ├── RegisterPatientAtReception.withData(patient)
  │    │     └── POST /api/reception/record  [Bearer token]
  │    ├── AssignPatientToQueue.withId(patientId)
  │    │     └── POST /api/waiting-queue/assign  [Bearer token]
  │    ├── ValidatePayment.forPatient(patientId)
  │    │     └── POST /api/cashier/validate  [Bearer token]
  │    ├── StartConsultation / CompleteConsultation
  │    │     └── POST /api/consultation/start|complete  [Bearer token]
  │    ├── DiscoverTrajectories.withFilter(...)
  │    │     └── GET /api/patient-trajectories  [Bearer token]
  │    ├── QueryTrajectoryDetail.byId(trajectoryId)
  │    │     └── GET /api/patient-trajectories/{id}  [Bearer token]
  │    ├── RebuildProjection.execute()
  │    │     └── POST /api/patient-trajectories/rebuild  [Support token]
  │    └── ValidateContractStructure.on(response)
  │          └── Validates JSON schema fields
  │
  └─ should(seeThat(Question...))
       ├── ResponseStatusCode.is(200|201|401|403)
       ├── ResponseBodyField.named("field").equalTo(value)
       └── ApiResponseContains.expectedData(...)
```

#### Patrones de Consistencia Eventual

```java
// WaitHelper.java — Awaitility para sincronizacion con Event Sourcing
WaitHelper.waitForEventualConsistency(() -> {
    actor.attemptsTo(DiscoverTrajectories.withFilter(patientId));
    return statusCodeOf(lastResponseOf(actor)) == 200
        && bodyOf(lastResponseOf(actor)).contains(patientId);
});
// Configurable: 500ms poll, 15s timeout
```

#### Escenarios de Test

| Feature | Escenario | Tipo | Cobertura |
|---|---|---|---|
| `flujo_atencion_paciente` | Flujo completo E2E | Happy path | Recepcion → Cola → Caja → Consulta → Cierre → Discovery → Detalle |
| `trayectoria_paciente` | Discovery positivo | Functional | GET filtrado retorna trayectorias |
| `trayectoria_paciente` | Discovery vacio | Negative | Paciente inexistente → lista vacia |
| `trayectoria_paciente` | Detalle cronologico | Functional | GET por ID → stages ordenados |
| `trayectoria_paciente` | Contrato de discovery | Contract | Campos obligatorios presentes |
| `trayectoria_paciente` | Contrato de detalle | Contract | Campos de etapa presentes |
| `trayectoria_paciente` | Rebuild como Supervisor | RBAC | 403 Forbidden |
| `trayectoria_paciente` | Rebuild como Support | RBAC | 200 OK |
| `trayectoria_paciente` | DDT: filtros discovery (x3) | DDT / Boundary | queueId, patientId, combinado |
| `contrato_seguridad_api` | Sin token → 401 | Security | Endpoints protegidos sin auth |
| `contrato_seguridad_api` | Token invalido → 401 | Security | Bearer token corrupto |
| `contrato_seguridad_api` | Rol sin permisos → 403 | RBAC | Support no puede operar recepcion |
| `contrato_seguridad_api` | Discovery protegido | Security | Requiere autenticacion |
| `contrato_seguridad_api` | Rebuild solo Support | RBAC | Solo rol Support puede rebuild |
| `contrato_seguridad_api` | Error canonico 404 | Contract | Estructura estandar de error |
| `datos_limite_paciente` | Nombre en limite | BVA | Exactamente 1 char, 100 chars |
| `datos_limite_paciente` | ID en limite | BVA | UUID format, empty, max length |
| `datos_limite_paciente` | DDT datos invalidos (x3) | DDT / Negative | Vacio, nulo, desbordamiento |

---

### 2.2 AUTO_FRONT_POM_FACTORY — UI Testing con Page Object Model

#### Estructura

```
src/test/
├── java/co/com/sofka/
│   ├── pages/
│   │   ├── LoginPage.java              # @FindBy + actions
│   │   ├── PublicLandingPage.java       # @FindBy + visibility
│   │   └── TrajectoryPage.java          # @FindBy + React JS injection
│   ├── runners/
│   │   ├── LoginTest.java               # CucumberWithSerenity
│   │   └── TrajectoryTest.java          # CucumberWithSerenity
│   ├── stepdefinitions/
│   │   ├── LoginStepDefinitions.java    # Glue code login
│   │   └── TrajectoryStepDefinitions.java # Glue code trayectoria
│   └── steps/
│       ├── LoginSteps.java              # @Step library: login flow
│       └── TrajectorySteps.java         # @Step library: search flow
└── resources/
    ├── features/
    │   ├── login.feature                # 4 scenarios (DDT)
    │   └── trajectory.feature           # 3 scenarios (DDT)
    └── serenity.conf
```

#### Arquitectura de 4 Capas

```
┌─────────────────────────────┐
│     Feature (Gherkin)       │  Lenguaje de negocio
├─────────────────────────────┤
│   StepDefinitions (Glue)    │  Mapeo Given/When/Then → Steps
├─────────────────────────────┤
│   Steps (@Step Library)     │  Orquestacion de acciones con @Step
├─────────────────────────────┤
│   Pages (PageObject)        │  Localizadores + interacciones con DOM
└─────────────────────────────┘
```

**Flujo de ejecucion**:

```
login.feature
  └── LoginStepDefinitions.java
        └── LoginSteps.java (@Step methods)
              └── LoginPage.java (@FindBy locators + actions)

trajectory.feature
  └── TrajectoryStepDefinitions.java
        └── TrajectorySteps.java (@Step methods)
              └── TrajectoryPage.java (@FindBy locators + React JS injection)
              └── LoginPage.java (reutilizado para autenticacion)
```

#### Manejo de React Hook Form

```java
// TrajectoryPage.java — Inyeccion JavaScript para inputs controlados
private void setReactInputValue(WebElement element, String value) {
    JavascriptExecutor js = (JavascriptExecutor) getDriver();
    js.executeScript(
        "var nativeInputValueSetter = " +
        "Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;" +
        "nativeInputValueSetter.call(arguments[0], arguments[1]);" +
        "arguments[0].dispatchEvent(new Event('input', { bubbles: true }));" +
        "arguments[0].dispatchEvent(new Event('change', { bubbles: true }));",
        element, value
    );
}
```

> **Justificacion**: React Hook Form mantiene estado interno que no se actualiza con `sendKeys()` de Selenium. La inyeccion JS con `nativeInputValueSetter` + dispatch de eventos `input`/`change` garantiza que el framework React reconozca el cambio de valor.

#### Escenarios de Test

| Feature | Escenario | Tipo | Cobertura |
|---|---|---|---|
| `login` | Login exitoso como Supervisor | Happy path | Credenciales validas → redireccion |
| `login` | Login fallido (DDT x4) | Negative / DDT | Vacio-vacio, invalido-invalido, valido-invalido, invalido-valido |
| `trajectory` | Acceso a consola de trayectoria | Smoke | Login → consola visible con status badges |
| `trajectory` | Busqueda de paciente inexistente | Negative | Random UUID → estado vacio |
| `trajectory` | Busqueda con IDs limite (DDT x3) | BVA / DDT | 1 char, NONEXISTENT-UUID, 50 chars |

---

### 2.3 AUTO_FRONT_SCREENPLAY — UI Testing con Screenplay

#### Estructura

```
src/
├── main/java/co/com/sofka/
│   ├── config/
│   │   └── AutomationEnvironment.java     # Config 3-tier
│   ├── models/
│   │   └── Patient.java                    # Modelo de paciente
│   ├── questions/
│   │   ├── PublicLandingVisible.java        # Landing publica visible?
│   │   ├── RegistrationResult.java          # Registro exitoso o error?
│   │   ├── TrajectoryConsoleVisible.java    # Consola de trayectoria visible?
│   │   └── TrajectorySearchResult.java      # Resultado de busqueda?
│   ├── tasks/
│   │   ├── Login.java                       # Navegar + autenticar
│   │   ├── OpenPublicLanding.java           # Navegar a landing
│   │   ├── OpenRegistrationPage.java        # Navegar a recepcion
│   │   ├── OpenTrajectoryPage.java          # Navegar a trayectoria
│   │   ├── FillRegistrationForm.java        # Llenar formulario (React JS)
│   │   ├── SubmitRegistration.java          # Enviar formulario
│   │   ├── SearchTrajectory.java            # Buscar trayectoria (React JS)
│   │   └── WaitForTrajectorySearchResponse.java  # Esperar respuesta
│   ├── userinterface/
│   │   ├── LoginPage.java                   # Targets de login
│   │   ├── PublicLandingPage.java           # Targets de landing
│   │   ├── RegistrationPage.java            # Targets de registro
│   │   └── TrajectoryPage.java              # Targets de trayectoria
│   └── util/
│       ├── ExpectedResult.java              # Enum SUCCESS/FAILURE
│       └── PatientFactory.java              # Factory de datos aleatorios
└── test/
    ├── java/co/com/sofka/
    │   ├── runners/
    │   │   ├── RegistrationTest.java         # Runner registro
    │   │   └── TrajectoryTest.java           # Runner trayectoria
    │   └── stepdefinitions/
    │       ├── RegistrationStepDefinitions.java
    │       └── TrajectoryStepDefinitions.java
    └── resources/
        ├── features/
        │   ├── registration.feature           # 3 escenarios
        │   └── trajectory.feature             # 3 escenarios (DDT)
        └── serenity.conf
```

#### Diagrama de Interaccion Screenplay UI

```
StepDefinitions
  ├─ theActorCalled("Supervisor") / theActorCalled("Usuario")
  │    via OnlineCast (auto-provisions WebDriver)
  │
  ├─ attemptsTo(Task...)
  │    ├── Login.withCredentials(url, user, pass)
  │    │     └── LoginPage: TXT_USERNAME, TXT_PASSWORD, BTN_LOGIN
  │    ├── OpenPublicLanding.at(url)
  │    │     └── PublicLandingPage: WELCOME_MESSAGE
  │    ├── OpenRegistrationPage.at(url)
  │    │     └── RegistrationPage: TXT_PATIENT_ID, TXT_PATIENT_NAME
  │    ├── OpenTrajectoryPage.at(url)
  │    │     └── TrajectoryPage: LBL_SECCION_CONSULTA, BTN_BUSCAR
  │    ├── FillRegistrationForm.withData(patient)
  │    │     └── React JS injection → RegistrationPage targets
  │    ├── SubmitRegistration.form()
  │    │     └── RegistrationPage: BTN_REGISTER → wait success/error
  │    ├── SearchTrajectory.forPatient(patientId)
  │    │     └── React JS injection → TrajectoryPage targets
  │    └── WaitForTrajectorySearchResponse.emptyState()
  │          └── TrajectoryPage: LBL_RESULTADO_ITEM, LBL_SIN_RESULTADOS
  │
  └─ should(seeThat(Question...))
       ├── PublicLandingVisible.isVisible()
       ├── RegistrationResult.is(SUCCESS | FAILURE)
       ├── TrajectoryConsoleVisible.isDisplayed()
       └── TrajectorySearchResult.isEmpty()
```

#### Escenarios de Test

| Feature | Escenario | Tipo | Cobertura |
|---|---|---|---|
| `registration` | Registro exitoso de paciente | Happy path | Login → llenar datos validos → submit → exito |
| `registration` | Registro fallido por datos incompletos | Negative | Login → datos incompletos → submit → error |
| `registration` | Pagina publica visible | Smoke | Navegar a landing → verificar contenido |
| `trajectory` | Acceso a consola como Supervisor | Smoke | Login → consola visible con badges |
| `trajectory` | Busqueda de paciente inexistente | Negative | UUID aleatorio → estado vacio |
| `trajectory` | Busqueda con IDs limite (DDT x3) | BVA / DDT | "X", NONEXISTENT-UUID, 50 chars |

---

## 3. Patrones de Diseno Aplicados

### 3.1 Catalogo de Patrones

| Patron | API | POM | Screenplay | Justificacion |
|---|---|---|---|---|
| **BDD (Behaviour Driven Development)** | Yes | Yes | Yes | Gherkin como lenguaje de negocio compartido |
| **Screenplay Pattern** | Yes | — | Yes | Separacion Actor/Task/Question para API y UI |
| **Page Object Model** | — | Yes | — | Encapsulacion de localizadores y acciones de pagina |
| **Step Library Pattern** | — | Yes | — | Reutilizacion de flujos con `@Step` de Serenity |
| **AAA (Arrange-Act-Assert)** | Yes | Yes | Yes | Estructura explicita en step definitions |
| **DDT (Data-Driven Testing)** | Yes | Yes | Yes | `Scenario Outline` + `Examples` |
| **Boundary Value Analysis** | Yes | Yes | Yes | Limites de longitud, UUID, campos vacios |
| **Contract Testing** | Yes | — | — | Validacion de campos JSON obligatorios |
| **Security Testing** | Yes | — | — | 401/403, RBAC, token invalido |
| **Test Data Factory** | Yes | — | Yes | Generacion aleatoria con UUID isolation |
| **Configuration-as-Code** | Yes | Yes | Yes | 3-tier environment resolution |
| **Eventual Consistency Handling** | Yes | — | — | Awaitility polling para Event Sourcing |

### 3.2 Comparativa Screenplay vs POM

| Dimension | Screenplay (API + UI) | POM |
|---|---|---|
| **Abstraccion principal** | Actor → Task → Question | Page → Steps → StepDefinition |
| **Composicion de acciones** | Fluent: `actor.attemptsTo(task1, task2)` | Secuencial: `steps.doAction()` |
| **Verificacion** | `actor.should(seeThat(question, matcher))` | `assertThat(page.getElement()).isVisible()` |
| **Reutilizacion** | Tasks y Questions independientes, composibles | Pages compartidas entre Steps |
| **Escalabilidad** | Alta — nuevos Tasks/Questions sin modificar existentes | Media — nuevas acciones requieren modificar Pages |
| **Reportes Serenity** | Reporte rico con cada Task como paso | Reporte con `@Step` como puntos de reporte |
| **Curva de aprendizaje** | Mayor — requiere entender Actor model | Menor — patron ampliamente conocido |

### 3.3 Coexistencia de Patrones: Rationale

La decision de implementar tanto POM como Screenplay para UI testing es deliberada:

1. **POM (`AUTO_FRONT_POM_FACTORY`)**: Demuestra competencia en el patron mas extendido de la industria, con 4 capas limpias y reutilizacion mediante Step Libraries.
2. **Screenplay (`AUTO_FRONT_SCREENPLAY`)**: Demuestra el patron mas avanzado, alineado con la evolucion de Serenity BDD, con mejor composicion y reportes mas ricos.
3. **Screenplay API (`AUTO_API_SCREENPLAY`)**: Extiende el patron a validacion REST, demostrando que Screenplay no se limita a UI.

---

## 4. Mapa de Interacciones entre Proyectos y SUT

### 4.1 Cobertura por Capa del Sistema

```
┌──────────────────────────────────────────────────────────────────┐
│                        CAPA DE PRUEBA                           │
├─────────────────┬──────────────────────┬────────────────────────┤
│   UI Layer      │    API Layer         │    Domain Layer        │
│                 │                      │                        │
│ AUTO_FRONT_POM  │ AUTO_API_SCREENPLAY  │ (backend xUnit)       │
│ AUTO_FRONT_     │                      │                        │
│ SCREENPLAY      │                      │                        │
├─────────────────┼──────────────────────┼────────────────────────┤
│ • Login flow    │ • Auth/token mgmt    │ • Aggregate rules     │
│ • Registration  │ • E2E patient flow   │ • State transitions   │
│ • Landing page  │ • Discovery/Detail   │ • Idempotency         │
│ • Traj. search  │ • RBAC enforcement   │ • Concurrency         │
│ • DDT/BVA       │ • Contract validation│ • Event replay        │
│ • React compat. │ • Boundary values    │ • Projection writer   │
│                 │ • Eventual consist.  │                        │
└─────────────────┴──────────────────────┴────────────────────────┘
```

### 4.2 Flujo E2E Completo: De API a UI

El flujo API E2E (`flujo_atencion_paciente.feature`) es el unico escenario que recorre todo el pipeline clinico:

```
1. Authenticate(Supervisor)
2. RegisterPatientAtReception(patient)     → POST /api/reception/record
3. AssignPatientToQueue(patientId)         → POST /api/waiting-queue/assign
4. ValidatePayment(patientId)              → POST /api/cashier/validate
5. StartConsultation(patientId)            → POST /api/consultation/start
6. CompleteConsultation(patientId)          → POST /api/consultation/complete
   ↓ (eventual consistency — Awaitility poll)
7. DiscoverTrajectories(filter)            → GET  /api/patient-trajectories
8. QueryTrajectoryDetail(trajectoryId)     → GET  /api/patient-trajectories/{id}
   ↓ (validates 5 stages chronologically ordered)
```

Los proyectos UI validan el mismo flujo desde la perspectiva del usuario:

```
Login → Registration (recepcion) → Trajectory Console (discovery/search)
```

### 4.3 Matriz de Solapamiento y Complementariedad

| Capacidad Validada | API | POM | Screenplay | Complemento |
|---|---|---|---|---|
| Autenticacion exitosa | Token JWT | Login UI | Login UI | API valida token; UI valida redireccion |
| Registro de paciente | REST payload | — | Form submit | API valida respuesta; UI valida feedback visual |
| Discovery de trayectorias | REST + contrato | Search UI | Search UI | API valida schema; UI valida rendering |
| Seguridad 401/403 | Exhaustivo | — | — | Solo API (correcto: seguridad es server-side) |
| Datos limite (BVA) | REST boundaries | DDT UI | DDT UI | API valida respuesta HTTP; UI valida mensajes |
| Landing publica | — | — | Smoke | Solo Screenplay (landing no requiere API) |
| Login fallido | — | DDT x4 | — | Solo POM (cobertura exhaustiva de combinaciones) |
| Contrato JSON | Schema fields | — | — | Solo API (contrato es server-side) |
| Eventual consistency | Awaitility | — | — | Solo API (UI no verifica convergencia programatica) |

---

## 5. Evaluacion de Calidad

### 5.1 Scorecard Consolidado

| Dimension | API | POM | Screenplay | Promedio |
|---|---|---|---|---|
| Patron principal (correctness) | 8.5/10 | 8/10 | 8/10 | **8.2/10** |
| BDD implementation | 8/10 | 7.5/10 | 7/10 | **7.5/10** |
| SOLID compliance | 8.2/10 | 7/10 | 8/10 | **7.7/10** |
| FIRST compliance | 7.6/10 | 8/10 | 7/10 | **7.5/10** |
| Code DRY-ness | 7/10 | 7.5/10 | 6/10 | **6.8/10** |
| Maintainability | 7.5/10 | 7.5/10 | 7/10 | **7.3/10** |
| Test coverage breadth | 9/10 | 7/10 | 6/10 | **7.3/10** |
| **Overall** | **8.0/10** | **7.5/10** | **7.0/10** | **7.5/10** |

### 5.2 Fortalezas Destacadas

1. **Aislamiento por UUID**: `PatientFactory` genera IDs aleatorios por ejecucion, eliminando colisiones entre tests concurrentes.
2. **Consistencia eventual**: `WaitHelper` con Awaitility resuelve correctamente el desafio de validar sistemas Event Sourcing desde tests de integracion.
3. **Seguridad exhaustiva**: 6 escenarios dedicados a RBAC, tokens invalidos y errores canonicos (sobrecobertura positiva para un sistema de salud).
4. **React Hook Form compatibility**: Solucion robusta con `nativeInputValueSetter` aplicada consistentemente en los 3 proyectos UI.
5. **Configuracion externalizada**: Los 3 proyectos permiten ejecucion en cualquier ambiente sin modificar codigo fuente.
6. **Contract testing**: Validacion de campos obligatorios en respuestas JSON, detectando drift contractual temprano.
7. **DDT + BVA**: Uso sistematico de `Scenario Outline` con tablas de datos limite para maximizar cobertura con minimo codigo.

### 5.3 Anti-Patrones y Deuda Tecnica Identificada

| ID | Proyecto | Problema | Impacto | Severidad |
|---|---|---|---|---|
| DEBT-01 | API | Codigo muerto: `RecursoConsultado`, metodos factory sin uso | Ruido en mantenimiento | Baja |
| DEBT-02 | API | Nomenclatura mixta espanol/ingles en clases y features | Confusion para nuevos contribuidores | Media |
| DEBT-03 | POM | Login duplicado en `TrajectorySteps` (deberia delegar a `LoginSteps`) | Violacion DRY | Media |
| DEBT-04 | POM | Dependencias Screenplay no usadas en `build.gradle` | Build innecesariamente pesado | Baja |
| DEBT-05 | POM | Estrategias de localizacion mixtas: `@FindBy` + `By` constants | Inconsistencia de convencion | Baja |
| DEBT-06 | Screenplay | React JS injection duplicada entre `FillRegistrationForm` y `SearchTrajectory` | Violacion DRY | Media |
| DEBT-07 | Screenplay | Logica de verificacion duplicada entre `WaitForTrajectorySearchResponse` (Task) y `TrajectorySearchResult` (Question) | Responsabilidad difusa | Media |
| DEBT-08 | Screenplay | `LBL_SECCION_CONSULTA` apunta a `#discoveryPatientId` (alias confuso) | Mantenibilidad reducida | Baja |
| DEBT-09 | Screenplay | Sin tags `@smoke` / `@regression` en features | No permite ejecucion selectiva | Media |
| DEBT-10 | Todos | JUnit 4 + Vintage Engine (JUnit 5 nativo es el path moderno) | Deuda tecnica acumulada | Baja |
| DEBT-11 | Screenplay | `invalidCredentials()` definido pero nunca usado (no hay test de login fallido) | Codigo muerto + gap de cobertura | Media |
| DEBT-12 | Screenplay | `@When` step realiza assertion (`should(seeThat(...))`) | Violacion de semantica Gherkin | Baja |

---

## 6. Metricas de Ejecucion

### 6.1 Resultados Consolidados (ultima ejecucion)

| Proyecto | Tests | Escenarios | Pasados | Fallidos | Pendientes | Tasa |
|---|---|---|---|---|---|---|
| AUTO_API_SCREENPLAY | 22 | 18 | 22 | 0 | 0 | **100%** |
| AUTO_FRONT_POM_FACTORY | 12 | 7 | 12 | 0 | 0 | **100%** |
| AUTO_FRONT_SCREENPLAY | 8 | 6 | 8 | 0 | 0 | **100%** |
| **Total** | **42** | **31** | **42** | **0** | **0** | **100%** |

### 6.2 Reportes

Los reportes Serenity BDD se generan automaticamente en `target/site/serenity/index.html` de cada proyecto.

```bash
# Ejecutar todos los tests y generar reportes
cd AUTO_API_SCREENPLAY && ./gradlew clean test
cd AUTO_FRONT_POM_FACTORY && ./gradlew clean test
cd AUTO_FRONT_SCREENPLAY && ./gradlew clean test

# Abrir reportes
xdg-open AUTO_API_SCREENPLAY/target/site/serenity/index.html
xdg-open AUTO_FRONT_POM_FACTORY/target/site/serenity/index.html
xdg-open AUTO_FRONT_SCREENPLAY/target/site/serenity/index.html
```

---

## 7. Mejores Practicas Aplicadas

### 7.1 Aislamiento de Tests

- **UUID por ejecucion**: Cada test genera su propio `patientId` con `UUID.randomUUID()`, garantizando independencia total.
- **Sin estado compartido**: Los tests no dependen de datos pre-existentes ni de orden de ejecucion.
- **OnlineCast / OnStage**: Serenity gestiona el ciclo de vida del actor y WebDriver automaticamente.

### 7.2 Resiliencia ante Frameworks Reactivos

- **React Hook Form**: `nativeInputValueSetter` + `dispatchEvent('input')` + `dispatchEvent('change')` garantiza que los inputs controlados por React reconozcan los valores inyectados.
- **Esperas explicitas**: `WebDriverWait` y `WaitUntil` de Serenity evitan flakiness por timing.

### 7.3 Configuracion como Codigo

- **3-tier resolution**: System property → Environment variable → `serenity.conf` permite ejecucion local, CI y staging sin cambios.
- **serenity.conf**: HOCON con variables de entorno para URLs, credenciales y opciones de navegador.

### 7.4 Reportes Enriquecidos

- **Serenity BDD**: Cada Task/Step se registra como paso individual en el reporte, proporcionando trazabilidad visual completa.
- **Screenshots**: Configurados como `FOR_EACH_ACTION`, capturando evidencia visual de cada interaccion.
- **Aggregate**: El goal `aggregate` de Gradle consolida los JSONs de Serenity en reportes HTML navegables.

---

## 8. Recomendaciones de Mejora

### 8.1 Prioridad Alta

| # | Recomendacion | Proyecto | Beneficio |
|---|---|---|---|
| R-01 | Extraer `ReactInputHelper` como utilidad compartida | POM + Screenplay | Eliminar duplicacion de JS injection en 4+ archivos |
| R-02 | Delegar login de `TrajectorySteps` a `LoginSteps` | POM | SRP + DRY |
| R-03 | Agregar tags Cucumber (`@smoke`, `@regression`, `@security`, `@boundary`) | Todos | Ejecucion selectiva en CI |
| R-04 | Eliminar codigo muerto (`RecursoConsultado`, factory methods sin uso, `invalidCredentials`) | API + Screenplay | Reducir ruido |

### 8.2 Prioridad Media

| # | Recomendacion | Proyecto | Beneficio |
|---|---|---|---|
| R-05 | Unificar nomenclatura a ingles en clases Java y features Gherkin | Todos | Consistencia para contribuidores |
| R-06 | Agregar escenario de login fallido (usar `invalidCredentials()` existente) | Screenplay | Cerrar gap de cobertura |
| R-07 | Separar logica de verificacion: `WaitForTrajectorySearchResponse` solo espera, `TrajectorySearchResult` solo verifica | Screenplay | SRP |
| R-08 | Eliminar dependencias Screenplay no usadas del `build.gradle` | POM | Build mas eficiente |

### 8.3 Prioridad Baja (Deuda Tecnica)

| # | Recomendacion | Proyecto | Beneficio |
|---|---|---|---|
| R-09 | Migrar de JUnit 4 + Vintage a JUnit 5 nativo (cuando Serenity lo soporte completamente) | Todos | Modernizacion del stack |
| R-10 | Unificar localizadores a una sola estrategia (`By` constants o `@FindBy`, no ambas) | POM | Consistencia |
| R-11 | Corregir alias `LBL_SECCION_CONSULTA` → target real de seccion | Screenplay | Claridad de intent |
| R-12 | Configurar ejecucion paralela de features via Cucumber parallel plugin | Todos | Reducir tiempo total |

---

## 9. Relacion con Documentacion Interna

Este documento complementa los siguientes artefactos del QA pack:

| Documento | Relacion |
|---|---|
| `03-TEST-STRATEGY.md` | Las automatizaciones externas implementan las capas "API tests" y "E2E tests" de la piramide |
| `07-TRACEABILITY-MATRIX.md` | Se actualiza para incluir mapping HU → escenarios externos |
| `08-AUTOMATION-DESIGN.md` | Cubre stack interno (xUnit/vitest); este documento cubre stack externo (Serenity BDD) |
| `10-TEST-REPORT-SIMULATED.md` | Se actualiza para incluir los 42 tests externos en el reporte consolidado |
