# USCF verification corpus

Real USCF-rated SwissSys tournaments used as the ground-truth oracle for
FreePair's USCF pairing engine (`src/FreePair.Core/Uscf/` +
`src/FreePair.UscfEngine/`). The harness in
`tests/FreePair.Core.Tests/Uscf/Harness/` walks this directory, runs
`UscfPairer.Pair` on each round of each section, and compares the result
against what SwissSys 11 actually produced.

## Provenance

Donated by the FreePair maintainer from real events they directed in
2025–2026. Source files come straight off SwissSys 11's working directory
— including the per-round `*.BK` backups it writes after every change —
so we have not just final state but every intermediate step.

> **Privacy.** These tournaments contain real player names, USCF IDs,
> and ratings. The FreePair source repo is **private** (Azure DevOps,
> not GitHub) per `docs/RELEASING.md`. Do not copy this folder to the
> public release repo, and do not paste excerpts of these files into
> public bug reports / issues.

## Layout

Two flavours of entry exist side-by-side under `uscf/`:

1. **Per-event folders** for tournaments donated as a complete SwissSys
   working directory (final `.sjson` + USCF rating-report DBFs +
   per-round `Backups/` snapshots). One folder per event, named after
   the SwissSys folder (spaces normalized to underscores so paths stay
   sane in C#).
2. **`library/`** — flat collection of donated `.sjson` files (final
   state only, no DBFs / no backups). Cheaper to add a new event:
   just drop the file in.

Both shapes are picked up by `UscfSampleDiscovery` (it walks every
immediate sub-directory, treating each as an "event").

```
uscf/
├── Apr_2026/
│   ├── Chess_A2Z_April_Open_2026.sjson    ← SwissSys saved state (final)
│   ├── TDEXPORT.DBF                        ← USCF rating-report DBFs (the
│   ├── THEXPORT.DBF                          ground truth USCF receives)
│   └── TSEXPORT.DBF
├── Jan_2026/
│   ├── Hello 2026 Report.json              ← SwissSys 11.34+ uses .json
│   └── …DBFs…
├── Mar_2026/
│   └── … same shape …
├── MCC_2026-04/
│   ├── MCC__2026_04.sjson                  ← final state
│   ├── MCC - 2026-04 Report.json           ← also saved
│   ├── Backups/
│   │   └── MCC__2026_04 Backup~Rd<N><Section> <timestamp>.BK
│   │        ← ~30 round-by-round snapshots written by SwissSys after
│   │          every TD action. Same JSON format as .sjson — they're
│   │          renamed `.sjson` files. Not currently consumed because
│   │          P5b synthesises the same per-round state from the embedded
│   │          history in the final `.sjson` instead.
│   └── …other SwissSys cruft…
├── Oct_2025/
│   └── … same shape …
└── library/                                ← flat collection of bare .sjson
    ├── Hello_2026.sjson
    ├── Hello_2026_SwissSys11.sjson         ← same event, legacy 11.x format
    ├── 90th_Greater_Boston_Open.sjson
    ├── MCC__2025_03_SwissSys11.sjson
    ├── … 100+ more events …
```

## Duplicate events

Some events appear twice in `library/`: once as `<Event>.sjson` (newer
SwissSys 11.34+ format) and once as `<Event>_SwissSys11.sjson` (legacy
SwissSys 11.x). Both files describe the same tournament — keeping both
exercises FreePair's importer against both formats and proves they
produce identical pairings (round 1 of `<Event>.sjson` MUST match round
1 of `<Event>_SwissSys11.sjson`, end of story). The harness reports
both as separate "event :: section" cases; that's intentional.

A handful of events also appear once in a per-event folder
(e.g. `Apr_2026/`) and again in `library/` because the folder version
came with DBFs and the flat version didn't. Same data, picked up twice
— harmless redundancy.

## What the harness uses

The round-1 harness (`UscfRound1HarnessTests`, P5 phase 0) only needs
the `*.sjson` / `*.json` final-state files. Round 1 is reconstructable
from the final state because SwissSys preserves the full round-by-round
result history per player.

The future per-round harness (P5b, planned) will replay each event:
walk `Backups/*.BK` chronologically, find the snapshot matching "end of
round N for section X", drive `UscfPairer.Pair` against a TRF derived
from that snapshot, and compare the result to the round-N+1 pairings in
the next snapshot.

## Adding a new event

1. Drop the SwissSys folder under `uscf/<event>/`. Keep the original
   layout — the `*.BK` backups are valuable for the per-round harness.
2. Run the test suite. The harness auto-discovers any `.sjson` / `.json`
   under this directory tree.
3. If the event isn't USCF-rated (`Rating to use` ≠ USCF), the harness
   skips it with a note in the test output.
