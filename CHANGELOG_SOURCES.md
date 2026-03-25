# Orquestador de Trayectorias Clínicas Sincronizadas

---

## 1. Registro de Actividad

| Fecha      | Hora    | Descripción del Cambio | Impacto en el Diseño |
|------------|---------|------------------------|----------------------|
| 25/03/2026 | 9:30 AM | Creación de la rama y [documento base](https://docs.google.com/document/d/1tQPaExwsD1HFYFfXh1atzBVtWVXCzYJSpAJz5XLoVmQ/edit?tab=t.0) | Se inicia el análisis de la feature y se define el espacio de trabajo. |
| 25/03/2026 | 1:00 PM | Revisión de la idea inicial con apoyo de IA | Se detecta que la idea original puede estar sobredimensionada. |
| 25/03/2026 | 2:15 PM | Análisis del sistema actual de turnos | Se empieza a identificar un problema más relacionado con pacientes que con médicos. |
| 25/03/2026 | 2:20 PM | Definición del contexto y problema del sistema | Se identifica la falta de continuidad del paciente entre etapas como limitación principal. |
| 25/03/2026 | 2:30 PM | Construcción de historias de usuario | Se aterriza la solución en necesidades concretas relacionadas con continuidad, transición y trazabilidad. |

---

## 2. Bitácora de Investigación

---

### Idea inicial

Se empezó con la idea de trabajar sobre la gestión de descansos médicos (Medical Break & Conflict Resolver).

Con ayuda de IA, se plantearon posibles reglas y comportamientos, pero rápidamente surgió una duda: la solución no se sentía como una feature puntual, sino algo mucho más grande.

---

### Duda sobre el alcance

Al revisar mejor la propuesta, se hizo evidente que involucraba varias cosas al mismo tiempo:

* Estados de médicos
* Asignación de pacientes
* Validaciones críticas

Esto hizo pensar que el problema no estaba bien acotado.
Más que una feature, parecía un subsistema completo.

En este punto se decide no avanzar sin antes validar mejor el problema.

---

### Cambio de enfoque

Se revisa el sistema actual, haciendo énfasis en el flujo de los pacientes y en cómo cambian de estado.

Se observa que cada módulo trabaja de forma independiente, especialmente en las colas.

Algo que llama la atención:

* Cuando un paciente cambia de etapa, prácticamente se vuelve a registrar
* No hay una continuidad clara entre procesos

---

### Definición del problema

Se llega a una idea más clara:

El sistema maneja bien los estados dentro de cada etapa, pero no el flujo completo del paciente.

Es decir, funciona por partes, pero no como un todo.
Esto genera pérdida de contexto y dificulta el seguimiento del paciente durante su atención.

---

### Propuesta de solución

A partir de lo anterior, se plantea la creación de un **Orquestador de Trayectorias Clínicas Sincronizadas**, enfocado en manejar el recorrido completo del paciente como una sola unidad dentro del sistema.

---

### Aterrizaje inicial

En esta etapa se empieza a concretar la solución:

* Se definen historias de usuario relacionadas con continuidad, transición entre etapas y trazabilidad
