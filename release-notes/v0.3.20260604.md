# FreePair v0.3.20260604

USCF pairing engine fidelity improvements from a manual case-by-case review of the regression corpus. Five real engine fixes, no regressions.

## What's new

### USCF pairing engine — colour and floater fixes

The colour allocation and score-group drop-selection rules now reproduce SwissSys's behaviour more faithfully across the curated regression suite. Specific TD-facing improvements:

- **Better float-down picks under colour pressure.** When the lowest-rated player of a score group can't be paired without an unavoidable colour conflict, the engine now correctly considers the colour outcome of every floater candidate before settling on one — not just the natural slide. This eliminates a class of cases where the engine was floating the wrong player and creating downstream colour cascades.
- **Equalisation wins over score when colour preferences match.** When two players are both due the same colour, the player who has actually played fewer of that colour now gets it — even if the other player has a higher score. (Previously, score could override a clean equalisation claim.)
- **Smarter alternation tiebreaker.** When two players both last played the same colour but have different per-colour recency (one was on a bye in between, one had it back-to-back), the engine now gives White to whoever has gone longest without it. Handles "never played White" cases gracefully too.
- **Unified rule for "no colour history" pairings.** When both players had byes or forfeits in every prior round (so neither has a colour preference yet), the higher-rated player now consistently gets White — regardless of whether their scores match. Replaces an older fallback that produced different results depending on the section's initial-colour setting and parity of the board number.

### Tracking known engine-vs-SwissSys divergences

Added `docs/USCF_DISCRETIONARY_DIVERGENCES.md` to track two categories:

- **Accepted divergences** — cases where SwissSys deviates from a strict reading of USCF rules and FreePair correctly follows the rule (e.g. "lowest-rated floats", "not unrated if avoidable"). These will continue to show as MISMATCH in the harness but are *intentional* — FreePair is rule-correct.
- **Deferred improvements** — cases where SwissSys's choice is better than FreePair's by a clear rule (typically USCF 29E colour quality) but the obvious fix regresses other passing cases. Tracked so future work doesn't lose them; each one calls out the larger refactor needed before a safe fix can land.

## Installer

This release ships a self-contained Windows x64 installer (`FreePair-win-x64-Setup.exe`) and a no-install portable build (`FreePair-win-x64-Portable.zip`). Both bundle:

- the FreePair desktop application (Avalonia UI),
- the FreePair USCF pairing engine,
- the FIDE Dutch pairing engine (BBP Pairings),
- the .NET 10 runtime — **no separate runtime install required**.

The auto-updater (Velopack) is included; once a future release is published, the app will offer to update itself on next launch.

## Known limitations

A small number of pairings still diverge from SwissSys's output. Categories (unchanged from the previous release):

1. **Board-ordering preferences** — pairings, colours, and bye all match; only the board numbering differs.
2. **SwissSys-discretionary choices that conflict with USCF rules** — SwissSys sometimes gives byes to unrated players when a rated alternative was available, or floats a non-lowest-rated player; FreePair correctly follows USCF rules. See `docs/USCF_DISCRETIONARY_DIVERGENCES.md` for the catalogue.
3. **Deferred engine improvements requiring larger refactors** — weighted conflict-cost model (so absolute `WW`/`BB` preferences outrank mild `w`/`b` preferences), absolute-board-number threading for cross-section colour tiebreaks, and floater-lock heuristic refinements. Also catalogued.

Future releases will continue to narrow the gap event by event.
