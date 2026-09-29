# CLAUDE.md

Floating Clock: a minimal, chromeless, always-on-top stopwatch for Windows that shows how long
I have been working. Stack, commands and architecture are decided in the first grilling session
and added here afterwards.

## Workflow

- New feature: `grilling` → `spec` → implement → spec-check → `/ponytail-review` → `/code-review`
  → commit. Bug fixes and small, fully specified changes skip grilling and spec.
- Decisions that are hard to reverse, surprising, and a real trade-off go to `docs/adr/`
  (rules and format: the ADR section of `.claude/skills/grilling/SKILL.md`).
- All files (code, comments, ADRs, specs) are English.
