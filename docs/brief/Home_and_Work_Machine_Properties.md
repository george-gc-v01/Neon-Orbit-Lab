# Home and Work Machine Properties

Copyright © 2026 George Culache (`george-gc-v01`).

## Purpose

This file records only the machine properties relevant to developing, building and testing Neon Orbit. Values not established by the supplied PowerShell outputs are marked as unknown or pending rather than inferred.

## Comparison

| Property | Home machine | Work machine |
|---|---|---|
| Operating system | Windows; exact edition/version not yet checked | Windows 11 Pro, version 25H2 |
| Windows build | Not yet checked | 26200.9106 |
| Architecture | x64, confirmed by the current browser environment | x64 target appropriate; PowerShell architecture field returned blank |
| PowerShell | Windows PowerShell; exact version not recorded | Windows PowerShell 5.1.26100.8875 |
| `dotnet.exe` location | `C:\Program Files\dotnet\dotnet.exe` | `C:\Program Files\dotnet\dotnet.exe` |
| .NET SDK | Microsoft .NET SDK 10.0.401 installed | No SDK installed |
| .NET 10 SDK installation | Completed and verified | Not installed |
| .NET desktop runtime | Not enumerated | Microsoft.WindowsDesktop.App 10.0.11 installed |
| Other .NET runtimes | Not enumerated | .NET Core/Desktop 3.1, 6, 8, 9 and 10 runtimes present; ASP.NET Core 6 and 8 present |
| Git for Windows | 2.55.0.windows.5 | Not installed or not available in `PATH` |
| WinGet | 1.29.290 | 1.29.290 |
| Monitor DPI/scaling | Not checked | Registry test returned no useful value; application must query per-monitor DPI dynamically |

## Development Readiness

### Home machine

- Git and WinGet are ready.
- Microsoft .NET SDK 10.0.401 is installed and verified.
- The home machine is suitable for local source builds and testing.

### Work machine

- The installed .NET 10 Desktop Runtime can run an appropriate framework-dependent build.
- A self-contained Neon Orbit release will not require any separately installed .NET runtime.
- Local source compilation is not currently available because no .NET SDK is installed.
- Git command-line development is not currently available because Git was not found.
- No work-machine installations are required merely to run a self-contained approved release.

## Most Important Distinction

- A **.NET Runtime** runs an already-built application.
- A **.NET SDK** compiles and develops the application.
- **Git** manages local source history and GitHub synchronisation; it is not needed by an end user installing the finished application.

## Home-Machine SDK Confirmation

The verification command returned:

```powershell
10.0.401 [C:\Program Files\dotnet\sdk]
```

No further .NET installation is currently required on the home machine.

## Agreed Build Implications

- Development and signed/unsigned test builds will be produced on the home machine or a Windows GitHub Actions runner.
- End users will receive a self-contained x64 package and will not need Git or the .NET SDK.
- The work machine should be used only in accordance with workplace software and security policies.
- Windows Awake will use documented Windows APIs and default to On in the application; display-awake behaviour will default to Off.
- Runtime verification must include Windows power requests, lock/sleep handling, mixed-DPI displays and low-resource Neon low behaviour.

## Repository and Rights

- Repository: `Neon-Orbit-Lab`, private during initial development.
- Licence: GNU General Public License v3.0.
- Source headers and README: `Copyright © 2026 George Culache (george-gc-v01)`.
- Branding reservation: `The Neon Orbit name and logo are not licensed for use in derivative products.`

