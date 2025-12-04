---
trigger: model_decision
description: GitFlow-based branch naming conventions for TarkovLootScanner
---

# Branch Naming Rules (GitFlow + SemVer)

- **Base branches**
  - `main`: production-ready code, tagged releases only
  - `develop`: integration branch for ongoing development

- **Feature branches**
  - Format: `feature/<kebab-case-description>`
  - Base: branch **from `develop`**, merge **back into `develop`**
  - Examples:
    - `feature/ai-workflows-and-agents-docs`
    - `feature/loot-scanning-ui`
    - `feature/settings-window`

- **Bugfix branches (non-hotfix)**
  - Format: `bugfix/<kebab-case-description>`
  - Base: `develop`
  - Examples:
    - `bugfix/fix-loot-grid-sorting`
    - `bugfix/fix-window-position-save`

- **Release branches**
  - Format: `release/<semver>`
  - Base: branch **from `develop`**, merge into **`main` and `develop`**
  - Tag on `main` when done: `v<semver>`
  - Examples:
    - `release/0.2.0`
    - `release/0.3.0`

- **Hotfix branches**
  - Format: `hotfix/<semver-patch-or-desc>`
  - Base: branch from **`main`**, merge into **`main` and `develop`**
  - Examples:
    - `hotfix/0.1.1`
    - `hotfix/crash-on-startup`

- **Experiment / spike branches**
  - Format: `experiment/<kebab-case-description>` or `spike/<kebab-case-description>`
  - Base: usually `develop`

## Rules for Agents

- Prefer **short, descriptive, kebab-case** names.
- Always branch from:
  - `develop` for `feature/*`, `bugfix/*`, `release/*`.
  - `main` for `hotfix/*`.
- When proposing a branch, **explicitly state base branch and purpose**.