# Composition Root Strategy

## Rule

Program.cs o bootstrap equivalente solo compone modulos. No define reglas de negocio.

## Registration model

- AddDomainModule no registra infraestructura
- AddApplicationModule registra casos de uso y puertos
- AddInfrastructureModule registra adaptadores concretos
- AddApiModule registra endpoints, middleware y contracts
