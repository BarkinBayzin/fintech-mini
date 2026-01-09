## ⚠️ Configuration Notice

This repository is a **technical case / demo project**.

For ease of review and quick startup:
- `appsettings.Development.json` files are intentionally committed
- Credentials are **local / non-production only**
- Keycloak, RabbitMQ, and PostgreSQL run via Docker

⚠️ **Do NOT reuse these settings in real environments.**

---

## Authentication (Demo)

This project uses **Keycloak** for authentication and role-based authorization.

- The `ops` role is required for sensitive operations such as **payment capture**
- Authorization is enforced both on the **backend (API)** and **frontend (UI)**

### Demo Login (No Postman Needed)

To simplify evaluation, a **demo login screen** is provided:

1. Navigate to `/login`
2. Login using a Keycloak demo user (password grant – demo only)
3. You will be redirected to the Payments UI

> No Postman or manual token handling is required.

The focus of this setup is to demonstrate:
- Backend authorization with JWT + roles
- Event-driven payment flow (Outbox + RabbitMQ)
- Clean separation of responsibilities between UI and API
