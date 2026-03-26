---
phase: 01-monorepo-scaffold-solution-structure
plan: 07
type: execute
wave: 4
depends_on:
  - 01-PLAN-test-projects
files_modified:
  - .editorconfig
  - .config/dotnet-tools.json
autonomous: true
requirements: []
must_haves:
  truths:
    - ".editorconfig exists at repo root with C# formatting rules"
    - ".config/dotnet-tools.json exists and declares dotnet-format as a local tool"
    - "dotnet format --verify-no-changes exits 0 on the current codebase (all stubs comply)"
    - "dotnet restore --tool-manifest succeeds"
  artifacts:
    - path: ".editorconfig"
      provides: "Editor and formatter configuration for C# code style"
      contains: "[*.cs]"
    - path: ".config/dotnet-tools.json"
      provides: "Local dotnet tool manifest for CI reproducibility"
      contains: "dotnet-format"
  key_links:
    - from: ".editorconfig"
      to: "dotnet format"
      via: "dotnet format reads .editorconfig for C# style rules"
      pattern: "dotnet format --verify-no-changes exits 0"
    - from: ".config/dotnet-tools.json"
      to: ".github/workflows/ci.yml"
      via: "CI runs dotnet format which resolves via tool manifest"
      pattern: "dotnet format --verify-no-changes"
---

<objective>
Create the .editorconfig and .config/dotnet-tools.json files that enforce consistent code style across the project and in CI. dotnet format --verify-no-changes must pass on all existing stubs.

Purpose: The CI workflow (Plan 06) runs dotnet format --verify-no-changes. Without a .editorconfig, the formatter uses defaults that may differ between developer machines. Establishing these now means all future code is formatted consistently from day one.
Output: .editorconfig, .config/dotnet-tools.json
</objective>

<execution_context>
@/home/gil/dev/recrd/.claude/get-shit-done/workflows/execute-plan.md
@/home/gil/dev/recrd/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md
@/home/gil/dev/recrd/Directory.Build.props
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create .editorconfig with C# formatting rules</name>
  <files>.editorconfig</files>
  <read_first>
    - /home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-RESEARCH.md (Pitfall 6 — dotnet format may fail on empty projects; all projects now have at least one .cs file)
    - /home/gil/dev/recrd/packages/Recrd.Core/Placeholder.cs (representative .cs file — format rules must accept this stub)
  </read_first>
  <action>
Create /home/gil/dev/recrd/.editorconfig with these C# formatting rules. These are the minimal settings that align with .NET conventions and dotnet format defaults:

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_style = space
indent_size = 4
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Naming conventions
dotnet_naming_rule.interface_should_be_begins_with_i.severity = warning
dotnet_naming_rule.interface_should_be_begins_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_begins_with_i.style = begins_with_i

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected

dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case

# Whitespace preferences
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_method_call_parameter_list_parentheses = false

[*.{csproj,props,targets}]
indent_style = space
indent_size = 2

[*.json]
indent_style = space
indent_size = 2

[*.yml]
indent_style = space
indent_size = 2
```
  </action>
  <verify>
    <automated>grep -c "root = true" /home/gil/dev/recrd/.editorconfig && grep -c "\[*.cs\]" /home/gil/dev/recrd/.editorconfig</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/.editorconfig exists
    - .editorconfig contains `root = true`
    - .editorconfig contains `[*.cs]` section
    - .editorconfig contains `indent_size = 4` under [*.cs]
    - .editorconfig contains `insert_final_newline = true`
    - .editorconfig contains `[*.{csproj,props,targets}]` section
  </acceptance_criteria>
  <done>.editorconfig created at repo root with C# formatting rules.</done>
</task>

<task type="auto">
  <name>Task 2: Create dotnet-tools.json manifest and verify dotnet format passes</name>
  <files>.config/dotnet-tools.json</files>
  <read_first>
    - /home/gil/dev/recrd/.editorconfig (confirm Task 1 complete before running format verification)
    - /home/gil/dev/recrd/recrd.sln (dotnet format operates on this solution)
  </read_first>
  <action>
Create /home/gil/dev/recrd/.config/dotnet-tools.json. This manifest records local dotnet tools so CI can restore them reproducibly. In Phase 1, the relevant tool is dotnet-format (built into SDK 10 — no explicit tool install needed), but the manifest structure is established now for Phase 5 which will add Stryker.NET:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {}
}
```

NOTE: `dotnet format` is built into the .NET 10 SDK and does not need to be declared as a local tool. The manifest is created empty (tools: {}) so Phase 5 can add Stryker.NET as: `dotnet tool install dotnet-stryker` without creating the manifest from scratch.

After creating the manifest, run dotnet format to verify the current codebase passes:

```bash
cd /home/gil/dev/recrd && dotnet format --verify-no-changes
```

If dotnet format reports formatting violations in any of the placeholder .cs files, fix them (the placeholder files should be trivial to format correctly — they're single-line comments and namespace declarations). The goal is that `dotnet format --verify-no-changes` exits 0.

If dotnet format rewrites any file, commit the reformatted version — do NOT suppress the formatter. The .editorconfig rules must be satisfied by all source files.
  </action>
  <verify>
    <automated>cd /home/gil/dev/recrd && dotnet format --verify-no-changes 2>&1; echo "Exit: $?"</automated>
  </verify>
  <acceptance_criteria>
    - /home/gil/dev/recrd/.config/dotnet-tools.json exists and is valid JSON
    - .config/dotnet-tools.json contains `"version": 1` and `"isRoot": true`
    - `dotnet format --verify-no-changes` exits 0 (no formatting violations in any .cs file)
    - If dotnet format made changes, those changes are part of this task's commit (no unformatted code remains)
  </acceptance_criteria>
  <done>dotnet-tools.json manifest created. dotnet format --verify-no-changes exits 0 on the full solution.</done>
</task>

</tasks>

<verification>
After all tasks:
1. `grep "root = true" /home/gil/dev/recrd/.editorconfig` exits 0
2. `dotnet format --verify-no-changes` exits 0 (run from /home/gil/dev/recrd)
3. `cat /home/gil/dev/recrd/.config/dotnet-tools.json` is valid JSON with version=1 and isRoot=true
</verification>

<success_criteria>
- .editorconfig exists with C# indent_size=4 and insert_final_newline=true
- .config/dotnet-tools.json exists (empty tools, ready for Phase 5 Stryker addition)
- dotnet format --verify-no-changes exits 0 on full solution
</success_criteria>

<output>
After completion, create `/home/gil/dev/recrd/.planning/phases/01-monorepo-scaffold-solution-structure/01-07-SUMMARY.md`
</output>
