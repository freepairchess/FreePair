# SwissSys-discretionary divergences from USCF rules

Cases in the curated SwissSys 11 pairing corpus where SwissSys's actual
pairings deviate from a strict reading of USCF rules and FreePair's
USCF engine follows the rule instead. These are tracked as **intentional
mismatches** — FreePair is the rule-correct side and we do NOT plan to
change the engine to match SwissSys here.

This log exists so future investigators don't waste time "fixing" a
case the engine is already handling correctly. When a regression test
fails on one of these (file, section, round) triples and the underlying
SwissSys vs FreePair difference matches the description below, mark it
as accepted and move on.

| Event | Section | Round | Rule cited | Description |
|---|---|---|---|---|
| Hellp 2026 | Under_700 | R3 | USCF 29C (lowest-rated floats) | 1.0-group floater: SwissSys floats player rated 478; FreePair floats lowest-rated 473 (who also has the stronger color claim — `WW` absolute B due). FreePair is more 29C-correct AND gives a better color outcome on the cross-group pair. |
| A2Z April Open 2026 | Under_1000 | R4 | USCF 29C (lowest-rated floats) | 1.0-group floater: SwissSys floats player rated 689 (second-lowest); FreePair floats lowest-rated 684. Color quality is identical either way (0 internal conflicts + 1 unavoidable cross-group conflict in both); SwissSys's choice has no rule justification (its own natural-pairings tool reports the same "4 bad alternations" before and after, so it wasn't color-driven). |

## How to add a new entry

1. Investigate the mismatch via the manual-testing workflow (compare
   SwissSys's natural-pairings diagram against FreePair's per-board
   output from the test harness).
2. Confirm that the rule FreePair follows is the standard reading of
   the cited USCF Official Rules section (28L*, 29C, 29D, 29E, etc.).
3. Confirm SwissSys's choice has no clear rule basis (or relies on a
   SwissSys-specific heuristic outside USCF rules).
4. Add a row above with the exact (event, section, round) triple plus
   a one-sentence summary that captures both the divergence and why
   FreePair is correct.

Per the release-notes convention, never mention specific event /
filename in user-facing release notes — describe the category in the
"Known limitations" section instead.
