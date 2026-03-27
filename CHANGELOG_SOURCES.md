# Orquestador de Trayectorias Clínicas Sincronizadas

## 1. Propósito del documento

Este documento tiene como objetivo registrar la evolución del análisis, diseño y construcción de la feature **Orquestador de Trayectorias Clínicas Sincronizadas**, así como documentar las fuentes de información utilizadas y cómo estas influyeron en la solución final.

Se busca evidenciar:

- El proceso de pensamiento detrás del diseño
- La evolución del problema y la solución
- El uso de referencias reales (técnicas y normativas)
- La trazabilidad del trabajo realizado
- La alineación entre reglas de negocio, requerimientos y decisiones técnicas

---

## 2. Registro de actividad

| Descripción del cambio | Impacto en el diseño |
|------------------------|----------------------|
| Creación de la rama y documento base | Se define el espacio de trabajo inicial |
| Definición de idea inicial (gestión de descansos médicos) | Se plantea un problema inicial que luego sería replanteado |
| Análisis del sistema actual | Se identifican limitaciones en el flujo del paciente |
| Cambio de enfoque hacia el paciente | Se redefine el problema principal |
| Definición del problema real | Se identifica la fragmentación del flujo clínico |
| Propuesta del orquestador | Se plantea una solución estructural |
| Construcción de historias de usuario | Se aterriza la solución en requerimientos concretos |
| Definición inicial de reglas de negocio | Se establecen primeras restricciones del sistema |
| Refinamiento de reglas de negocio (RN-01 a RN-30) | Se formalizan invariantes del dominio y reglas críticas |
| Alineación HU ↔ Reglas de negocio | Se garantiza trazabilidad entre requerimientos y lógica del sistema |
| Definición de consistencia e idempotencia | Se fortalece el control de concurrencia y prevención de duplicados |
| Consolidación del modelo como Single Source of Truth | Se define la trayectoria como núcleo del sistema |
| Inclusión de PRD | Se formaliza la solución a nivel de producto |
| Estimación con Scrum Poker | Se dimensiona el esfuerzo técnico |
| Planificación por iteraciones | Se define una estrategia incremental de implementación |
| Inclusión de requisitos no funcionales (NFR) | Se incorporan atributos de calidad del sistema (rendimiento, escalabilidad, disponibilidad, seguridad, consistencia, trazabilidad, mantenibilidad y usabilidad) alineando el diseño con estándares de sistemas distribuidos |
| Definición del modelo conceptual | Se establece la estructura de datos del sistema |
| Definición de la arquitectura | Se establece la estructura del sistema |
| Se actualizaron los diagramas, el de Entidad-Relacion y el de bloques | Se detalló la arquitectura del sistema y se migro de la elaboracion original de los diagrmas en Miro a Elaboracion en [Mermaid](https://mermaid.ai) |
| Se añadio un bloque explicativo de los diagramas | Se detalla la arquitectura del sistema y se explica el funcionamiento de los diagramas |

---

## 3. Bitácora de investigación

### 3.1 Idea inicial

El proceso inició con la intención de desarrollar una funcionalidad relacionada con la gestión de descansos médicos.

Sin embargo, esta aproximación presentaba un enfoque limitado y fragmentado, sin resolver problemas estructurales del sistema.

---

### 3.2 Identificación del problema

Al analizar el sistema actual, se identificaron los siguientes puntos críticos:

- Cada módulo opera de forma independiente
- No existe continuidad del paciente entre etapas
- Se generan reprocesos constantes de información
- No hay visibilidad global del estado del paciente
- Existen latencias operativas debido a polling (hasta 5 segundos)

Esto permitió identificar que el problema no era funcional, sino **estructural**.

---

### 3.3 Cambio de enfoque

Se decidió cambiar el enfoque desde los módulos hacia el paciente como eje central del sistema.

Esto permitió redefinir el problema como:

> **Fragmentación del flujo clínico y ausencia de una fuente única de verdad**

---

### 3.4 Definición de la solución

Se plantea la implementación de un:

**Orquestador de Trayectorias Clínicas Sincronizadas**

Con los siguientes objetivos:

- Centralizar el flujo del paciente
- Garantizar una única trayectoria activa
- Mantener una fuente única de verdad (Single Source of Truth)
- Asegurar trazabilidad completa del proceso clínico
- Reducir latencia y reprocesos

---

### 3.5 Aterrizaje de la solución

Se definieron y formalizaron:

- Historias de usuario alineadas al dominio
- Reglas de negocio como invariantes del sistema (RN-01 a RN-30)
- Relación explícita entre historias de usuario y reglas de negocio
- Criterios de aceptación verificables (testables)
- Principios de consistencia, idempotencia y control de concurrencia

Esto permitió convertir la solución en un modelo implementable, validable y escalable.

---

### 3.6 Consolidación

Se estructuró el documento completo incluyendo:

- PRD (visión, objetivos, alcance, KPIs)
- Reglas de negocio e invariantes del dominio
- Historias de usuario con criterios de aceptación
- Estimación y planificación por iteraciones
- Consideraciones de seguridad, cumplimiento y trazabilidad
- Definición de requisitos no funcionales (rendimiento, escalabilidad, disponibilidad, consistencia, seguridad, trazabilidad, mantenibilidad y usabilidad)

---

### 3.7 Evolución técnica del diseño

Durante la evolución del documento, la solución pasó de ser una mejora funcional a convertirse en un rediseño estructural basado en:

- Event Sourcing como mecanismo de persistencia del historial
- CQRS para separación de lectura y escritura
- Uso de invariantes como mecanismo de control de consistencia
- Definición de la trayectoria como agregado principal del dominio
- Enfoque en idempotencia para evitar duplicidad de eventos
- Reducción de latencia mediante arquitectura orientada a eventos

Esto permitió que la solución no solo resolviera el problema funcional, sino que fuera:

- Escalable
- Consistente
- Tolerante a fallos
- Alineada con sistemas distribuidos modernos

---

## 4. Fuentes de información y justificación

### 4.1 Arquitectura de software

**Martin Fowler – Event Sourcing**
<https://martinfowler.com/eaaDev/EventSourcing.html>

- **Por qué se utilizó:** Proporciona la base conceptual para modelar sistemas donde el estado se deriva de eventos.
- **En qué ayudó:** Definición de la trayectoria clínica como una secuencia de eventos inmutables.
- **Para qué se usó:** Diseño del modelo de persistencia basado en Event Sourcing.

---

**Martin Fowler – Patterns of Enterprise Application Architecture (Catalog)**
<https://martinfowler.com/eaaCatalog/>

- **Por qué se utilizó:** Referencia fundamental en diseño de sistemas empresariales.
- **En qué ayudó:** Identificación de patrones para separación de responsabilidades y desacoplamiento.
- **Para qué se usó:** Definición de la arquitectura base del sistema.

---

**Vaughn Vernon – Effective Aggregate Design (DDD)**
<https://www.dddcommunity.org/wp-content/uploads/files/pdf_articles/Vernon_2011_1.pdf>

- **Por qué se utilizó:** Explica el diseño correcto de agregados en Domain-Driven Design.
- **En qué ayudó:** Definición de la trayectoria como agregado raíz y fuente de verdad.
- **Para qué se usó:** Construcción de invariantes y reglas de negocio del dominio.

---

**Microsoft Docs – Event-Driven Architecture**
<https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven>

- **Por qué se utilizó:** Guía oficial para arquitecturas orientadas a eventos.
- **En qué ayudó:** Comprensión de flujos asincrónicos y desacoplados.
- **Para qué se usó:** Diseño del sistema basado en eventos distribuidos.

---

**Microsoft Docs – SignalR (ASP.NET Core)**
<https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction>

- **Por qué se utilizó:** Tecnología para comunicación en tiempo real.
- **En qué ayudó:** Eliminación de polling y reducción de latencia.
- **Para qué se usó:** Actualización en tiempo real de la UI (<1 segundo).

---

**Microservices.io – Saga Pattern**
<https://microservices.io/patterns/data/saga.html>

- **Por qué se utilizó:** Patrón para coordinación de transacciones distribuidas.
- **En qué ayudó:** Manejo de consistencia entre múltiples etapas del flujo clínico.
- **Para qué se usó:** Diseño del orquestador como coordinador de la trayectoria.

---

### 4.2 Normativa y sector salud

**HIPAA – Security Rule Overview (HHS)**
<https://www.hhs.gov/hipaa/for-professionals/security/laws-regulations/index.html>

- **Por qué se utilizó:** Estándar clave en protección de datos de salud.
- **En qué ayudó:** Definición de controles de seguridad y privacidad.
- **Para qué se usó:** Diseño de políticas de acceso y protección de datos clínicos.

---

**GDPR – Article 17 (Right to Erasure)**
<https://gdpr-info.eu/art-17-gdpr/>

- **Por qué se utilizó:** Regulación global de protección de datos.
- **En qué ayudó:** Comprensión de derechos sobre datos personales.
- **Para qué se usó:** Complemento en diseño de privacidad y cumplimiento.

---

**Ley 1581 de 2012 – Protección de Datos Personales (Colombia)**
<https://www.funcionpublica.gov.co/eva/gestornormativo/norma.php?i=49981>

- **Por qué se utilizó:** Marco legal obligatorio en Colombia.
- **En qué ayudó:** Contextualización normativa local.
- **Para qué se usó:** Cumplimiento legal del manejo de datos clínicos.

---

**HL7 FHIR – Overview**
<https://www.hl7.org/fhir/overview.html>

- **Por qué se utilizó:** Estándar internacional de interoperabilidad en salud.
- **En qué ayudó:** Modelado estructurado de datos clínicos.
- **Para qué se usó:** Alineación del sistema con estándares del sector salud.

---

### 4.3 QA y sistemas distribuidos

**Cucumber – BDD Guide**
<https://cucumber.io/docs/bdd/>

- **Por qué se utilizó:** Framework para pruebas basadas en comportamiento.
- **En qué ayudó:** Traducción de requerimientos a criterios verificables.
- **Para qué se usó:** Validación funcional del sistema mediante escenarios.

---

**Cisco DevNet – Distributed Systems**
<https://developer.cisco.com/docs/>

- **Por qué se utilizó:** Buenas prácticas en sistemas distribuidos.
- **En qué ayudó:** Comprensión de resiliencia, latencia y comunicación.
- **Para qué se usó:** Diseño robusto y tolerante a fallos.

---

**Adobe Experience League – Event-Driven Architecture**
<https://experienceleague.adobe.com/docs/experience-platform/architecture/event-driven.html>

- **Por qué se utilizó:** Casos reales de arquitectura orientada a eventos.
- **En qué ayudó:** Integración de datos en tiempo real.
- **Para qué se usó:** Diseño del flujo de eventos del sistema.

---

### 4.4 Gestión de producto

**Atlassian – Product Requirements Document (PRD) Guide**
<https://www.atlassian.com/agile/product-management/requirements>

- **Por qué se utilizó:** Guía estructurada para documentación de producto.
- **En qué ayudó:** Organización del PRD.
- **Para qué se usó:** Definición de visión, objetivos y alcance del sistema.

---
