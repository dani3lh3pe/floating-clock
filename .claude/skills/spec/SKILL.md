---
name: spec
description: Turns the decisions of the current conversation, typically a finished grilling session, into a lean spec file at .scratch/FEATURE/spec.md that survives context compaction and that spec-check later reviews the diff against. Synthesises, does not interview. Use when a grilling session has ended, or when the user says "schreib das als Spec", "halt fest, was wir entschieden haben", "fass das zusammen, dann legen wir los". Do not use to explore an open idea, that is `grilling`, or to review code against a spec, that is `spec-check`.
metadata:
  source: Adapted from https://github.com/mattpocock/skills (to-spec), MIT, Copyright (c) 2026 Matt Pocock
---

# Spec

Write down what the conversation decided, so the decisions live in a file instead of a context
window. Long sessions get compacted, and a decision that only existed in the conversation is
gone afterwards.

## 1. Synthesise, do not interview

The decisions were made already, usually in `grilling`. Do not re-ask them. If the
conversation left a real decision open, do not invent an answer: put it under **Open** and ask
it before the spec is confirmed.

**Carry precise answers over verbatim.** Numbers, orderings, defaults and negative requirements
("reset never without confirmation", "no taskbar button") are what gets softened into vague
prose between an interview and a spec, and the spec then looks complete while missing the thing
that was actually decided. Re-read the user's own answers in the conversation and check each one
landed in the spec with its exact value.

## 2. Pick the test seams

A seam is the public boundary a test observes behaviour at. Prefer existing seams to new ones,
and the highest seam possible; the fewer seams the better. Logic that can be tested on the
Linux dev host (time arithmetic, settings persistence, formatting) is worth a seam. What only
shows on Windows (always-on-top, transparency, dragging) goes under Acceptance checks as a
manual step instead of a test nobody can run here. Confirm the seams with the user.

## 3. Write the file

Path: `.scratch/<feature-slug>/spec.md`. `.scratch/` is gitignored on purpose: a spec is
working material for one feature. The durable record of *why* is the ADRs in `docs/adr/`.
Record the base commit so `spec-check` knows what the change is: `git rev-parse HEAD`, or
`none` if the repo has no commits yet. Write the file in English.

```md
# <Feature>

Base: <commit sha | none>

## Problem
<The problem from the user's perspective.>

## Solution
<What the user will see and be able to do. Not how it is built.>

## Decisions
1. <One precise, checkable decision per line, exact values included. Link the ADR where one exists.>

## Out of scope
- <What is deliberately not built, and why if not obvious.>

## Test seams
- <Seam>: <what the tests there prove>

## Acceptance checks
- <What to do on Windows> → <what must be seen>

## Open
- <Decisions still missing. Empty before implementation starts.>
```

Leave out file paths and code: they go stale before the feature is done. The exception is a
snippet that states a decision more precisely than prose can (a state machine, a settings
schema).

## 4. Confirm

Show the Decisions and Out of scope sections in the chat and ask the user to confirm. **Open**
must be empty before implementation starts, because an open decision gets decided silently by
whoever writes the code.

## Out of scope

- **Tickets and an issue tracker**: `to-tickets` was evaluated and left out 2026-09-29. This is
  a solo project without a tracker, and features fit one session. Add it when a feature no
  longer fits one session.
- **User-story lists**: ceremony for a team; Decisions carry the same content checkably.
- **Reviewing the implementation**: that is `spec-check`.
