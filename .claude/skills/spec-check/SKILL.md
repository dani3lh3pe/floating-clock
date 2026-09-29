---
name: spec-check
description: Reviews the current change against its spec in .scratch/FEATURE/spec.md in a fresh context and reports what is missing, what was implemented wrong, and what was built without being asked for (scope creep). Report only, changes nothing. Use before committing a feature that has a spec, and whenever the user asks "haben wir alles aus der Spec umgesetzt?", "prüf den Diff gegen die Spec", "ist da Scope Creep drin?". Do not use for a correctness or bug review, that is /code-review, or for an over-engineering review, that is /ponytail-review.
argument-hint: "[path to spec.md]"
context: fork
allowed-tools:
  - Read
  - Grep
  - Glob
  - Bash(git diff:*)
  - Bash(git status:*)
  - Bash(git log:*)
metadata:
  source: Adapted from https://github.com/mattpocock/skills (code-review, Spec axis), MIT, Copyright (c) 2026 Matt Pocock
---

# Spec Check

You are reviewing someone else's work against what was agreed. You did not write this code
and have no stake in it being right. Report findings; do not fix anything. The user decides.

## 1. Find the spec

`$ARGUMENTS` is the spec path if given. Otherwise glob `.scratch/*/spec.md`: exactly one match
is the spec. **No match or several matches: stop** and report the candidates. Guessing the
wrong spec produces a confident review of the wrong feature.

Read the spec in full. An **Open** section that is not empty is itself a finding: something
was decided silently in the code.

## 2. Pin the change

The spec's `Base:` line says where the change starts. **Not a git repository, or no `Base:`
line: stop** and say which one.

- Base is a sha → `git diff <sha>` (working tree against base, committed and uncommitted),
  plus `git log --oneline <sha>..HEAD`.
- Base is `none` → the repo had no commits when the spec was written; every file is part of the
  change.
- In both cases also read the files `git status --porcelain --untracked-files=all` marks `??`.
  `git diff` does not show untracked files, and in a young repo that is most of the new code.

An empty change: stop and say so, instead of reporting "all good" over nothing.

## 3. Review

Go through the spec line by line against the change:

- **Missing or partial**: a Decision or Solution point that is not implemented, or only half.
- **Implemented wrong**: it looks implemented, but differs from what the spec says. Exact
  values matter here: a spec saying "hh:mm" and code showing seconds is a finding.
- **Scope creep**: behaviour nobody asked for, and anything listed under **Out of scope** that
  was built anyway.
- **Unverified**: Acceptance checks that the code cannot prove (anything that only shows when
  the app runs on Windows). Say that it needs a manual check; never mark it as passed.

Quote the spec line for every finding and give `file:line` where the code is involved. Skip
style, naming and bugs that the spec does not speak to: that is `/code-review`'s job, and
mixing the two hides one inside the other.

## 4. Report

Use exactly this shape, so runs can be compared. Drop empty sections except **Passed**:

```
## Spec check: <feature>
Spec: <path> · Base: <sha | none> · Files in change: <n>

### Missing or partial (blocks the commit)
- **<decision>**: spec says "<quote>". <What is missing.> `file:line`

### Implemented wrong (blocks the commit)
- **<decision>**: spec says "<quote>", code does <x>. `file:line`

### Scope creep (user decides: add it to the spec, or remove it)
- **<behaviour>**: `file:line`. <Why it is not covered by the spec.>

### Unverified (manual check on Windows)
- <acceptance check>

### Passed
- <decisions that hold, for the record>
```

## Out of scope

- **Fixing findings**: this skill reports, the user decides.
- **Correctness and over-engineering**: `/code-review` and `/ponytail-review` run after this
  one. A standards axis with a code-smell baseline (as in the upstream code-review skill) was
  evaluated and left out 2026-09-29: several of those smells push towards more types and
  abstractions, which contradicts ponytail-review.
