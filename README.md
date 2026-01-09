# Fintech Mini Platform (Demo)

Production-like fintech demo showcasing:
- Payment intents + capture flow
- Double-entry ledger (journal entries & balances)
- Event-driven integration via RabbitMQ
- Outbox pattern for reliable message publishing
- Keycloak JWT authentication + role-based authorization (ops-only capture)
- Angular UI (login, list/create/capture payments)

---

## ⚠️ Configuration Notice

This repository is a **technical case / demo project**.

For ease of review and quick startup:
- `appsettings.Development.json` files are intentionally committed
- Credentials are **local / non-production only**
- Keycloak, RabbitMQ, and PostgreSQL run via Docker

⚠️ **Do NOT reuse these settings in real environments.**

---

## Tech Stack

- Backend: .NET 8 (Minimal APIs), EF Core, PostgreSQL
- Messaging: RabbitMQ + Outbox
- Auth: Keycloak (JWT)
- Frontend: Angular (Payments UI)
- Infra: Docker Compose

---

## Quick Start

### 1) Start infrastructure

cd infra
docker compose up -d

### 2) Start backend services
cd backend/services/payments/Payments.Api
dotnet run

cd ../../ledger/Ledger.Api
dotnet run

### 3) Start frontend 
cd frontend
npm install
npm run start

### Keycloak Setup (Automatic)

The Keycloak realm, client, roles, and demo users are **automatically imported**
on startup using the provided realm export:

infra/keycloak/fintech-realm.json

No manual Keycloak configuration is required.


### Open:

Frontend: http://localhost:4200

Payments API Swagger: http://localhost:<payments-port>/swagger

Ledger API Swagger: http://localhost:<ledger-port>/swagger

Keycloak: http://localhost:8081


### Demo Accounts

Demo users are provided for evaluation.

Ops user (can capture payments):

username: ops1

password: Ops-00001

role: ops

### Demo Flow

Go to /login and sign in with the demo user

Create a payment intent

Capture the payment (requires ops role)

Ledger receives PaymentCaptured event and records a balanced journal entry

### Security Model (Demo)

JWT validation uses Keycloak issuer

Role-based authorization:

ops role is required for payment capture

Frontend hides/disables capture action for non-ops users

### Architecture Notes

Payments publishes PaymentCaptured via Outbox → RabbitMQ

Ledger consumes the event and writes a double-entry journal entry

Outbox ensures reliable publish even if the broker is temporarily unavailable

### Troubleshooting

If login fails:

Ensure Keycloak is up: http://localhost:8081  (credentials = admin/admin)

Ensure Keycloak container started successfully and the realm was imported:
- Default realm: fintech
- Demo user: ops1 / Ops-00001

If capture returns 401/403:

Verify you are logged in as ops1 and token includes ops role

