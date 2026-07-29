# Development Guide - Enterprise Clipboard Manager

This document provides developer guidelines, code conventions, testing instructions, and db schema migrations details.

## Project Structure
- `src/EnterpriseClipboard.Domain/` - Domain model and interfaces.
- `src/EnterpriseClipboard.Application/` - Application core and service implementations.
- `src/EnterpriseClipboard.Infrastructure/` - Low-level filesystem and encryption services.
- `src/EnterpriseClipboard.Persistence/` - Entity Framework Core SQLite DB context, initializers, and repositories.
- `src/EnterpriseClipboard.WindowsIntegration/` - Win32 message loop hooks, clipboard listeners, active process indicators, and keyboard simulators.
- `src/EnterpriseClipboard.App/` - WPF UI view layers and MVVM viewmodels.

---

## Coding Conventions
- **Clean Architecture**: Never reference Outer Layers (Persistence, Infrastructure, App) from Domain or Application.
- **Asynchronous Execution**: Always prefer asynchronous methods (`async`/`await`) for any database or disk operation.
- **Thread Safety (STA)**: Remember that Windows clipboard interactions (`System.Windows.Clipboard`) must run on single-threaded apartment (STA) threads. When accessing from thread pools or background tasks, spawn a new thread with `ApartmentState.STA`.

---

## Running Unit Tests
Execute the xUnit test suites from Powershell:
```powershell
dotnet test c:\DEVELOPMENT\Fabrica\EnterpriseClipboard\EnterpriseClipboard.sln
```
All tests are mocked and do not modify the developer's real Windows clipboard.
