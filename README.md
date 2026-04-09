# SportsStore - Distributed Order Processing Platform

## Overview

This project extends the original SportsStore MVC application into a full distributed order processing platform using .NET 10, RabbitMQ, Blazor, and React.

## Architecture

```
Customer (Blazor Portal)  Admin (React Dashboard)
         │                         │
         └──────────┬──────────────┘
                    │
          Order Management API (.NET 10)
          ┌─────────┴──────────┐
          │   CQRS / MediatR   │
          │   AutoMapper       │
          │   SQLite / EF Core │
          └─────────┬──────────┘
                    │ RabbitMQ
          ┌─────────┼──────────────────┐
          │         │                  │
   Inventory    Payment Service   Shipping Service
   Service      (Worker Service)  (Worker Service)
   (Worker)
```

### Event Flow

```
POST /api/orders/checkout
        │
        ▼
Order saved (Status: Submitted)
        │
        ▼ [RabbitMQ: order.submitted]
        │
Inventory Service
  - Checks stock for each item
  - Reserves stock if available
        │
        ▼ [RabbitMQ: inventory.check.completed]
        │
Order API updates status
  ✓ InventoryConfirmed → publishes payment.processing.requested
  ✗ InventoryFailed   → publishes order.failed → Status: Failed
        │
        ▼ [RabbitMQ: payment.processing.requested]
        │
Payment Service
  - Simulates payment authorization
  - 10% random rejection rate
  - "customer-rejected" always fails
        │
        ▼ [RabbitMQ: payment.processed]
        │
Order API updates status
  ✓ PaymentApproved → publishes shipping.requested
  ✗ PaymentFailed   → publishes order.failed → Status: Failed
        │
        ▼ [RabbitMQ: shipping.requested]
        │
Shipping Service
  - Generates tracking number
  - Estimates dispatch date (2-5 days)
  - 5% random failure rate
        │
        ▼ [RabbitMQ: shipping.created / shipping.failed]
        │
Order API → Status: Completed / Failed
```

## Projects

| Project | Type | Port | Description |
|---------|------|------|-------------|
| `SportsStore` | ASP.NET MVC | 5000 | Original shopping app |
| `Shared.Contracts` | Class Library | — | RabbitMQ event contracts |
| `OrderManagement.API` | Web API | 5001 | Central order API (CQRS/MediatR) |
| `InventoryService` | Worker Service | — | Stock validation consumer |
| `PaymentService` | Worker Service | — | Payment simulation consumer |
| `ShippingService` | Worker Service | — | Shipment creation consumer |
| `CustomerPortal.Blazor` | Blazor Server | 5002 | Customer-facing UI |
| `AdminDashboard.React` | React + TS | 3000 | Admin operations UI |
| `Integration.Tests` | xUnit | — | API integration tests |
| `SportsStore.Tests` | xUnit | — | Original unit tests |

## Order State Machine

```
Cart → Submitted → InventoryPending → InventoryConfirmed → PaymentPending
                                    ↘ InventoryFailed → Failed
                                                       → PaymentApproved → ShippingPending → ShippingCreated → Completed
                                                       ↘ PaymentFailed → Failed
                                                                                            ↘ ShippingFailed → Failed
```

## Architecture Patterns

### CQRS with MediatR
All business logic flows through MediatR handlers:

**Commands** (write operations):
- `CheckoutOrderCommand` — creates order and publishes event
- `ProcessInventoryResultCommand` — handles inventory outcome
- `ProcessPaymentResultCommand` — handles payment outcome
- `CreateShipmentCommand` — records shipment, completes order
- `CancelOrderCommand` — cancels a pending order
- `FailOrderCommand` — marks order as failed

**Queries** (read operations):
- `GetOrderByIdQuery`
- `GetOrdersQuery`
- `GetCustomerOrdersQuery`
- `GetOrdersByStatusQuery`
- `GetOrderStatusQuery`
- `GetProductsQuery`
- `GetDashboardSummaryQuery`

### AutoMapper
Mapping profiles in `OrderManagement.API/Mappings/OrderMappingProfile.cs`:
- `Order` → `OrderDto`
- `OrderItem` → `OrderItemDto`
- `InventoryRecord` → `InventoryRecordDto`
- `PaymentRecord` → `PaymentRecordDto`
- `ShipmentRecord` → `ShipmentRecordDto`
- `Product` → `ProductDto`

### Serilog Structured Logging
All services log with structured context including `OrderId`, `CorrelationId`, `ServiceName`. Logs are written to console and rolling daily files in `logs/`.

## How to Run

### Option 1: Docker Compose (recommended)

```bash
docker compose up --build
```

Services available:
- Customer Portal: http://localhost:5002
- Admin Dashboard: http://localhost:3000
- Order API + Swagger: http://localhost:5001/swagger
- RabbitMQ Management: http://localhost:15672 (guest/guest)

### Option 2: Run locally

**Prerequisites:** .NET 10 SDK, Node.js 20+, RabbitMQ running on localhost:5672

**1. Start RabbitMQ:**
```bash
docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

**2. Start Order Management API:**
```bash
cd OrderManagement.API
dotnet run
# Swagger at http://localhost:5001/swagger
```

**3. Start microservices (each in a separate terminal):**
```bash
cd InventoryService && dotnet run
cd PaymentService && dotnet run
cd ShippingService && dotnet run
```

**4. Start Customer Portal:**
```bash
cd CustomerPortal.Blazor
dotnet run
# Available at http://localhost:5002
```

**5. Start Admin Dashboard:**
```bash
cd AdminDashboard.React
npm install
npm start
# Available at http://localhost:3000
```

### Running Tests

```bash
# All tests with coverage
dotnet test SportsSln.sln --collect:"XPlat Code Coverage"

# Integration tests only
dotnet test Integration.Tests/Integration.Tests.csproj

# Original unit tests only
dotnet test SportsStore.Tests/SportsStore.Tests.csproj
```

## API Reference

### Orders
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/orders/checkout` | Submit a new order |
| GET | `/api/orders` | List all orders (paginated) |
| GET | `/api/orders/{id}` | Get order by ID |
| GET | `/api/orders/{id}/status` | Get order status |
| GET | `/api/orders/status/{status}` | Filter orders by status |
| DELETE | `/api/orders/{id}` | Cancel an order |

### Products & Customers
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | List all products |
| GET | `/api/customers/{id}/orders` | Get customer's orders |
| GET | `/api/dashboard/summary` | Admin dashboard summary |

## Database

Using **SQLite** for portability (easily swappable to SQL Server or PostgreSQL via the connection string).

**Entities:** `Orders`, `OrderItems`, `Products`, `Customers`, `InventoryRecords`, `PaymentRecords`, `ShipmentRecords`

Database is auto-created on first run via `EnsureCreated()`. Products are seeded from the original SportsStore catalogue.

## Testing Payment Scenarios

- **Normal customer** (`customer-1`): ~90% approval rate
- **Rejected customer** (`customer-rejected`): always declined
- **Inventory failure**: request more than available stock (e.g. 999 Stadiums)

## Assumptions and Limitations

- SQLite is used instead of SQL Server for portability in the new services
- Inventory stock is held in-memory in the InventoryService (resets on restart)
- Payment processing is simulated (no real payment gateway)
- The original `SportsStore` project is preserved unchanged alongside the new platform
- RabbitMQ connection retries up to 10 times with 3-second intervals on startup
- No authentication on the new Order API (add JWT bearer for production)
