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
| A2Z April Open 2026 | Under_700 | R4 | USCF 28L ("not unrated if avoidable") | Bye selection in the 0.0 group: SwissSys gives the full-point bye to the unrated player (#16 Saripalli); FreePair gives it to the lowest-rated *rated* player (#14 Thompson, 198). FreePair correctly follows 28L's "lowest-rated, but not unrated if avoidable" preference. The downstream bd 3–4 transposition difference is a side-effect of which player is removed from the pool — color-conflict counts are identical either way. |

## Deferred — engine could improve, not yet safe to change

Cases where SwissSys's choice is *better* than FreePair's by a clear
rule (typically USCF 29E colour quality) but the obvious fix to our
drop-selection heuristic regresses other passing cases in the corpus.
Tracked here so we don't lose them, but **do not attempt a narrow fix
without expanding the regression coverage first** — every attempted
patch so far has shifted the failing-test set rather than shrinking
it.

| Event | Section | Round | Issue | Why deferred |
|---|---|---|---|---|
| A2Z April Open 2026 | Open I | R3 | 1.0-group drop: SwissSys floats second-lowest (Mallapu 1437) for 0-conflict internal SLIDE; FreePair floats lowest (Kesavan 1411) which leaves 1 unavoidable internal conflict AND 1 cross-group conflict (W-due vs Randall W-due). SwissSys's pick is strictly better on USCF 29E colour grounds. | Adding cross-group cost or loosening the "natural < 2 conflicts" threshold fixes this case but regresses 3–8 others (Open II R2, multiple MCC rounds, Hellp later rounds). The current `CountColorConflictPairs` weights all conflicts equally and ignores absolute-vs-mild colour preferences (`WW`/`BB` absolutes count the same as a mild `w`/`b` preference). A proper fix needs a weighted conflict-cost model that distinguishes preference strength and applies cross-group cost only when the drop's downfloat is forced into a specific opponent. |
| MCC April 2026 | U1700 | R2 | Color of bd 6 (#2 Smith 1543 vs #8 Nicewicz 1480, both R1-bye): SwissSys gives White to #8 (lower-rated bottom). FreePair gives White to #2 (higher-rated top) via the v8 "higher-rated gets White when both no history" rule. SwissSys appears to use absolute-board-number alternation from "White-on-bd-1" default, ignoring the section's TD-chosen InitialColor='b'. Also: Open R2 in the same event uses the SAME rule but with different board ordering — fixing only the color rule swaps which test passes. | Two-part problem: (a) SwissSys uses absolute-bd-number alternation ignoring section InitialColor for both-no-history pairings; (b) FreePair's board ordering within a score group containing multiple bye-bye pairs doesn't match SwissSys's (sort-by-max-rating-desc differs across the same pool). Fixing (a) without (b) just shifts the failure. Needs both fixes landed together; out of scope for a single-event session. |
| MCC May 2026 | Open | R3 | 1.5-group pairing: FreePair locks the floater #6 (from 2.0) to top of group #3 per USCF 28F2, giving SLIDE (3,6)(5,13)(7,15) = 3 color conflicts. SwissSys unlocks the floater and pairs (3,13)(5,6)(7,15) = 1 conflict (huge USCF 29E win). | Tried two fixes: (1) remove the floater lock entirely — broke 4 other cases that depend on the lock. (2) Conditionally unlock when reduction ≥2 — still broke 2 cases (MCC April U1700 R3, R5). The lock has a strong correctness tradeoff: it's right most of the time but wrong when 28F2 needs to yield to 29E. A proper fix needs to model when SwissSys breaks 28F2 vs honors it; possibly tied to floater rating relative to top of lower group (here #6 1825 is close to #5 1836, so floater is "naturally in the middle" of the lower group — but that's just one signal). |
| MCC May 2026 | U1700 | R2 | bd 6 (#20 1050 vs #21 1034, both R1-HPB, both 0.5 pts): SwissSys gives White to lower-rated #21. FreePair's v8 rule "higher-rated gets White when both no history" gives #20 White. Pattern across the corpus: within-score-group both-no-history pairs use absolute-board-number alternation (white-on-bd-1 default); cross-score-group (downfloater) both-no-history pairs use higher-rated-gets-White. The two rules disagree, and FreePair can't currently distinguish because `board` is section-relative-1-based and the section's absolute first-board isn't threaded through `UscfPairer`. Same shape also affects bd 11 + bye recipient (SwissSys bye=#26, FreePair bye=#25). | Two-part fix needed: (a) Thread section FirstBoard into UscfPairer so abs-board can be computed; (b) Distinguish same-score-group from cross-group bye pairings to pick the right rule. Out of scope for a single-event session. |

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
