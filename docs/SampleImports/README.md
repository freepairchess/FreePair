# Sample player-import files

Drop any of these into FreePair via the **📥 Import…** button on the
Players tab to see the import flow end-to-end.

| File | Format | Notes |
|---|---|---|
| `players.csv` | Comma-separated | Also shows `"Dave ""Doc"" Daniels"` with embedded quotes escaped per RFC 4180, and `unrated` in the rating cell (falls back to 0 with a warning). |
| `players.tsv` | Tab-separated | Same rows, simpler escaping (no quotes needed). Use this if your data contains commas. |

## Expected columns

Header names are **case-insensitive**, and each recognized field
has a few aliases. Unknown columns are skipped (with one aggregate
warning).

| Canonical field | Aliases |
|---|---|
| Name (required) | `Name`, `Player`, `Player name`, `Full name` |
| Rating | `Rating`, `USCF rating`, `Regular rating`, `Rtg` |
| Rating 2 | `Rating 2`, `Rating2`, `Secondary rating`, `Quick rating` |
| USCF ID | `ID`, `USCF ID`, `USCF`, `Membership`, `Membership ID` |
| Membership exp. | `Exp`, `Expires`, `Expiration`, `Membership exp`, `Exp1` |
| Club | `Club` |
| State | `State`, `St` |
| Team | `Team` |
| Email | `Email`, `E-mail` |
| Phone | `Phone`, `Tel`, `Telephone` |

## Excel (`.xlsx`)

Excel files are also supported — the importer reads the first
worksheet and uses the same column-header rules. Any worksheet
that opens cleanly as a table in Excel will work; save your
spreadsheet as `.xlsx` (not `.xls`) and pick it from the import
dialog.

## Where do imported players go?

Each row becomes a new player in the **currently-selected**
section via the standard `AddPlayer` mutation. Pair numbers are
assigned as `max(existing) + 1`. If the section has already
paired rounds, each new player's history is back-filled with
zero-point byes for every past round (same default the "➕ Add
player" dialog uses).

## Validation

| Condition | Behavior |
|---|---|
| Missing `Name` column | Import aborts with a warning; no rows created. |
| Blank name in a row | Row skipped with a warning. |
| Un-parseable rating | Rating defaults to 0, warning logged. |
| Un-parseable secondary rating | Secondary rating ignored, warning logged. |
| Duplicate import | No de-duplication — running the same file twice creates duplicate players. Delete manually if you need to re-import. |
