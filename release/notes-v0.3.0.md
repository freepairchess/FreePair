# FreePair v0.3.0

USCF pairing engine fidelity overhaul, validated against a real-world SwissSys 11 corpus.

## What's new

A new curated regression harness (`UscfSwissSysPairingTests`) generates one test per `(file × section × round)` from real-world `.sjson` exports of USCF events (A2Z, MACA, MCC, PTC) and asserts that FreePair's `UscfPairer` reproduces SwissSys's pairings, byes, board ordering, and colours exactly. Where FreePair's deliberate convention differs harmlessly (e.g. higher-rated pair on the upper board), the harness now recognises a "spirit match" if every pairing and every colour matches and only the board numbering differs.

## Pairing fidelity

| File | Tests pass | Pairings match |
|---|---|---|
| A2Z May Open 2026 | 14 / 14 | **178 / 178 — 100.0%** |
| A2Z April Open 2026 | 10 / 14 | 97 / 108 — 89.8% |
| A2Z Inaugural Open 2026 | 7 / 14 | 115 / 133 — 86.5% |
| MACA Senior Open | 3 / 4 | 33 / 37 — 89.2% |
| MCC 2026-03 | 5 / 8 | **73 / 73 — 100.0%** |
| MCC 2026-04 | 2 / 10 | 85 / 99 — 85.9% |
| MCC 2026-05 | 2 / 8 | 63 / 86 — 73.3% |
| PTC Hello 2026 | 2 / 9 | 89 / 162 — 54.9% |
| **TOTAL** | **45 / 81** | **733 / 876 — 83.7%** |

## Engine rules now enforced

- **USCF 28C / 28E** — rating-descending order with `PairNumber` ascending as tiebreak (entry order, matching SwissSys's convention). Unrated players slot into the slide in the same relative order SwissSys assigns.
- **USCF 28L** — automatic full-point bye goes to the lowest-rated player who hasn't already had a bye **and isn't unrated when a rated alternative exists**. Unrated players need games to establish a rating; a bye gives no rating data.
- **USCF 28L4** — "a player should not receive more than one full-point bye." Enforced via:
  - past-history check (round cell with result `'U'`),
  - TD-scheduled bye flag (`HasScheduledBye`),
  - and a pre-pass at the `Pair()` entry point that selects the correct bye candidate before the score-group loop runs, so the rest of the pairing structures around it.
- **USCF 28L3 forced merge** — when a score group's pool cannot be paired without a rematch even after a cross-half interchange, every member floats down to the next group instead of committing the rematch.
- **USCF 28F2 floater placement** — all floater pairs are pulled to the top boards after the colour-optimisation pass (the previous code only checked the first floater pair).
- **USCF 29C float drop** — prefer the lowest-rated **rated** drop candidate; unrated players stay in their natural score group when a rated alternative exists. The colour-friendly drop only overrides when the natural drop has ≥2 colour conflicts AND the alternative achieves zero conflicts.

## Other improvements

- Forfeit / withdrawn pairings accept either colour orientation in test comparison (no actual game was played; SwissSys's recorded colour is arbitrary).
- Removed an over-aggressive 8-player single-floater colour shim that was producing strictly-worse transpositions than the standard reducer for exactly the pool shape it targeted.
- `RoundOne` slide tiebreak fixed for unrated entries (was sorting by surname; now uses pair-number, matching SwissSys).
- Tournament writer round-trips full-point byes faithfully when multiple players received them in the same round (TD pre-assigned full byes).

## Known differences (deliberate)

A few SwissSys outputs intentionally don't match FreePair:

- **A2Z May Open · Under_700 R2** — same pairings, same colours, just SwissSys's board numbering. Passes as a "spirit match".
- **A2Z Inaugural Open · Under_700 R1** — SwissSys gave the auto-bye to the unrated player at the bottom of the seed list; FreePair correctly gives it to the lowest-rated **rated** player per USCF 28L. This is a one-off SwissSys discretionary choice.
- **PTC R3+** — SwissSys merges single-player score groups upward into the adjacent higher group; FreePair floats them down. Both produce valid USCF-legal pairings but the pair structures differ. The auto-bye selection itself is correct in every PTC round.

## Installer

This release ships a self-contained Windows x64 installer (`FreePair-win-Setup.exe`) — no separate .NET runtime install required. The bundled `bbpPairings.exe` (FIDE Dutch engine) and the separate `FreePair.UscfEngine.exe` (USCF engine) are included so the app works out of the box for both rating systems.
