# Engineering Principles

## SOLID baseline

- SRP: cada clase debe tener una responsabilidad unica y verificable
- OCP: extensiones por puertos, policies o estrategias; no por condicionales cruzados
- LSP: puertos y contratos sustituibles sin comportamiento inesperado
- ISP: interfaces pequenas y orientadas a caso de uso
- DIP: aplicacion y dominio dependen de abstracciones, no de detalles concretos

## Clean code baseline

- nombres del dominio consistentes con el glosario
- metodos pequenos y con una sola intencion
- no logica de negocio en endpoints ni middleware
- no comentarios redundantes
- errores de negocio expresados con excepciones o resultados de dominio claros

## Code review rules

- toda regla nueva debe vivir en dominio o en una policy bien ubicada
- toda dependencia externa debe entrar por adaptador
- todo handler debe orquestar, no decidir reglas nucleares del negocio
