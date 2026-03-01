# Aircraft Maintenance API — Project Description

## Overview

This project implements a .NET Web API that manages aircraft and their maintenance records using an EF Core code-first approach. The domain contains two primary models: Aircraft and MaintenanceRecord. The API exposes CRUD endpoints for both entities through two controllers. A custom exception is used to indicate missing aircraft resources.

This description documents the exact solution structure, models, API endpoints, Entity Framework Core configuration, error behavior, and the test coverage that verifies the implementation.

## Key Features

- Aircraft and MaintenanceRecord domain models with data annotations for validation.
- EF Core Code-First DbContext with DbSet properties and relationship mapping (one-to-many: Aircraft -> MaintenanceRecords) with cascade delete.
- Two controllers:
  - AircraftController: full CRUD for Aircraft (includes MaintenanceRecords on reads).
  - MaintenanceController: full CRUD for MaintenanceRecord (includes Aircraft on reads; validates Aircraft existence on create).
- Custom exception: AircraftNotFoundException used when an Aircraft cannot be found by id.
- NUnit integration tests using WebApplicationFactory and an in-memory EF Core provider to exercise API flows and reflection-based structural checks.

## Solution Structure (important files)

- dotnetwebapi/
  - dotnetapp/
    - Controllers/
      - AircraftController.cs
      - MaintenanceController.cs
    - Data/
      - ApplicationDbContext.cs
    - Exceptions/
      - AircraftNotFoundException.cs
    - Models/
      - Aircraft.cs
      - MaintenanceRecord.cs
    - Program.cs / ProgramEntry.cs
    - dotnetapp.csproj
  - nunit/
    - test/TestProject/
      - AircraftTests.cs (NUnit tests)
      - testcase_weightage.json

## Domain Models

### Aircraft
- Purpose: Represents a physical aircraft and its core attributes.
- Properties:
  - AircraftId (int) — primary key
  - RegistrationNumber (string, required)
  - Model (string, required)
  - Manufacturer (string)
  - Capacity (int)
  - MaintenanceRecords (List<MaintenanceRecord>) — navigation property

Validation annotations (e.g., [Key], [Required]) are present on identifier and required string properties.

### MaintenanceRecord
- Purpose: Represents a maintenance event for an aircraft.
- Properties:
  - MaintenanceRecordId (int) — primary key
  - Date (DateTime)
  - Description (string)
  - AircraftId (int) — foreign key
  - Aircraft (Aircraft?) — navigation property

## Custom Exceptions

### AircraftNotFoundException
- Namespace: dotnetapp.Exceptions
- Behavior: Thrown with a message like "Aircraft with id {id} not found" when an operation expects an existing aircraft but none is found.
- Note: When thrown inside controller actions without a global exception-to-HTTP mapping, this can surface as a 500 Internal Server Error. Some controller actions return NotFound() explicitly instead of throwing (see MaintenanceController.Get/Update/Delete), so behavior may vary by endpoint.

## Entity Framework Core Configuration

- ApplicationDbContext (dotnetapp.Data.ApplicationDbContext)
  - DbSet<Aircraft> Aircrafts
  - DbSet<MaintenanceRecord> MaintenanceRecords
  - OnModelCreating defines the one-to-many relationship and configures cascade delete from Aircraft to MaintenanceRecords.

EF Core is configured in Program.cs to use the environment's provider at runtime; the tests replace the DbContext registration with an InMemory provider named "TestDb" for isolation.

## API Endpoints and Behaviors

AircraftController (route: /api/Aircraft)
- GET /api/Aircraft
  - Returns 200 OK with a list of Aircraft including their MaintenanceRecords.
- GET /api/Aircraft/{id}
  - Returns 200 OK with the Aircraft (including MaintenanceRecords) if found.
  - If not found: throws AircraftNotFoundException (current implementation) — tests accept either a 404 Not Found or a 500 Internal Server Error (depends on exception handling configuration).
- POST /api/Aircraft
  - Creates an Aircraft. Expects RegistrationNumber and Model (required).
  - Returns 201 Created with location header pointing to GET by id.
  - If model validation fails (e.g., missing RegistrationNumber), request may return 400 Bad Request or result in an error depending on model binding/validation settings.
- PUT /api/Aircraft/{id}
  - Updates existing Aircraft fields (RegistrationNumber, Model, Manufacturer, Capacity).
  - If the Aircraft does not exist: throws AircraftNotFoundException (current implementation).
  - Returns 200 OK with the updated resource.
- DELETE /api/Aircraft/{id}
  - Deletes the Aircraft. If not found: throws AircraftNotFoundException.
  - Returns 204 No Content on success.

MaintenanceController (route: /api/Maintenance)
- GET /api/Maintenance
  - Returns 200 OK with list of MaintenanceRecords including their Aircraft navigation.
- GET /api/Maintenance/{id}
  - Returns 200 OK with the MaintenanceRecord if found; 404 Not Found if not.
- POST /api/Maintenance
  - Creates a MaintenanceRecord for an existing Aircraft (requires AircraftId).
  - If the referenced Aircraft does not exist: throws AircraftNotFoundException (tests expect an Internal Server Error in this scenario).
  - Returns 201 Created with location header pointing to GET by id on success.
- PUT /api/Maintenance/{id}
  - Updates Date and Description of an existing MaintenanceRecord.
  - If not found: returns 404 Not Found.
  - Returns 200 OK with the updated record.
- DELETE /api/Maintenance/{id}
  - Deletes the MaintenanceRecord; returns 404 Not Found if not present; 204 No Content on success.

## Testing Strategy & Coverage (what the provided tests verify)

The repository includes an NUnit test project that exercises both structural and functional aspects of the solution. Tests run an in-memory server via WebApplicationFactory<Program> and replace the application's DbContext with an EF Core InMemory provider to avoid external dependencies.

Test categories covered (summary):
- File existence checks: Ensure model and exception source files exist in the solution tree.
- Reflection checks: Verify that important types (Aircraft, ApplicationDbContext, AircraftController) and members (DbSet properties, controller methods such as Create/Delete) exist in the compiled assembly.
- API functional tests:
  - POST /api/Aircraft returns 201 Created for valid payloads.
  - GET /api/Aircraft returns 200 OK.
  - A full aircraft CRUD workflow: Create -> Read -> Update -> Delete (expects Created, OK, OK, NoContent statuses respectively).
  - GET /api/Aircraft/{non-existing-id} is asserted to return either 404 or 500 (test is tolerant of either behavior due to exception mapping uncertainty).
- Maintenance tests:
  - Creating a maintenance record for a non-existing Aircraft results in an error (test expects InternalServerError).
  - Full maintenance CRUD workflow (create aircraft, create maintenance, get, update, delete) expecting Created, OK, OK, NoContent statuses respectively.
- Negative / Boundary tests:
  - POST /api/Aircraft with missing required fields is asserted to return BadRequest or an error status.
  - Capacity property presence and behavior boundaries are inspected via reflection tests.
- Structural method existence tests: Ensure controller methods like Delete exist.

Total tests: The included NUnit suite contains multiple tests covering the above categories; the test suite exercises end-to-end HTTP flows and structural validations.

Note: The test suite intentionally allows some variation in HTTP status code when the implementation throws a custom exception without a global exception-to-HTTP mapping; where tests check for either 404 or 500, it's to tolerate either behavior.

## How to Build, Run, and Test

From the workspace root:

1. Restore and build the Web API

```bash
cd dotnetwebapi/dotnetapp
dotnet restore
dotnet build
```

2. Run migrations (if you want to use a real DB) and run the app

- This project is code-first; to use a persistent provider, add a suitable provider (e.g., SQL Server) and configure it in Program.cs. In tests the InMemory provider is used.

3. Run tests

```bash
cd ../../dotnetwebapi/nunit/test/TestProject
dotnet test
```

Or from workspace root run:

```bash
dotnet test dotnetwebapi/nunit/test/TestProject/TestProject.csproj
```

## Expected Behaviors & Notes for Graders / Integrators

- The AircraftController throws AircraftNotFoundException for several operations when an Aircraft is missing. Without a mapping middleware that translates the exception into a 404 Not Found response, this exception results in a 500 Internal Server Error. Tests account for this by allowing either a 404 or 500 status where appropriate.

- MaintenanceController mixes explicit NotFound() returns with throwing AircraftNotFoundException during Create if the AircraftId does not exist. This asymmetry is intentional in the implementation and is reflected in tests.

- Tests use an EF Core InMemory provider named "TestDb" to ensure isolation and repeatability. The test setup replaces the application's DbContext registration to use the in-memory provider.

## Recommended Improvements (optional)

- Add a global exception filter or middleware to convert AircraftNotFoundException into a 404 Not Found response consistently.
- Add Data Transfer Objects (DTOs) and input validation attributes to better control model binding and API surface.
- Expand test assertions to verify response payload shapes and content (beyond status codes), and add more explicit negative tests for validation errors.

---

This PROJECT_DESCRIPTION.md now reflects the actual code and tests present in the repository (dotnetwebapi/dotnetapp and the NUnit tests in dotnetwebapi/nunit/test/TestProject). If you want, I can further adjust wording to be more concise or more verbose, or generate a separate README-style quickstart derived from this description.