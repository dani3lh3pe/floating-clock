---
name: grilling
description: Interviews the user in rounds about a planned feature or design until every decision is settled, recommends an answer to each question, researches facts itself instead of asking, and records hard-to-reverse decisions as ADRs in docs/adr/ while it goes. Use whenever the user wants to think a change through before any code is written, e.g. "grill mich dazu", "brainstorme mit mir vor jeglicher Umsetzung", "lass uns das erst durchdenken", or opens a new feature whose behaviour is still fuzzy. Do not use for a bug fix or a change that is already fully specified, and do not use to write the spec itself, that is `spec`.
metadata:
  source: Adapted from https://github.com/mattpocock/skills (grilling + domain-modeling), MIT, Copyright (c) 2026 Matt Pocock
---

# Grilling

Interview the user until you both share one understanding of the change. Nothing gets
implemented during this skill: every decision made here is cheap, every one discovered after
the code exists costs a rework round.

## The design tree

Map the change as a tree: every decision branches into the decisions that hang off it. The
**frontier** is every decision whose prerequisites are already settled, the questions you can
ask now without guessing at answers you have not heard yet.

Work the tree in **rounds**:

1. Ask the frontier, at most ~6 questions, the ones that gate the most other decisions first.
   Longer rounds get skimmed and answered with "passt", which settles nothing.
2. Number each question and give your recommended answer with a one-line reason. The user
   should be able to accept most of a round in a word.
3. Wait for the answers. They reshape the tree; recompute the frontier and ask the next round.
   A question that depends on another question still open belongs to a later round.

Ask in the language the user writes in. Format a question like this:

```
**Q1: <title>**
<question, with the options if there are any>
→ Recommendation: <answer>, because <reason>
```

**Facts are your job, decisions are the user's.** When a question needs a fact (what the
platform supports, what an API does, what already exists in the repo), find it yourself. For
anything that takes more than a quick look, dispatch a sub-agent, and do not block on it: only
the questions downstream of that fact wait, ask the rest of the frontier now. Never ask the
user something you could look up.

"Mach einfach, du weißt schon" is not an answer to a fork in the road. Turn it into a stated
assumption ("Ich nehme X an, weil Y") and have it confirmed, so it cannot be silently forgotten.

## Seed questions for this project

A Windows desktop overlay has a handful of areas where a missed question costs a rework round
later. Use them to grow the tree when the change touches the area. They are prompts for you,
not a questionnaire to read out.

- **What time means**: does elapsed time keep counting through sleep, hibernate, screen lock,
  app restart, reboot? What happens on a clock or time-zone change? Which clock source advances
  during sleep is a fact to verify, not assume.
- **Destructive actions**: reset throws away the measured time. Confirmation, undo, or neither?
- **Always on top**: over fullscreen apps, presentations, the taskbar? Toggleable?
- **No window chrome**: how is it moved, resized, closed, quit? Taskbar button, tray icon,
  Alt+Tab entry: which of them, if any?
- **Controls**: click, double-click, context menu, global hotkeys? What stops an accidental click?
- **Appearance**: which colours, fonts, formats (hh:mm:ss or hh:mm), sizes, opacity? Is
  click-through wanted? Settings UI or config file?
- **Monitors and DPI**: is the position remembered? What happens when that monitor is
  disconnected and the window would open off-screen? Per-monitor DPI scaling?
- **Stack and build**: development happens on Linux, the app runs on Windows. Which stack builds
  there, and how does a build reach the Windows machine for testing?
- **Where it runs**: a managed work machine with application control? Unsigned binaries were
  blocked there by Defender ASR (field-confirmed 2026-08-14 in entra-pim-manager). Signing,
  portable exe, or installer?
- **Explicit no's**: session history, daily totals, several timers? Naming what is out is as
  valuable as naming what is in.

## ADRs, written the moment a decision qualifies

Record a decision as an ADR only when all three hold:

1. **Hard to reverse**: changing your mind later costs real work.
2. **Surprising without context**: a future reader would wonder why it was done this way.
3. **A real trade-off**: there were genuine alternatives and one was picked for reasons.

If any of the three is missing, skip it: an easy decision gets reversed, an obvious one needs
no record. Most rounds produce none, and that is correct. Rejected alternatives whose rejection
is not obvious belong in the ADR too, or they get suggested again in a month.

Write it the moment the decision settles, not batched at the end, where half the reasoning is
already gone. Location `docs/adr/NNNN-slug.md` (create the directory on first use; number =
highest existing + 1). Write it in English, like every file in this repo:

```md
# <Short title of the decision>

<1-3 sentences: the context, what was decided, and why.>

<Optional, only when worth remembering: Considered options / Consequences.>
```

Tell the user in one line when you wrote one.

## Done

The session is done when the frontier is empty: every branch visited, nothing silently assumed.
Close with a short summary in three labelled parts, because those are exactly where a
misunderstanding shows:

```
Entschieden : <decision>, <decision>, …
Annahmen    : <what you assumed and the user confirmed>
Nicht dabei : <what is explicitly out of scope>
```

Then offer to write the spec (`spec`). Do not start implementing until the user confirms the
shared understanding.

## Out of scope

- **Writing the spec**: that is `spec`, which works from this conversation.
- **A `CONTEXT.md` glossary**: evaluated and left out 2026-09-29. Its value is shared vocabulary
  between several humans; for a small solo project it is pure overhead. Revisit if terminology
  confusion actually shows up.
- **Writing code**, including prototypes. If a question can only be answered by trying
  something, say so and let the user decide whether to spike it.
