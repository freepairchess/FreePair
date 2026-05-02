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
| P1 | Round 2+: score-group pairing within rating order. | TODO |
| P2 | Repeat-pairing avoidance via transpositions inside score groups (USCF 28L1–L3). | TODO |
| P3 | Colour allocation per USCF 29D (absolute / strong / mild preference); cross-score-group drop-downs. | TODO |
| P4 | Half-point bye / withdrawal / forced-pairing parity with `BbpPairingEngine`. | TODO |
| **P5a** | **Round-1 verification harness**: replays round 1 of every USCF-rated `.sjson` under `docs/samples/swisssys/uscf/`, compares against SwissSys actuals. Pinned to baseline counts (matched ≥ N, hard-fail ≤ M); fails on regression in either direction. | ✅ |
| **P5b** | **Round-2+ verification harness**: synthesises per-round TRF state from `Player.History` and runs the engine. Catches `NotImplementedException` as a soft "engine doesn't yet handle this round" outcome — every section-round shifts from `⤬ unimplemented` to `✓ matched` as P1+ land, giving us a 400+ case TDD net. | ✅ |
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
it. Today the engine throws `NotImplementedException` for any
history-bearing document (deliberate guardrail — see `UscfPairer.cs`),
which the harness catches and logs as the soft outcome `⤬ unimplemented`.

Current baseline (across the 110-event corpus):

| Outcome | Count | Meaning |
|---|---|---|
| ✓ matched | **0** | Engine doesn't pair round 2+ yet — that's P1's job. |
| ⤬ unimplemented | **412** | The deliberate `NotImplementedException` from `UscfPairer.Pair`. Each one of these is a TDD-ready test case waiting for P1+ to start producing real output to compare against SwissSys. |
| ⤼ skipped | 235 | Sections with `RoundsPlayed < 2` (single-round events, not-yet-started events) plus pre-flagged byes. |
| ✗ mismatch | 0 / ! error | 0 | Both fatal categories are at zero — the engine fails loudly (`NotImplementedException`) where it can't pair, never silently produces wrong output. |

As P1 (score-group matching) lands, those 412 `⤬ unimplemented` outcomes
start shifting to `✓ matched`, `◐ colour-only`, or `✗ mismatch`. Each
flip is provable forward motion. The harness fails on any **hard
mismatch** or **non-`NotImplementedException` error**, so when P1 starts
producing pairings, every regression / oversight surfaces immediately
on the next test run.

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
