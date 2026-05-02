# FreePair.UscfEngine

A USCF (US Chess Federation) Swiss pairing engine packaged as a CLI binary
that is **drop-in compatible with `bbpPairings.exe`**.

## Why

FreePair invokes its pairing engine via subprocess: it writes a FIDE TRF
file, runs `<engine.exe> --dutch <input.trf> -p <output.txt>`, and parses
the BBP plain-text pairings file. To use the FreePair USCF engine
instead of bbpPairings, a TD only has to change the
`Settings → Pairing engine binary` path — no other configuration.

## CLI

```
FreePair.UscfEngine.exe [--dutch | --uscf] [--baku] <input.trf> -p <output.txt>
```

| Flag | Meaning |
|---|---|
| `--dutch` | Accepted for compatibility (FreePair always passes it). Treated as a synonym for `--uscf`. |
| `--uscf` | Explicit opt-in for USCF rules. |
| `--baku` | Accepted but currently a no-op; a warning is written to stderr. USCF rarely uses Baku acceleration; we'll add the SwissSys-style scheme later. |
| `-p <file>` | Output path for the BBP-format pairings file. |
| `<file>.trf` | Positional input — a FIDE TRF document (the one FreePair's `TrfWriter` produces). |

Exit codes match `bbpPairings`: `0` success, non-zero on parse / pairing
failure with a one-line message on stderr.

## Status

Round 1 pairing only (top-half vs bottom-half by rating, USCF rule 28C).
Round 2+ is the next phase. See `docs/USCF_ENGINE.md` for the roadmap.
