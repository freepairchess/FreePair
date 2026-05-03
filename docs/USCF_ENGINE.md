# FreePair USCF pairing engine

This is FreePair's home-grown USCF (US Chess Federation) Swiss pairing
engine, built so FreePair doesn't have to lean on `bbpPairings.exe` (which
implements FIDE Dutch, not USCF) for events that need to be USCF-rated.

## Architecture

Two artefacts:

| Project | Purpose |
|---|---|
| `src/FreePair.Core/Uscf/` | Engine **library** — TRF reader, BBP-format writer, the pairing algorithm itself. Pure C#, no I/O on the hot path, fully unit-testable. |
| `src/FreePair.UscfEngine/` | Console **exe** — a thin wrapper around the library that mirrors the `bbpPairings.exe` CLI so a TD can swap engines by changing only `Settings → Pairing engine binary`. |

The library is referenced directly by the test project, so the
algorithm runs in-process under xUnit; the exe is only relevant when
exercising the binary-swap integration with FreePair's existing
`BbpPairingEngine` subprocess code.

## Input / output

**Input**: FIDE TRF, the same format `FreePair.Core.Trf.TrfWriter` produces
when feeding bbpPairings. The reader is a tolerant column-based parser
that recognises the subset of TRF tags FreePair writes (`012`, `042`,
`052`, `XXR`, `XXC`, and `001` player lines).

**Output**: BBP plain-text pairings (`{count}\n{w} {b}\n…`), the same
format `FreePair.Core.Bbp.BbpPairingsParser` already understands. A bye
is emitted as `{bye_pair} 0` on its own line.

This means **nothing** on the FreePair side needs to change to use the
new engine: the existing `BbpPairingEngine` writes a TRF, runs whichever
exe `Settings.PairingEngineBinaryPath` points at, and parses the BBP-
format output. Swap `bbpPairings.exe → FreePair.UscfEngine.exe` and the
app keeps working.

## Phased roadmap

| Phase | Scope | Status |
|---|---|---|
| **P0** | Project skeleton, `TrfReader`, `BbpFormatWriter`, **round-1 pairing** (USCF 28C top-half-vs-bottom-half by rating, alternating colours starting from `XXC`). | ✅ |
| **P1** | **Round 2+: score-group pairing** (USCF 28D / 28L). Score groups (highest first), top-half-vs-bottom-half within each group, lowest-rated of odd groups floats down to the next group, last-group leftover gets the round's bye. Simple "fewer whites gets white" colour rule as a P3 placeholder. | ✅ |
| **P2** | **Repeat-pairing avoidance** via single-swap transpositions (USCF 28L1). When the natural top-half-vs-bottom-half pairing inside a score group would force a rematch, swap `bot[i]` with the closest `bot[j]` (j > i) where neither resulting pair is a rematch. Deeper transpositions / interchanges (28L2-L3) are P2-future. | ✅ |
| P3 | Colour allocation per USCF 29D (absolute / strong / mild preference); cross-score-group drop-downs. | TODO |
| P4 | Half-point bye / withdrawal / forced-pairing parity with `BbpPairingEngine`. | TODO |
| **P5a** | **Round-1 verification harness**: replays round 1 of every USCF-rated `.sjson` under `docs/samples/swisssys/uscf/`, compares against SwissSys actuals. Pinned to baseline counts (matched ≥ N, hard-fail ≤ M); fails on regression in either direction. | ✅ |
| **P5b** | **Round-2+ verification harness**: synthesises per-round TRF state from `Player.History` and runs the engine. Pinned-counts contract; was 100% `⤬ unimplemented` until P1 landed. | ✅ |
| P5c (later) | Adopt the `Backups/*.BK` snapshots from `MCC_2026-04/` as a finer per-mid-round oracle (verifies the engine against the *actual* SwissSys state at each TD action, not just end-of-round). | TODO |
| P6 | App wiring: settings UI radio (BBP / USCF), bundle the new exe in the Velopack installer. | TODO |

## P5a — what the round-1 harness has confirmed

`tests/FreePair.Core.Tests/Uscf/Harness/UscfRound1HarnessTests.cs` walks
the corpus at `docs/samples/swisssys/uscf/` (now ~110 events, ~360
sections) and runs `UscfPairer.Pair` against every section's
pre-round-1 state.

Current baseline (pinned via `MinExpectedMatches` /
`MaxExpectedHardMismatches` / `MaxExpectedColorOnlyDiffs` constants):

| Outcome | Count | Meaning |
|---|---|---|
| ✓ matched | **112** | Pair-set + bye + board ordering + colours all agree with SwissSys, with the initial colour (XXC) inferred per-section by trying both `'w'` and `'b'`. |
| ◐ colour-only diff | **6** | Pair-set + bye correct but board / colour differ. Most are TD manual swaps (rule 29E) or rating-tie / 29D equalisation nuances our P0 doesn't model. Will shrink as P3 lands. |
| ✗ hard mismatch | **27** | Pair-set or bye disagrees. Investigation of these is P3 follow-up; examples: small-section "1v4 / 2v3" convention (Vortex Side Games, 4p), rating-tie ordering between bottom-half consecutive seeds (Mar 2026 Under_1000, 20p). |
| ⤼ skipped | **215** | Mostly "round 1 not yet played" or sections with pre-flagged half-point byes (P4 territory). |

The pinned-counts contract gives us a regression net without forcing
us to fix every real-world divergence up-front: the build stays green
when the corpus surfaces new known-divergence categories, and turns
red the moment a previously-matching section starts mismatching or a
previously-unaccounted error appears.

## P5b — what the multi-round harness sets up

`tests/FreePair.Core.Tests/Uscf/Harness/UscfMultiRoundHarnessTests.cs`
synthesises a `TrfDocument` for every (section, round) where the round
was actually played and round ≥ 2, then runs `UscfPairer.Pair` against
it. Same pinned-counts contract as P5a.

Current baseline (after P2 — single-swap transpositions):

| Outcome | Count | Δ from P1 baseline |
|---|---|---|
| ✓ matched | **25** | +6 (transpositions fixed cases that were rematches) |
| ◐ colour-only diff | **14** | +3 (cases where transposition produced the right matching but colour was already off) |
| ✗ mismatch | **373** | -9 (each fixed by a single bot[i]↔bot[j] swap) |
| ⤬ unimplemented | 0 | — |
| ⤼ section / round skip | 235 | — |
| ! error | 0 | — |

Net: **+9 cases now have correct matching** (matched + colour-only).
The remaining 373 hard mismatches need either deeper transpositions
(28L2-L3 — multi-swap, interchanges across half-boundaries) or colour-
balance-aware swap selection. Both are P2-future / P3 work.

## CLI compatibility cheat-sheet

| Flag | bbpPairings | FreePair.UscfEngine |
|---|---|---|
| `--dutch` | FIDE Dutch | accepted as a no-op alias for `--uscf` (binary always runs USCF) |
| `--uscf` | not supported | explicit USCF mode |
| `--baku` | Baku acceleration | accepted; warns on stderr; currently unimplemented (P1+) |
| `<input.trf>` | yes | yes |
| `-p <out>` | yes | yes |

## Why a separate engine binary?

We could have implemented USCF in-process inside `BbpPairingEngine` and
chosen at runtime. The subprocess design wins on three points:

1. **Drop-in compatibility.** A TD who's used to bbpPairings can swap
   the binary without touching any other configuration; both options
   are then available and easy to A/B.
2. **Fault isolation.** A buggy pairer can't take down the FreePair
   process — it just fails the round and the TD sees a clear error.
3. **Easier to verify.** The exe takes a TRF in and emits a BBP-format
   text out; the verification harness (P5) can drive it the same way
   FreePair does, comparing against SwissSys-produced pairings line
   for line.

(That said, the library is exposed as plain C#, so a future
"in-process pairer" path that skips `Process.Start` is just an
`IBbpPairingEngine` adapter away.)
