# ORQUESTADOR DE TRAYECTORIAS CLÍNICAS SINCRONIZADAS

## RESUMEN DE IMPLEMENTACIÓN

El objetivo de esta feature, es transformar el sistema RLApp, de un sistema de gestión de colas fragmentado a un sistema de orquestación real, que permita:

1. Mantener una trayectoria única y coherente del paciente desde la admisión hasta la finalización del proceso.
2. Garantizar la trazabilidad completa y visibilidad del estado global del paciente.
3. Reducir latencias críticas y reprocesos de la información.
4. cumplir con los estándares internacionales de seguridad, privacidad y cumplimientos normativos de los datos clínicos.

## CONTEXTO Y ANTECEDENTES DEL PROBLEMA

Actualmente se utiliza Event Sourcing y CQRS, pero las auditorias realizadas por la IA previamente al momento de identificar fallos internos y o propuestas de mejoras para implementar una nueva feature, identificaron:

1. Fragmentación del flujo, y es que en cada etapa del sistema o sus estados (admisión, espera, caja, medico) están funcionando de manera independiente.
2. Polling Lag, en Outbox Processor hasta 5 segundos de afectación de la respuesta de la UI.
3. Reprocesos de datos, generando ineficiencia y errores potenciales.
4. Falta de un nexo persistente que unifique el recorrido completo del paciente.

## PROBLEMA IDENTIFICADO

1. Fragmentación del flujo de atención: imposibilidad de tener un panorama completo del recorrido del paciente.
2. Reprocesos administrativos: duplicación de ingresos de información entre etapas.
3. Visibilidad limitada del estado global: no se sabe en qué etapa del flujo completo se encuentra un paciente en tiempo real.
4. Latencia operativa alta: actualización de UI con retrasos de hasta 5 segundos.

## PROPUESTA DE SOLUCIÓN

Se propone implementar un orquestador de flujos distribuidos y persistentes que gestione la trayectoria completa del paciente a lo largo de todas las etapas clínicas y su trayectoria clínica o asignación de turnos, coherente, consistente y trazable.

**Cómo funciona:**

* Cada paciente tendrá una trayectoria única que centraliza toda su información, evitando que el personal tenga que ingresar la misma información varias veces.
* El sistema gestiona automáticamente las etapas del proceso clínico, evitando que el personal tenga que ingresar la misma información varias veces.
* Las actualizaciones sobre el estado del paciente se reflejan en tiempo real para todos los usuarios relevantes, eliminando demoras en la toma de decisiones.
* Se guardará un historial completo y seguro de cada paso del paciente, permitiendo auditorías y cumplimientos normativos.

**Beneficios esperados:**

1. Reducción de reprocesos y errores administrativos.
   El personal no necesita duplicar registros ni verificar información manualmente, lo que ahorra tiempo y reduce la probabilidad de errores.
2. Visibilidad total del recorrido del paciente.
   Los administradores y médicos pueden conocer en cualquier momento en qué etapa del proceso se encuentra cada paciente, optimizando la coordinación y la asignación de recursos.
3. Cumplimiento normativo y seguro de la información.
   Toda la información del paciente se gestiona de forma segura y trazable, garantizando confidencialidad, integridad y disponibilidad. El sistema cumple con regulaciones locales e internacionales de privacidad de datos, incluyendo ISO 27001 (gestión de seguridad de la información) e IEEE 11073 (interoperabilidad y estándares de información clínica).

## DOCUMENTO DE REQUISITOS DE PRODUCTOS (PRD)

### Visión

RLApp se posicionará como un sistema de referencia en eficiencia clínica, mediante la sincronización integral del flujo del paciente. Esto permitirá eliminar fricciones administrativas y técnicas, garantizando continuidad, trazabilidad y visibilidad completa de cada etapa del proceso clínico.

### Objetivos

Trazabilidad completa. Garantizando que el 100% de las transacciones del paciente se registren y sean auditables.

Reducción de latencia. Mejorará la actualización de la UI de alrededor de 5 segundos a menos de 0.5 segundos (valor aproximado), ofreciendo visibilidad casi en tiempo real

Cumplimiento normativo. Asegurar la confidencialidad, integridad y disponibilidad de datos clínicos sensibles, cumpliendo con la ISO/IEC 27001, IEEE 11073, HIPAA, GDPR y Ley 1581 Colombia (Este conjunto de normas, leyes y estándares representa el marco fundamental para la seguridad, privacidad e interoperabilidad en la gestión de datos, especialmente en el sector salud y tecnologías digitales.)

### Alcance

#### Incluye

* Implementación de un sistema centralizado que gestione el recorrido completo del paciente, unificando su información a lo largo de todas las etapas del proceso.
* Disponibilidad de actualizaciones en tiempo real del estado del paciente, permitiendo una respuesta operativa inmediata por parte del personal.
* Registro y almacenamiento de un historial completo y trazable de la atención del paciente, accesible para consulta y auditoría.
* Disponibilidad de consultas y reportes del estado global del paciente, facilitando la toma de decisiones y el control operativo.

#### Excluye

* Cambios estructurales en el sistema actual de facturación (solo referencia y simulación de pagos), el cual continuará operando como hasta ahora.
* Transformaciones mayores en la infraestructura tecnológica existente, más allá de ajustes necesarios para mejorar el rendimiento del sistema.

### KPIs de éxito

* Tiempo de actualización del sistema: Reducción de tiempo significativo a una experiencia casi en tiempo real.
* Errores en el flujo del paciente: Disminución significativa de inconsistencias en el proceso.
* Eficiencia operativa del personal: Reducción de tareas manuales y reprocesos administrativos.
* Cumplimiento normativo: Garantía de auditorías exitosas sin incidencias relacionadas con privacidad o manejo de datos.

### Supuestos y restricciones

* La infraestructura tecnológica actual es capaz de soportar el incremento en procesamiento y almacenamiento de información.
* El sistema de mensajería y comunicación entre módulos es lo suficientemente estable o podrá ser ajustado sin afectar la operación.
* Las interfaces utilizadas por el personal clínico permiten recibir actualizaciones en tiempo real.
* Los sistemas actuales (admisión, Médico, Caja) podrán integrarse sin necesidad de cambios significativos en su funcionamiento.

## REGLAS DE NEGOCIO E INVARIANTES

### 1. Reglas de unicidad y estado

* RN-01: Un paciente debe tener una única trayectoria activa.
* RN-02: No pueden existir trayectorias duplicadas.
* RN-03: Un paciente no puede estar en múltiples etapas simultáneamente.
* RN-04: Toda trayectoria debe tener un estado actual único.

### 2. Reglas de inicio y finalización

* RN-05: Toda trayectoria inicia con una etapa válida.
* RN-06: Toda trayectoria debe finalizar explícitamente.
* RN-07: No existen trayectorias en estado indefinido.

### 3. Reglas de transición

* RN-08: No hay transición sin estado previo.
* RN-09: Las transiciones deben respetar el flujo permitido.
* RN-10: No se permiten saltos inválidos.
* RN-11: Las transiciones son atómicas.

### 4. Reglas de integridad de datos

* RN-12: La información debe mantenerse consistente.
* RN-13: No se debe reingresar información existente.
* RN-14: El sistema debe ser idempotente.
* RN-15: La trayectoria es la fuente única de verdad.

### 5. Reglas de trazabilidad y auditoría

* RN-16: Registrar historial completo.
* RN-17: Cada evento debe tener timestamp, actor e identificador.
* RN-18: Historial inmutable.
* RN-19: Orden cronológico garantizado.
* RN-20: No modificaciones retroactivas.

### 6. Reglas de concurrencia y consistencia

* RN-21: Manejo correcto de concurrencia.
* RN-22: Uso de control optimista.
* RN-23: No estados intermedios visibles.

### 7. Reglas de disponibilidad y tiempo real

* RN-24: Estado disponible en tiempo cercano a real.
* RN-25: Propagación consistente de eventos.
* RN-26: Tolerancia a fallos.

### 8. Reglas de cumplimiento normativo y seguridad

* RN-27: Confidencialidad, integridad y disponibilidad.
* RN-28: Auditoría completa.
* RN-29: Cumplimiento normativo.
* RN-30: Control de acceso por roles.

## HISTORIAS DE USUARIO (HU)

### 1. HU-01 Continuidad del paciente

**Actor:** Sistema

**Historia:** Como sistema, quiero mantener una única trayectoria activa por paciente durante su paso por el sistema, para asegurar la continuidad de su atención entre las diferentes etapas.

**Descripción funcional:** El sistema debe gestionar el recorrido del paciente como una única trayectoria continua y persistente, centralizando toda su información en un solo flujo. Esta trayectoria debe actuar como fuente única de verdad del estado del paciente, evitando fragmentación, duplicidad de registros y pérdida de contexto entre etapas.

**Valor de negocio:**

* Garantiza continuidad en la atención del paciente
* Reduce errores operativos y duplicidad de información
* Permite trazabilidad confiable del proceso clínico
* Soporta cumplimiento normativo y auditorías

**Relación con reglas de negocio:**

* RN-01, RN-03, RN-05

**Análisis INVEST:**

* I (Independiente): Base estructural del sistema
* N (Negociable): No negociable
* V (Valiosa): Crítica
* E (Estimable): Sí
* S (Small): Alta
* T (Testable): Sí

**Criterios de aceptación:**

* CA-01: Un paciente no puede tener más de una trayectoria activa simultáneamente.
* CA-02: El sistema debe impedir la creación de trayectorias duplicadas.
* CA-03: Toda trayectoria debe iniciar con una etapa válida definida.
* CA-04: El estado de la trayectoria debe mantenerse consistente durante todo el proceso.
* CA-05: El sistema debe manejar correctamente acciones simultáneas sobre un mismo paciente sin generar inconsistencias.

**Story Points:** 20 SP

### 2. HU-02 Transiciones sin reproceso

**Actor:** Recepcionista

**Historia:** Como recepcionista, quiero que al enviar un paciente a la siguiente etapa, su información se conserve automáticamente, para evitar tener que registrarlo nuevamente.

**Descripción funcional:** El sistema debe permitir que la información del paciente fluya automáticamente entre las distintas etapas del proceso clínico dentro de la misma trayectoria, evitando reprocesos y eliminando la necesidad de reingresar datos ya existentes.

**Valor de negocio:**

* Reduce tiempos de atención
* Disminuye carga operativa del personal
* Minimiza errores humanos
* Mejora la eficiencia del proceso clínico

**Relación con reglas de negocio:**

* RN-08, RN-09, RN-11, RN-12, RN-13, RN-14

**Análisis INVEST:**

* I (Independiente): Dependiente de HU-01
* N (Negociable): Parcialmente negociable
* V (Valiosa): Alta
* E (Estimable): Sí
* S (Small): Media
* T (Testable): Sí

**Criterios de aceptación:**

* CA-01: La información del paciente debe mantenerse íntegra al cambiar de etapa.
* CA-02: El sistema no debe solicitar nuevamente datos previamente registrados.
* CA-03: La transición debe ejecutarse en una sola acción por el usuario.
* CA-04: No deben generarse registros duplicados ante reintentos o fallos.
* CA-05: El sistema debe validar que la transición corresponde a un flujo permitido.

**Story Points:** 8 SP

### 3. HU-03 Visibilidad del estado global

**Actor:** Administrador

**Historia:** Como administrador, quiero identificar en qué etapa del proceso se encuentra un paciente, para poder gestionarlo adecuadamente dentro del sistema.

**Descripción funcional:** El sistema debe proporcionar visibilidad clara, centralizada y actualizada del estado del paciente dentro de su trayectoria, permitiendo monitorear el flujo de atención en tiempo cercano a real y facilitar la toma de decisiones operativas.

**Valor de negocio:**

* Mejora la coordinación entre áreas
* Permite gestión eficiente del flujo de pacientes
* Reduce tiempos de espera
* Mejora la capacidad de respuesta operativa

**Relación con reglas de negocio:**

* RN-04, RN-23, RN-24, RN-25

**Análisis INVEST:**

* I (Independiente): Dependiente de HU-01
* N (Negociable): Sí
* V (Valiosa): Alta
* E (Estimable): Sí
* S (Small): Media
* T (Testable): Sí

**Criterios de aceptación:**

* CA-01: El sistema debe mostrar la etapa actual del paciente en todo momento.
* CA-02: La información debe actualizarse en tiempo cercano a real (\<1 segundo).
* CA-03: El estado mostrado debe ser consistente con la información registrada.
* CA-04: El usuario debe poder visualizar múltiples pacientes simultáneamente.
* CA-05: El sistema debe manejar correctamente la actualización ante interrupciones de conexión.

**Story Points:** 13 SP

### 4. HU-04 Trazabilidad del recorrido

**Actor:** Administrador

**Historia:** Como administrador, quiero consultar el recorrido completo de un paciente dentro del sistema, para auditar el proceso y detectar posibles fallas en la atención.

**Descripción funcional:** El sistema debe registrar y permitir consultar el historial completo del recorrido del paciente, incluyendo todas las etapas por las que ha pasado, con sus respectivos tiempos y eventos, garantizando integridad, orden cronológico y no alteración de la información.

**Valor de negocio:**

* Permite auditoría clínica y operativa
* Soporta cumplimiento normativo (ISO, privacidad, salud)
* Facilita mejora continua del proceso
* Permite análisis de eficiencia y cuellos de botella

**Relación con reglas de negocio:**

* RN-16, RN-17, RN-18, RN-19, RN-20

**Análisis INVEST:**

* I (Independiente): Dependiente de HU-01
* N (Negociable): No
* V (Valiosa): Crítica
* E (Estimable): Sí
* S (Small): Alta
* T (Testable): Sí

**Criterios de aceptación:**

* CA-01: El sistema debe mostrar el historial completo de la trayectoria del paciente.
* CA-02: Cada etapa debe incluir información de inicio y fin (timestamp).
* CA-03: El historial no puede ser modificado una vez registrado.
* CA-04: El sistema debe permitir consultas para auditoría (filtros, rangos).
* CA-05: La consulta del historial no debe afectar el rendimiento del sistema.

**Story Points:** 20 SP

## Estimación de esfuerzo

La estimación del esfuerzo se realizó mediante la técnica de Scrum Poker, considerando no solo el volumen de trabajo, sino también la complejidad técnica, el riesgo y la incertidumbre asociada a cada historia de usuario.

Story Points Totales: 61 SP

## Planificación sugerida por iteraciones

Dado que el total de 61 Story Points excede la capacidad típica de un sprint, se propone una implementación incremental en múltiples iteraciones.

Asumiendo una capacidad promedio de 20 a 21 Story Points por sprint, y considerando que en este caso particular cada sprint tiene una duración de 1 día equivalente a 8 horas laborales (lunes 30 de marzo, martes 31 de marzo y miércoles 01 de abril), la planificación sugerida es la siguiente:

### Sprint 1 (Base del sistema – usariousariousario)

**HU-01:** Continuidad del paciente (20 SP)

Implementación del núcleo del sistema: trayectoria única, invariantes de negocio, control de concurrencia y consistencia del dominio.

### Sprint 2 (Flujo operativo – 21 SP)

**HU-02:** Transiciones sin reproceso (8 SP)

**HU-03:** Visibilidad del estado global (13 SP)

Implementación del flujo entre etapas, eliminación de reprocesos y visualización del estado en tiempo cercano a real.

### Sprint 3 (Auditoría y análisis – 20 SP)

**HU-04:** Trazabilidad del recorrido (20 SP)

Implementación de historial completo, consultas de auditoría y optimización de lectura de datos.

## Justificación de la estimación

* **HU-01 (20 S(20 SP):** Representa el núcleo del sistema. Incluye gestión de consistencia basada en Event Sourcing, control de concurrencia y validación de invariantes críticos del dominio.
* **HU-02 (8 SP):** Funcionalidad acotada que reutiliza la trayectoria existente. Su complejidad se centra en validaciones de flujo e idempotencia.
* **HU-03 (13 SP):** Requiere construcción de proyecciones, actualización en tiempo cercano a real y manejo de reconexiones en la interfaz.
* **HU-04 (20 SP):** Alta complejidad debido al volumen de datos históricos, necesidad de consultas eficientes y cumplimiento de requisitos de auditoría e inmutabilidad.

## REQUISITO NO FUNCIONAL (RNF)

### 1. Rendimiento

**Objetivo:** Garantizar tiempos de respuesta en tiempo cercano a real para la operación clínica.

**Requisitos:**

* RNF-01: El sistema debe reflejar cambios de estado del paciente en menos de 500 ms en la interfaz de usuario.
* RNF-02: El tiempo de procesamiento de una transición de etapa no debe superar los 200 ms en condiciones normales.
* RNF-03: El sistema debe soportar múltiples transiciones concurrentes sin degradación perceptible del rendimiento.
* RNF-04: El retraso máximo en la propagación de eventos no debe superar 1 segundo.

**Implicaciones técnicas:**

* Eliminación de polling: uso de event-driven push (WebSockets o SSE).
* Optimización del Outbox Pattern.
* Uso de proyecciones optimizadas (read models).

### 2. Escalabilidad

**Objetivo:** Permitir crecimiento del sistema sin afectar al rendimiento.

**Requisitos:**

* RNF-05: El sistema debe soportar incremento en número de pacientes concurrentes sin degradación significativa.
* RNF-06: La arquitectura debe permitir escalado horizontal de servicios.
* RNF-07: Las lecturas (queries) deben escalar independientemente de las escrituras.

**Implicaciones técnicas:**

* CQRS (separación read/write)
* Microservicios o módulos desacoplados
* Uso de colas/event bus

### 3. Disponibilidad

**Objetivo:** Garantizar continuidad operativa del sistema clínico.

**Requisitos:**

* RNF-08: El sistema debe tener una disponibilidad mínima del 99.9%.
* RNF-09: Fallos en un módulo no deben afectar la operación global.
* RNF-10: El sistema debe ser tolerante a fallos en la mensajería.

**Implicaciones técnicas:**

* Retry policies
* Circuit breakers
* Eventual consistency controlada

### 4. Consistencia y Concurrencia

**Objetivo:** Garantizar integridad del estado del paciente.

**Requisitos:**

* RNF-11: El sistema debe garantizar consistencia eventual entre vistas de lectura.
* RNF-12: Debe existir control de concurrencia optimista en las trayectorias.
* RNF-13: No deben existir estados intermedios visibles al usuario.
* RNF-14: Las operaciones deben ser idempotentes ante reintentos.

**Implicaciones técnicas:**

* Versionado de eventos
* Control de conflictos
* Idempotency keys

### 5. Seguridad

**Objetivo:** Proteger datos clínicos sensibles.

**Requisitos:**

* RNF-15: Todos los datos deben ser cifrados en tránsito (HTTPS/TLS).
* RNF-16: Debe implementarse control de acceso basado en roles (RBAC).
* RNF-17: El sistema debe registrar accesos y modificaciones (auditoría).
* RNF-18: Los datos sensibles deben protegerse contra accesos no autorizados.

**Implicaciones técnicas:**

* ISO 27001
* HIPAA
* GDPR
* Ley 1581 Colombia

### 6. Trazabilidad y Auditoría

**Objetivo:** Garantizar seguimiento completo del paciente.

**Requisitos:**

* RNF-19: Todas las acciones deben ser registradas como eventos inmutables.
* RNF-20: Cada evento debe incluir:
  * timestamp
  * actor
  * tipo de evento
* RNF-21: El sistema debe permitir reconstrucción completa del estado del paciente.

**Implicaciones técnicas:**

* Event Sourcing obligatorio
* Event Store persistente

### 7. Mantenibilidad

**Objetivo:** Facilitar la evolución del sistema.

**Requisitos:**

* RNF-22: El sistema debe estar desacoplado por dominios.
* RNF-23: Los cambios en una etapa no deben afectar a otras.
* RNF-24: El código debe permitir pruebas unitarias y de integración.

### 8. Usabilidad

**Objetivo:** Mejorar la experiencia del personal clínico.

**Requisitos:**

* RNF-25: La interfaz debe reflejar cambios en tiempo real.
* RNF-26: El sistema debe minimizar la interacción manual repetitiva.
* RNF-27: La información debe presentarse de forma clara y consistente.

## MODELO CONCEPTUAL Y ARQUITECTURA

### Modelo conceptual

El sistema se basa en un enfoque Domain-Driven Design (DDD) donde la entidad central es la Trayectoria del Paciente, la cual actúa como Aggregate Root y fuente única de verdad del sistema.

#### Entidades principales

**Paciente**

* Identificador único
* Información básica (nombre, documento, etc.)
* No contiene estado clínico dinámico

**Trayectoria**

* Aggregate Root
* Representa el flujo completo del paciente:
  * Estado actual
  * Historial de eventos
  * Referencia al paciente

**Etapa**

* Representa una fase del proceso clínico:
  * Admisión
  * Espera
  * Consulta médica
  * Caja
* No es persistida como entidad independiente, sino como parte del flujo

**Evento de Trayectoria**

* Unidad mínima de cambio
* Inmutable

**Usuario (Actor)**

* Representa quién ejecuta la acción:
  * Recepcionista
  * Médico
  * Administrador
  * Sistema

### Diagrama Entidad-Relación (ER)

(...IMAGEN…)

### Explicación del Modelo Entidad-Relación (ER)

El modelo ER traduce las reglas del dominio clínico en estructuras de datos que garantizan la unicidad, trazabilidad e integridad del recorrido del paciente. Las tablas son las siguientes:

**PACIENTE:** Almacena la información demográfica y administrativa del paciente.

Un paciente puede tener múltiples trayectorias a lo largo del tiempo (atenciones diferentes), pero solo una activa en un momento dado (RN‑01).

La información básica no se duplica; se reutiliza en cada trayectoria, evitando reprocesos (RN‑13).

**TRAYECTORIA:** Representa el flujo completo de una atención, desde la admisión hasta la finalización.

Única trayectoria activa por paciente: mediante una restricción en la base de datos (índice único parcial en patient\_id donde completed\_at IS NULL) o validación en el dominio (RN‑01).

Control de concurrencia: el campo versión se incrementa con cada cambio, al actualizar se verifica que coincida con la versión esperada, evitando actualizaciones simultáneas inconsistentes (RN‑22).

Correlación: correlation\_id permite seguir el flujo a través de distintos microservicios o módulos, facilitando la trazabilidad y debugging (RNF‑19).

**EVENTOS\_TRAYECTORIA:** Registro inmutable de todos los cambios que ocurren en una trayectoria. Es la base del Event Sourcing.

Inmutabilidad: una vez insertado, un evento nunca se modifica (RN‑18).

Orden cronológico: la secuencia de eventos por trayectoria se define por timestamp o un número de secuencia (no mostrado en el esquema, pero debería existir un campo sequence).

Reconstrucción del estado: aplicando todos los eventos en orden se obtiene el estado actual de la trayectoria (RNF‑21).

Auditoría completa: cada evento contiene el actor (staff\_id) y el cambio de etapa, cumpliendo RN‑17.

**HISTORIAL\_AUDITORIA:** Registro de acciones de seguridad y acceso, independiente de los eventos de dominio. Útil para cumplir con ISO 27001, HIPAA y Ley 1581\.

Separación de preocupaciones: mientras que EVENTOS\_TRAYECTORIA registra los cambios de estado del dominio, HISTORIAL\_AUDITORIA registra accesos y acciones de seguridad (ej. consulta de datos de un paciente por un rol no autorizado).

Cumplimiento normativo: permite demostrar quién accedió a qué información, desde qué IP y con qué resultado (RNF‑17).

No reemplaza al Event Sourcing: es un complemento obligatorio para auditorías de seguridad.

**Relaciones clave**

PACIENTE (1) a TRAYECTORIA (N): un paciente puede tener múltiples trayectorias históricas.

TRAYECTORIA (1) a EVENTOS\_TRAYECTORIA (N): una trayectoria genera muchos eventos.

PACIENTE (1) a HISTORIAL\_AUDITORIA (N): un paciente puede aparecer en múltiples registros de auditoría.

EVENTOS\_TRAYECTORIA a HISTORIAL\_AUDITORIA: opcionalmente, un evento puede tener una entrada en auditoría (campo audit).

Este modelo ER garantiza que todas las reglas de negocio relacionadas con la unicidad, concurrencia, trazabilidad y auditoría estén soportadas tanto a nivel de base de datos como en la lógica del dominio.

### Diagrama de bloques

(...IMAGEN…)

### Explicación del Diagrama de Bloques

El diagrama de bloques muestra la interacción de los componentes técnicos para orquestar la trayectoria del paciente en tiempo real. A continuación se describe el flujo numerado, integrando las tablas y servicios.

1. Usuario envía comando
2. Validación en Application Layer (BFF)
3. Ejecución en el Aggregate Root (TRAYECTORIA)
4. Almacenamiento transaccional con Outbox
5. Publicación al Event Bus
6. Distribución de eventos a consumidores
7. Actualización de proyecciones
8. Notificación en tiempo real
9. Pasos adicionales: Consultas de auditoría e historial

* Un administrador puede consultar el historial completo de un paciente desde la UI.
* La solicitud se dirige directamente a la Audit Trace / History Log (o al Event Store) para obtener el recorrido completo.
* Lógica de negocio: se respeta la inmutabilidad del historial y se proporciona trazabilidad total (HU‑04, RN‑16‑20).

## EXPLICACIÓN ARQUITECTÓNICA

La arquitectura del sistema está diseñada para transformar la gestión clínica en un modelo orquestado, trazable y en tiempo real, combinando principios de sistemas distribuidos con un enfoque centrado en el paciente como eje del negocio.

### Pilares Arquitectónicos

#### Trazabilidad total como ventaja competitiva

Se implementa un modelo basado en eventos inmutables donde cada interacción del paciente queda registrada.

**Valor de negocio:**

* Auditoría completa y cumplimiento normativo
* Reconstrucción del historial clínico en cualquier momento
* Reducción de riesgos operativos y legales

#### Escalabilidad y rendimiento mediante separación de responsabilidades (CQRS)

Se separan las operaciones de escritura y lectura, permitiendo optimizar cada una de forma independiente.

**Valor de negocio:**

* Respuesta rápida en consultas operativas
* Escalabilidad sin afectar la operación clínica
* Mejora en la experiencia del usuario

#### Orquestación consistente del flujo clínico

La lógica de negocio se centraliza en la trayectoria del paciente, garantizando transiciones válidas y consistentes.

**Valor de negocio:**

* Eliminación de reprocesos
* Reducción de errores humanos
* Flujo clínico controlado y predecible

#### Comunicación eficiente y en tiempo real

Se optimiza la propagación de eventos eliminando latencias críticas mediante mensajería asincrónica y comunicación en tiempo real.

**Valor de negocio:**

* Visibilidad casi inmediata del estado del paciente
* Mejora en la toma de decisiones operativas
* Coordinación efectiva entre áreas

#### Arquitectura desacoplada y extensible

El uso de un bus de eventos permite desacoplar los componentes del sistema.

**Valor de negocio:**

* Facilidad para integrar nuevos módulos o servicios
* Escalabilidad horizontal
* Evolución del sistema sin afectar el core

#### Optimización de consultas y analítica

Se utilizan vistas especializadas para operación en tiempo real y auditoría histórica.

**Valor de negocio:**

* Acceso rápido a información crítica (\<100 ms)
* Soporte a analítica y mejora continua
* Identificación de cuellos de botella operativos

#### Seguridad y cumplimiento normativo

Se implementan controles de acceso, auditoría y cifrado de datos.

**Valor de negocio:**

* Cumplimiento de estándares internacionales (ISO 27001, HIPAA, GDPR)
* Protección de datos sensibles
* Confianza institucional y regulatoria

#### Consistencia y resiliencia operativa

Se aplican mecanismos de control de concurrencia e idempotencia.

**Valor de negocio:**

* Prevención de inconsistencias en escenarios concurrentes
* Operaciones seguras ante fallos o reintentos
* Alta confiabilidad del sistema
