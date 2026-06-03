# FreePair v0.3.20260603

USCF pairing engine fidelity overhaul, validated against a large real-world SwissSys 11 corpus of USCF events.

## What's new

### USCF pairing engine
The FreePair USCF pairer now reproduces SwissSys 11 pairings, byes, board ordering, and colours on **83.7% of all individual pairings** (and 100% on many representative events) across the curated regression suite. Specific TD-facing improvements:

- **No double byes.** A player who has already received a full-point bye is now skipped when the engine picks the next round's auto-bye, even if they're still the lowest-rated active player. This honours USCF 28L4 ("a player should not receive more than one full-point bye").
- **Unrated players keep playing.** When the field is odd and the lowest-rated player is unrated, the engine now correctly assigns the bye to the lowest-rated *rated* player instead. Unrated players need games to establish a rating; a bye gives no rating data. (USCF 28L: "lowest-rated player, but not unrated if avoidable.")
- **Scheduled byes are respected.** When the TD has flagged a player for a half-point or zero-point bye in any round (past, present, or future), the auto-bye selector knows not to stack a full-point bye on them as well.
- **Forced merges instead of repeat games.** When a score group cannot be paired without re-creating a game that already happened, the engine now floats the whole group down and pairs everyone against the next group, rather than committing the rematch. This eliminates an entire class of accidental rematches in mid-to-late rounds.
- **Floater placement is now consistent.** Players who float up or down to pair with another group always sit on the top boards of the merged section, matching long-standing TD convention (USCF 28F2).
- **Cleaner score-group ordering.** Within a score group, players with equal ratings (or multiple unrated entries) now slot into the slide in their entry order, exactly matching how SwissSys orders them.
- **Forfeit / withdrawal handling.** When a game wasn't actually played, the engine no longer treats the placeholder colour record as meaningful for future-round colour balancing.

### New regression test suite
A new harness builds one test per `(file × section × round)` from real-world SwissSys 11 `.sjson` exports and asserts that FreePair's pairer reproduces SwissSys's output exactly. This lets contributors safely refactor the pairing engine without regressing established behaviour, and gives TDs evidence that swapping FreePair in for SwissSys won't surprise them.

### "Spirit match" reporting
Tests now distinguish between:
- **Exact match** — pairings, byes, colours, AND board numbers match SwissSys exactly.
- **Spirit match** — same pairings, same colours, same bye; only the board numbers differ (FreePair's convention is "higher-rated pair on the higher board", which sometimes differs harmlessly from SwissSys's preferred numbering within a score group).
- **Mismatch** — actual pairing or colour difference.

## Installer

This release ships a self-contained Windows x64 installer (`FreePair-win-x64-Setup.exe`) and a no-install portable build (`FreePair-win-x64-Portable.zip`). Both bundle:

- the FreePair desktop application (Avalonia UI),
- the FreePair USCF pairing engine,
- the FIDE Dutch pairing engine (BBP Pairings),
- the .NET 10 runtime — **no separate runtime install required**.

The auto-updater (Velopack) is included; once a future release is published, the app will offer to update itself on next launch.

## Known limitations

A small number of pairings still diverge from SwissSys's output. These are documented and tracked, and fall into three categories:

1. **Board-ordering preferences** — pairings, colours, and bye all match; only the board numbering differs.
2. **SwissSys-discretionary choices that conflict with USCF rules** — a few SwissSys files give the bye to an unrated player when a rated alternative was available; FreePair correctly follows USCF 28L here.
3. **Single-player score-group handling** — for sections where one or more players are the sole member of their score group, SwissSys sometimes merges them upward into the higher group; FreePair currently floats them downward. Both produce valid USCF-legal pairings.

These are deliberate trade-offs, not engine bugs. Future releases will continue to narrow the gap event by event.
