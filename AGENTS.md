# AGENTS for TarkovLootScanner

Guidelines for AI agents (and humans) working on this repository.

---

## 1. Project Overview

- **Name**: TarkovLootScanner
- **Type**: WPF desktop application (.NET)
- **Target framework**: `net10.0-windows`
- **Versioning**: Semantic Versioning (SemVer)
  - Project version is defined in the main `.csproj` file via the `<Version>` property.
  - `0.x.y` (pre-1.0) is considered unstable and the API may change.

---

## 2. Branching Strategy (GitFlow)

- **Main branches**
  - `main`
    - Production-ready code only.
    - Each release on `main` should be tagged: `v<semver>` (e.g. `v0.1.0`).
  - `develop`
    - Integration branch for ongoing development.
    - Most work should start from and merge back into `develop`.

- **Feature branches**
  - Format: `feature/<kebab-case-description>`
  - Base: `develop`
  - Merge into: `develop`
  - Examples:
    - `feature/ai-workflows-and-agents-docs`
    - `feature/loot-scanning-ui`

- **Bugfix branches (non-hotfix)**
  - Format: `bugfix/<kebab-case-description>`
  - Base: `develop`
  - Merge into: `develop`

- **Release branches**
  - Format: `release/<semver>`
  - Base: `develop`
  - Merge into: `main` and `develop`
  - After merge to `main`, create tag: `v<semver>`

- **Hotfix branches**
  - Format: `hotfix/<semver-or-desc>`
  - Base: `main`
  - Merge into: `main` and `develop`

- **Agent rule**
  - When proposing a new branch, always:
    - Specify **type** (`feature/`, `bugfix/`, `release/`, `hotfix/`).
    - Specify **base branch** (`develop` or `main`).
    - Use **short, descriptive, kebab-case** names.

---

## 3. Versioning Rules

- Project versions are managed in the main `.csproj` file:

  ```xml
  <Version>...</Version>
  <AssemblyVersion>$(Version)</AssemblyVersion>
  <FileVersion>$(Version)</FileVersion>
  ```

- **Single source of truth**: `<Version>` is the canonical SemVer value.
- `AssemblyVersion` and `FileVersion` must follow `<Version>`.

- **Bumping versions**
  - Only bump when there is a meaningful change (code or behavior).
  - Use **🔖** in the commit for release-related changes (see below).
  - Tag releases on `main`: `v<version>` (e.g. `v0.1.0`).

---

## 4. Commit Conventions (Conventional Commits + gitmoji)

### 4.1 Subject format

Use:

`<emoji> <type>(<scope>): <description>`

- **type**: one of the conventional types below.
- **scope**: optional, lowercase noun (`ui`, `scanner`, `release`, `config`, `docs`, etc.).
- **description**: imperative, short, no trailing period.

Examples:

- `✨ feat(scanner): add live loot grid updates`
- `🔧 chore(config): adjust logging defaults`
- `🔖 chore(release): bump version to <new-version>`

### 4.2 Types and emoji

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

### 4.3 Special emoji for releases / versions

- Use **🔖 only for release/version-related work**:
  - `🔖 chore(release): bump version to <new-version>`
  - `🔖 chore(release): prepare <version> release`
  - `🔖 chore(changelog): update changelog for <version>`

- Use **🔧 for general chores** (not tied to a specific version):
  - `🔧 chore(config): adjust logging defaults`
  - `🔧 chore(ci): update build pipeline cache`
  - `🔧 chore(tooling): add windsurd workflows config`
  - `🔧 chore(cleanup): remove unused experimental code`

- Use **👷 for CI-specific changes**:
  - `👷 ci(github-actions): add dotnet build workflow`
  - `👷 ci(pipeline): cache nuget packages between runs`

### 4.4 Body and footers

- **Body** (optional)
  - Start after a blank line.
  - Use `-` bullet list.
  - Explain **what** and **why**.
  - Keep lines ≤ 100 chars.

- **Footers** (optional)
  - Issues / PRs:
    - `Fixes #123`
    - `Closes #45`
    - `Related to #98`
  - Breaking changes:
    - `BREAKING CHANGE: <description>`
  - Others:
    - `Co-authored-by: Name <email>`
    - `Reviewed-by: Name <email>`

---

## 5. Agent Behavior Guidelines

- Prefer **small, focused commits** with clear intent.
- Follow branch naming rules and propose the correct **base branch**.
- Keep project-wide conventions in sync with:
  - `.windsurf/rules/branch-naming.md`
  - `.windsurf/rules/commits.md`
- When changing versioning behavior or GitFlow process, update:
  - `AGENTS.md`
  - relevant `.windsurf/rules/*.md` files.
