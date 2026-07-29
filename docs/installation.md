# Installation Guide - Enterprise Clipboard Manager

This document provides installation, compiling, and enterprise packaging steps.

## System Prerequisites
- Windows 10 (Build 1809+) or Windows 11
- .NET 9.0 Runtime

---

## Build and Run Manual Steps

1. **Clone/Open solution directory:**
   `c:\DEVELOPMENT\Fabrica\EnterpriseClipboard\`
2. **Restore NuGet dependencies:**
   ```powershell
   dotnet restore EnterpriseClipboard.sln
   ```
3. **Build the solution:**
   ```powershell
   dotnet build EnterpriseClipboard.sln -c Release
   ```
4. **Run the application:**
   ```powershell
   dotnet run --project src/EnterpriseClipboard.App/EnterpriseClipboard.App.csproj
   ```

---

## Enterprise Deployment (MSIX Package)

To distribute the app across an enterprise environment:
1. Compile the self-contained package:
   ```powershell
   dotnet publish src/EnterpriseClipboard.App/EnterpriseClipboard.App.csproj `
     -c Release `
     -r win-x64 `
     --self-contained true `
     -p:PublishSingleFile=true
   ```
2. Wrap the output binary in an MSIX package using Windows App SDK package manifest template, signed with the corporate certificate.
