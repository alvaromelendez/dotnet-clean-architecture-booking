# BookingSystem (resumen en español)

[Read the full README in English](README.md)

API REST para reservar espacios compartidos (salas de reuniones, canchas, consultorios) que nunca permite
reservas solapadas. Construida con .NET 10, Clean Architecture y Domain-Driven Design.

Es un proyecto de portafolio. Muestra un modelo de dominio sin dependencias de frameworks, una base de datos
PostgreSQL que actúa como árbitro final ante condiciones de carrera (exclusion constraint), pruebas en todos los
niveles (unitarias, de integración con Testcontainers, de arquitectura y de mutación) y el trabajo de entrega:
contenedores, CI/CD, análisis de seguridad y decisiones documentadas en ADRs.

## Inicio rápido

Requisitos: Docker con Docker Compose v2.

```bash
git clone https://github.com/<OWNER>/<REPO>.git
cd <REPO>
cp .env.example .env        # y cambia POSTGRES_PASSWORD por tu propio valor
docker compose up --build
```

La API queda en <http://localhost:8080> (`/health/ready` responde `Healthy`). Para ver la UI de OpenAPI (Scalar)
en `/scalar`, define `API_ENVIRONMENT=Development` en `.env` y vuelve a ejecutar `docker compose up -d`.
Los ejemplos con `curl` están en el [README en inglés](README.md#quick-start).

## Documentación

- Arquitectura y diagramas: [docs/architecture.md](docs/architecture.md)
- Decisiones de arquitectura (ADRs): [docs/adr](docs/adr/README.md)
- Estrategia de pruebas: [docs/testing/strategy.md](docs/testing/strategy.md)

La documentación técnica completa está en inglés.
