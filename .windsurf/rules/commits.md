---
trigger: model_decision
description: Conventional Commits + gitmoji for TarkovLootScanner using GitFlow and SemVer
---

# Commit Message Rules

## General format

Use **Conventional Commits 1.0.0** + **gitmoji**.

**Subject line:**

`<emoji> <type>(<scope>): <description>`

- `type`: required (feat, fix, chore, docs, …)
- `scope`: optional, lowercase noun (e.g. ui, scanner, release)
- `description`: imperative, short, no trailing period

Then:

- blank line
- optional body (bulleted)
- blank line
- optional footers

## Type reference and emoji

| Type     | Emoji | Description                                      | Typical scopes                    |
|----------|-------|--------------------------------------------------|-----------------------------------|
| feat     | ✨    | New features                                     | ui, scanner, settings            |
| fix      | 🐛    | Bug fixes                                        | auth, data, scanner              |
| docs     | 📝    | Documentation changes                            | readme, api, changelog           |
| chore    | 🔧    | Maintenance, config, tooling, infra, non-release | config, ci, tooling, cleanup     |
| build    | 🏗️    | Build system or dependencies                     | msbuild, nuget, pipeline         |
| ci       | 👷    | CI configuration or scripts                      | github-actions, pipelines        |
| refactor | ♻️    | Refactoring without behavior change              | core, models, services           |
| perf     | ⚡️   | Performance improvements                         | query, cache, scanner            |
| style    | 💄    | Formatting, styling only                         | formatting, xaml, naming         |
| test     | ✅    | Adding or modifying tests                        | unit, integration, ui            |
| revert   | ⏪️    | Reverting previous commits                       | any                              |
| i18n     | 🌐    | Localization / internationalization              | locale, translation              |

### Special emoji for releases / versions

Use **🔖 only for release / version related work**:

- Version bumps:
  - `🔖 chore(release): bump version to 0.1.1`
- Release prep:
  - `🔖 chore(release): prepare v0.2.0`
- Changelog for a specific version:
  - `🔖 chore(changelog): update changelog for v0.2.0`

Use **🔧 for general chores** (not tied to a specific version):

- `🔧 chore(config): adjust logging defaults`
- `🔧 chore(ci): update build pipeline cache`
- `🔧 chore(tooling): add windsurd workflows config`
- `🔧 chore(cleanup): remove unused experimental code`

Use **👷 for CI-specific commits**:

- `👷 ci(github-actions): add dotnet build workflow`
- `👷 ci(pipeline): cache nuget packages between runs`

## Description rules

- English only.
- Imperative mood (e.g. “add”, “fix”, “update”).
- No ending period.
- Keep line length ≤ 100 characters.

## Body

- Optional, separated from subject by a blank line.
- Use `-` bullet list.
- Explain **what** and **why**, not just “update code”.
- Keep lines ≤ 100 characters.

Example:

- `- add version helper to read assembly and informational version`
- `- update main window title to show version in debug and release`

## Footers

- Issue / PR references:
  - `Fixes #123`
  - `Closes #45`
  - `Related to #98`
- Breaking changes:
  - `BREAKING CHANGE: <description>`
- Other:
  - `Co-authored-by: Name <email>`
  - `Reviewed-by: Name <email>`

## Relation to GitFlow and SemVer

- **Branches**
  - `main`: production-ready, tagged with `v<semver>`.
  - `develop`: integration branch.
  - `feature/*`, `bugfix/*`: branch from and merge into `develop`.
  - `release/*`: branch from `develop`, merge into `main` + `develop`.
  - `hotfix/*`: branch from `main`, merge into `main` + `develop`.

- **Versions**
  - Use **SemVer**: `<major>.<minor>.<patch>`, e.g. `0.1.1`.
  - `0.x.y` is pre-1.0 / unstable.

## Rules for agents

- Always output:
  - emoji, type, optional scope, and description on the first line.
- Prefer small, focused commits.
- For version bumps / release metadata, prefer **🔖 chore(release)**.
- For general housekeeping, prefer **🔧 chore(<scope>)**.
- For CI-only changes, prefer **👷 ci(<scope>)**.