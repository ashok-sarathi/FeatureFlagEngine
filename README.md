# 🚩 FeatureFlagEngine

A modular **Feature Flag Engine** built with **.NET 10** following
**Clean Architecture** principles.\
It enables applications to turn features **ON or OFF at runtime**
without redeploying code. This service depends on **PostgreSQL** and **Redis** and is designed to
run using **Docker Compose**.

------------------------------------------------------------------------

## 📌 What is a Feature Flag?

A **feature flag (feature toggle)** allows runtime control over
application functionality.

### Common Use Cases

-   Gradual feature rollouts\
-   Safe production deployments\
-   Environment-based feature control\
-   Hiding incomplete features\
-   Emergency kill switches

------------------------------------------------------------------------

## 🧱 Architecture Overview

This project follows **Clean Architecture (Onion Architecture)** to
ensure core models remain independent from infrastructure and
frameworks.

    ┌──────────────────────────┐
    │        API Layer         │  → HTTP endpoints
    └─────────────▲────────────┘
                  │
    ┌─────────────┴────────────┐
    │    Application Layer     │  → Business logic & services
    └─────────────▲────────────┘
                  │
    ┌─────────────┴────────────┐
    │       Domain Layer       │  → Core entities (entities & models only)
    └─────────────▲────────────┘
                  │
    ┌─────────────┴────────────┐
    │   Infrastructure Layer   │  → Database & external systems
    └──────────────────────────┘

------------------------------------------------------------------------

## 🔹 Layer Responsibilities

  ------------------------------------------------------------------------
  Layer                Responsibility
  -------------------- ---------------------------------------------------
  **Domain**           Contains only core entities like `FeatureFlag` and
                       enums. No business logic and no framework
                       dependencies.

  **Application**      Contains business logic, service implementations,
                       and interfaces used by the API.

  **Infrastructure**   EF Core DbContext, PostgreSQL integration, Redis
                       integration, repository implementations.

  **API**              Controllers, middleware, dependency injection,
                       configuration.

  **Tests**            Unit and integration tests.
  ------------------------------------------------------------------------

------------------------------------------------------------------------

## 🧠 Design Principles & Patterns

The system is intentionally simple and avoids unnecessary complexity.

  Principle / Pattern        Usage
  -------------------------- --------------------------------
  **Clean Architecture**     Clear separation of concerns
  **Repository Pattern**     Abstracted data access
  **Dependency Injection**   Loose coupling
  **SOLID Principles**       Maintainable and testable code

------------------------------------------------------------------------

## 🛠️ Tech Stack

  Category           Technology
  ------------------ ----------------------------------
  Framework          **.NET 10 Web API**
  ORM                **Entity Framework Core**
  Database           **PostgreSQL**
  Cache              **Redis**
  Containerization   **Docker**
  Orchestration      **Docker Compose**
  Testing            **xUnit, Moq, FluentAssertions**

------------------------------------------------------------------------

## 📂 Solution Structure

    FeatureFlagEngine.sln
    │
    ├── FeatureFlagEngine.Domain
    ├── FeatureFlagEngine.Application
    ├── FeatureFlagEngine.Infrastructure
    ├── FeatureFlagEngine.Api
    │
    ├── FeatureFlagEngine.Application.Tests
    ├── FeatureFlagEngine.Infrastructure.Tests
    └── FeatureFlagEngine.Api.Tests

------------------------------------------------------------------------

## ⚠️ Prerequisites

This application **cannot run standalone** because it requires:

-   🐘 PostgreSQL\
-   ⚡ Redis

👉 The recommended and supported way to run the system is **Docker
Compose**

You only need:

-   Docker\
-   Docker Compose

------------------------------------------------------------------------

## 🐳 Running the System

### 1️⃣ Clone the Repository

``` bash
git clone https://github.com/ashok-sarathi/FeatureFlagEngine.git
cd FeatureFlagEngine
```

### 2️⃣ Start All Services

``` bash
docker compose up --build
```

This will start:

  Service               Purpose               Port
  --------------------- --------------------- --------
  **featureflag.api**   .NET 10 Web API       `8080`
  **postgres**          PostgreSQL database   `5432`
  **redis**             Redis cache           `6379`

------------------------------------------------------------------------

## 🌍 Access the API

    http://localhost:8080

------------------------------------------------------------------------

## 🛑 Stopping the System

``` bash
docker compose down
```

Reset database volume:

``` bash
docker compose down -v
```

------------------------------------------------------------------------

## 🧪 Running Tests

Tests do **not** require Docker.

``` bash
dotnet test
```

Testing stack:

-   **xUnit** -- Test framework\
-   **Moq** -- Mocking dependencies\
-   **FluentAssertions** -- Readable assertions
