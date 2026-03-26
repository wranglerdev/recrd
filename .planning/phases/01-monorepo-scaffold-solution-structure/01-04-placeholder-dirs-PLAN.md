---
phase: 01-monorepo-scaffold-solution-structure
plan: 04
type: execute
wave: 2
depends_on:
  - 01-PLAN-solution-scaffold
files_modified:
  - plugins/.gitkeep
  - apps/vscode-extension/.gitkeep
autonomous: true
requirements: []
must_haves:
  truths:
    - "plugins/ directory exists at repo root with a .gitkeep file"
    - "apps/vscode-extension/ directory exists with a .gitkeep file"
    - "Neither directory contains any .csproj or .ts files (pure placeholders)"
  artifacts:
    - path: "plugins/.gitkeep"
      provides: "Placeholder for Phase 11 plugin examples"
      contains: ""
    - path: "apps/vscode-extension/.gitkeep"
      provides: "Placeholder for Phase 10 VS Code extension"
      contains: ""
  key_links:
    - from: "plugins/"
      to: "Phase 11 plugin system"
      via: "Directory existence enables plugin scaffold without restructuring"
      pattern: "ls plugins/ shows .gitkeep"
---

<objective>
Create the plugins/ and apps/vscode-extension/ placeholder directories so the repo matches the documented monorepo structure from day one. Both are empty stubs — no code, just .gitkeep files.

Purpose: The CLAUDE.md and PROJECT.md document these directories as part of the monorepo structure. Without them, the repo deviates from its own documentation. The initial scaffold (commit d0b19f7) omitted these — this plan corrects that.
Output: plugins/.gitkeep, apps/vscode-extension/.gitkeep
</objective>

<execution_context>
@/home/gil/dev/recrd/.claude/get-shit-done/workflows/execute-plan.md
@/home/gil/dev/recrd/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@/home/gil/dev/recrd/CLAUDE.md
@/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create plugins/ and apps/vscode-extension/ placeholder directories</name>
  <files>plugins/.gitkeep, apps/vscode-extension/.gitkeep</files>
  <read_first>
    - /home/gil/dev/recrd/CLAUDE.md (confirms plugins/ and apps/vscode-extension/ are part of the documented structure)
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Open Questions section — confirms empty .gitkeep only, no package.json or plugin stubs in Phase 1)
  </read_first>
  <action>
Create /home/gil/dev/recrd/plugins/.gitkeep as an empty file.

Create /home/gil/dev/recrd/apps/vscode-extension/.gitkeep as an empty file.

The apps/ directory should already exist from Plan 03 (recrd-cli was created there). If Plan 03 ran first, just create the vscode-extension subdirectory. If this plan runs before Plan 03, create apps/ as well — it is safe either way.

Do NOT create:
- plugins/Recrd.Plugin.Example/ (deferred to Phase 11/12)
- apps/vscode-extension/package.json (deferred to Phase 10)
- apps/vscode-extension/src/ (deferred to Phase 10)

These directories are intentionally empty. The .gitkeep files ensure git tracks the directories.
  </action>
  <verify>
    <automated>ls /home/gil/dev/recrd/plugins/.gitkeep && ls /home/gil/dev/recrd/apps/vscode-extension/.gitkeep</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/plugins/.gitkeep exists (file, not directory)
    - /home/gil/dev/recrd/apps/vscode-extension/.gitkeep exists (file, not directory)
    - `ls /home/gil/dev/recrd/plugins/` shows only `.gitkeep` (no other files)
    - `ls /home/gil/dev/recrd/apps/vscode-extension/` shows only `.gitkeep` (no other files)
    - `git status` shows plugins/.gitkeep and apps/vscode-extension/.gitkeep as new untracked files
  </acceptance_criteria>
  <done>Both placeholder directories exist with only .gitkeep files. No implementation stubs added.</done>
</task>

</tasks>

<verification>
1. `ls /home/gil/dev/recrd/plugins/` — shows .gitkeep
2. `ls /home/gil/dev/recrd/apps/vscode-extension/` — shows .gitkeep
3. No .csproj, .ts, or package.json files in either directory
</verification>

<success_criteria>
- plugins/.gitkeep exists (empty file)
- apps/vscode-extension/.gitkeep exists (empty file)
- No implementation files in either directory
</success_criteria>

<output>
After completion, create `/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-04-SUMMARY.md`
</output>
