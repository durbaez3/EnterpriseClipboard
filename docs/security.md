# Security Documentation - Enterprise Clipboard Manager

This document details the security posture, threat model, encryption details, and sensitive data protection mechanisms.

## Threat Model & Design Security

Enterprise Clipboard Manager is designed exclusively for local-only, secure, enterprise-grade clipboard history management.

### Key Security Axioms
1. **Zero Network Connectivity**: The application has no internet communication, no remote analytics/telemetry, no silent update channels, and no Web API listeners. Everything is kept within local machine storage.
2. **No Security Evasion**: Does not attempt to bypass local Windows EDR/antivirus, policies, EULA, or registry policies.
3. **Protected Logs**: The technical Serilog rotative logs specifically exclude clipboard item texts, decrypted strings, passwords, paths, or tokens.

---

## DPAPI Encryption Details

Sensitive contents are protected using Windows **DPAPI (Data Protection API)** via the `System.Security.Cryptography.ProtectedData` package.

### Scope Selection
- **Current User (Default)**: Encryption keys are bound to the specific Windows User Account. Other users logging into the same OS cannot decrypt the database.
- **Machine (Optional)**: Bound to the machine context.

No static keys or plain-text secrets are stored in files or the database.

---

## Sensitive Data Scanning

On every clipboard event, the application scans the captured text using the active regular expression rules.

| Rule Name | Regex Pattern | Action |
| :--- | :--- | :--- |
| **API Keys / JWT** | `(eyJhbGciOi\|bearer\|api[_-]?key\|secret[_-]?key\|access[_-]?token\|auth[_-]?token)` | Encrypt |
| **Certificates** | `(-----BEGIN[A-Z ]*PRIVATE KEY-----\|-----BEGIN CERTIFICATE-----)` | Encrypt |
| **Credit Cards** | `\b(?:4[0-9]{12}(?:[0-9]{3})?\|[25][0-9]{15}\|6011[0-9]{12}\|3[47][0-9]{13})\b` | Encrypt |
| **Connection Strings** | `(User ID=\w+;Password=\w+\|Host=\w+;Database=\w+\|Server=\w+;Database=\w+)` | Encrypt |

### Actions
- **Encrypt**: Replaces the plain-text in the SQLite database with a protected byte array and shows `[CONTENIDO SENSIBLE CIFRADO]` in previews.
- **DoNotSave**: Immediately discards the clipboard transaction.
- **Expire**: Deletes the database record automatically after a short duration (e.g. 15 minutes).
