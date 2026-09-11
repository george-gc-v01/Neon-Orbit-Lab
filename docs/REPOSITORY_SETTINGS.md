# Repository administration checklist

The repository is private. The workflow uses read-only repository permissions, pinned action commits, no deployment step and 14-day private build artifacts. Dependabot configuration and issue templates are committed.

Settings requiring account-level review in GitHub:

- Keep repository visibility private until explicitly approved.
- Enable secret scanning / push protection and private vulnerability reporting where the account plan supports them.
- Add a main-branch ruleset requiring the Windows build job before merging future pull requests; availability varies for private personal repositories.
- Keep signing certificates in an appropriate secret store; no signing certificate has been configured for this beta.
- Do not treat a successful CI run as completion of the manual Windows acceptance checklist.

These admin settings are recommendations, not claims that they were changed automatically. Public releases and visibility changes require the owner's explicit instruction.
