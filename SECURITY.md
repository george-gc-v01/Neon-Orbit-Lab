# Security and privacy

Report security concerns privately to the repository owner using GitHub private vulnerability reporting if enabled, or an existing private communication channel. Avoid including credentials or personal logs in public issues.

The app runs as the current user, offline, with no startup registration, service or updater. It uses documented Windows APIs and exposes a stable executable identity. It generates mouse movement only. The observer processes pointer events transiently; it does not capture keyboard contents, screenshots, window titles or personal cursor paths.

Local logs live under `%LOCALAPPDATA%\NeonOrbit`. Logging is optional and enabled by default. Disabling it stops further writes and clears the in-memory report data; it does not silently erase earlier files. Review logs before sharing.

Release reviewers should verify dependency licences, scan for secrets, review native handle cleanup and test input priority. Do not commit signing keys or private certificates. Initial beta installers are unsigned.
