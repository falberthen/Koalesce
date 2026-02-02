# Koalesce Samples

Sample projects demonstrating Koalesce merging capabilities.

## Quick Start (Docker)

**Swagger Sample** (Koalesce standalone):
```bash
cd samples
docker-compose --profile swagger up -d
```

**Ocelot Sample** (Koalesce + API Gateway):
```bash
cd samples
docker-compose --profile ocelot up -d
```

Open http://localhost:5000/swagger to see the merged OpenAPI spec.

## Services

| Service | URL | Description |
|---------|-----|-------------|
| Customers API | http://localhost:8001/swagger | Sample REST API |
| Products API | http://localhost:8002/swagger | Sample REST API |
| Inventory API | http://localhost:8003/swagger | Sample REST API |
| **Koalesce** | http://localhost:5000/swagger | **Merged spec from all APIs** |

## Stop

```bash
docker-compose down
```

## Notes
- Ensure Docker is installed and running on your machine.
- While alternating between samples, stop the currently running services with `docker-compose down` before starting another.
- If switching between profiles, you may need to hard refresh your browser to clear cached Swagger UI data.
- When running with Docker, services are hosted with HTTP for simplicity.
- In the Koalesce.Samples.Swagger, the inventory-api has a conflict in the "Products" schema to demonstrate Koalesce's conflict resolution capabilities.
- For debugging, you can still run the services locally (with https enabled) using multi-project solutions.
