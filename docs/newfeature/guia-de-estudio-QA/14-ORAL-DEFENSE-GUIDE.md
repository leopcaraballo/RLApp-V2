# Guia de Sustentacion Oral

## Objetivo

Usar el deck ejecutivo como una defensa de 10 a 12 minutos, con foco en decision, riesgo y evidencia. Esta guia evita caer en exceso de detalle tecnico y ayuda a responder preguntas del jurado o comite evaluador.

## Estructura sugerida

| Bloque | Tiempo | Mensaje central |
| --- | --- | --- |
| Apertura | 1 min | que se evaluo y por que importa |
| Riesgo y arquitectura | 2 min | donde puede fallar una feature distribuida |
| Estrategia y evidencia | 3 min | como QA construyo confianza por capas |
| Resultados y hallazgos | 3 min | que salio bien y que no esta listo |
| Decision y plan | 2 min | por que la recomendacion es No-Go a produccion |
| Cierre | 1 min | decision responsable basada en evidencia |

## Guion por diapositiva

### Diapositiva 1

Abrir con una frase corta:

"Esta sustentacion presenta la evaluacion QA de una feature critica para trazabilidad clinica, donde el reto no es solo funcional sino distribuido, operativo y de cumplimiento."

### Diapositiva 2

Explicar el problema sin entrar en endpoints:

"La necesidad de negocio es saber en que etapa esta el paciente y poder demostrarlo con consistencia, incluso cuando hay eventos asincronos, multiples actores y reconstruccion de historia."

### Diapositiva 3

Resaltar que el riesgo esta en los bordes:

"El camino feliz no era la principal preocupacion. La prioridad fue evaluar duplicidad, concurrencia, desfase de proyecciones, seguridad y auditabilidad."

### Diapositiva 4

Defender la arquitectura de validacion:

"No intentamos probar todo desde UI. Distribuimos la validacion por capas para que cada riesgo se comprobara en la superficie adecuada."

### Diapositiva 5

Conectar estrategia y criterio de calidad:

"La confianza en release no nace de una sola suite verde. Nace de dominio, integracion, contrato, UI y resiliencia trabajando juntos."

### Diapositiva 6

Mostrar madurez sin sobre vender:

"Ya existe automatizacion real dentro del repositorio: 210 tests unitarios de backend, 25 de integracion con Testcontainers y 38 de frontend. Eso reduce riesgo de regresion, pero no significa que toda la evidencia requerida para produccion este completa."

### Diapositiva 7

Enfatizar trazabilidad:

"Cada brecha encontrada queda mapeada a historia, criterio, regla y prueba. Eso permite decidir con precision que falta cerrar y no trabajar por intuicion."

### Diapositiva 8

Presentar resultados con lectura ejecutiva:

"273 tests ejecutados con 100% pass rate. La base es solida, pero la cobertura de reglas de negocio esta al 83% y faltan benchmarks formales de latencia. Esas brechas frenan la recomendacion productiva."

### Diapositiva 9

Nombrar hallazgos sin dramatizar:

"No estamos frente a una feature rota. Estamos frente a una feature que ya entrega valor, pero cuyo riesgo residual todavia exige evidencia adicional antes de produccion."

### Diapositiva 10

Defender la decision:

"Por eso la recomendacion es Go controlado en staging o piloto tecnico, y No-Go a produccion. La razon no es miedo; la razon es control de riesgo."

### Diapositiva 11

Cerrar con plan concreto:

"La salida no es rehacer el proyecto. La salida es cerrar cinco acciones de alto impacto que ya estan identificadas y trazadas."

### Diapositiva 12

Terminar con frase de defensa:

"La mejor prueba de madurez QA no es decir que todo esta listo. Es demostrar exactamente que si esta listo, que no, y por que."

## Preguntas probables y respuestas sugeridas

### Por que no recomendar produccion si el flujo nominal funciona

Porque en una arquitectura distribuida el mayor riesgo no esta en el flujo nominal. Esta en concurrencia, convergencia y seguridad negativa. Liberar sin esa evidencia seria una decision debil desde QA.

### Entonces la feature fracaso

No. La feature demuestra valor, estabilidad basica y direccion tecnica correcta. Lo que falta es elevar el nivel de evidencia para un entorno productivo con sensibilidad clinica y operativa.

### Cual es la brecha mas importante

La mas importante es la combinacion de tres frentes: matriz RBAC negativa, concurrencia del aggregate y convergencia visible bajo lag o redelivery.

### Que ya esta ganado

Ya esta ganado el modelo de trazabilidad, la base automatizada en API y UI, el flujo nominal estable, la consulta protegida y el control inicial de rebuild dry-run.

### Que haria falta para cambiar el No-Go a Go

Cerrar las acciones del plan de salida y emitir un reporte real en ambiente prod-like con KPIs dentro de umbral y sin brechas criticas abiertas.

## Recomendaciones de presentacion

- No leer tablas completas; interpretar tendencias y decisiones.
- Usar lenguaje de riesgo y evidencia, no solo lenguaje tecnico.
- Si preguntan por detalle, llevar la respuesta al artefacto fuente correspondiente del paquete QA.
- Si cuestionan el No-Go, sostener la postura con la frase: "preferimos una liberacion defendible a una liberacion optimista".
