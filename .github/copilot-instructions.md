# FreePair — Copilot instructions

Context for GitHub Copilot / Copilot Chat across sessions. Keep this file
concise; update it when a convention changes. Every new chat session
automatically picks these up.

## Product

FreePair is a cross-platform chess-tournament pairing application. It imports
SwissSys `.sjson` files, drives the [bbpPairings](https://github.com/BieremaBoyzProgramming/bbpPairings)
FIDE engine for Swiss pairings (and its own `RoundRobinScheduler` for
round-robin sections), and gives the TD tools to run the event end-to-end
(pair / score / intervene / report).

## Solution layout

```
src/
  FreePair.Core/           ← domain + pairing engine wrappers; no UI. netstandard-style.
    SwissSys/              raw JSON DTOs + mapper
    Tournaments/           domain records (Tournament, Section, Player, …)
      Constraints/         post-BBP same-team / same-club / do-not-pair swappers
      Standings/           + WallCharts/, Tiebreaks/
    Bbp/                   bbpPairings engine wrapper + BbpPairingResult
    Trf/                   TRF(x) writer for engine input
    Reports/               QuestPDF-based PDF report builder
    Formatting/            IScoreFormatter (ASCII / Unicode toggle)
    Settings/              AppSettings + ISettingsService
  FreePair.App/            ← Avalonia 12 desktop shell (WinExe)
    ViewModels/            CommunityToolkit.Mvvm [ObservableProperty] style
    Views/                 *.axaml + code-behind
tests/
  FreePair.Core.Tests/     xUnit; only test project (there is no App.Tests project — VM logic tested indirectly through the mutations layer)
  FreePair.Core.Tests/Data/ sample SwissSys .sjson fixtures
docs/                      scratch / reference docs (not built)
```

## Tech stack

- **.NET 10**, **C# 14** (target whatever the csproj says — do not downlevel).
- **Avalonia 12.0.\*** (floating patch). Fluent theme; DataGrid theme imported
  in `App.axaml` via `StyleInclude`.
- **CommunityToolkit.Mvvm 8.4.1** for `ObservableProperty` / `RelayCommand`
  source generators. All VMs derive from `ViewModelBase` (critical — the
  `ViewLocator` filters on it; `ObservableObject` alone won't resolve).
- **QuestPDF** (Community licence registered once in
  `PdfReportBuilder.RegisterLicense`).
- **xUnit** for tests.

## Domain patterns — immutable + mutation class

The domain model is a tree of immutable `record`s rooted at `Tournament`.
**Never mutate a record in place**; every edit goes through a static method
on `TournamentMutations` that returns a new tournament. This keeps
auto-save and undo sane.

Examples: `SetPairingResult`, `AppendRound`, `DeleteLastRound`,
`AddForcedPairing`, `SwapPairingColors`, `ConvertPairingToHalfPointBye`,
`SetTournamentInfo`, `SetPlayerWithdrawn`.

- Setter sentinel: `null` (nullable) or `SectionKind.Unknown` means
  "leave this field alone". Use this pattern when adding new batched mutations.
- Guards throw `ArgumentException` / `InvalidOperationException` with
  human-readable messages; the UI surfaces them in the error banner.

### Session-only fields

Several fields exist in the domain model but do **not** round-trip through
the `SwissSys` JSON writer yet: `Withdrawn`, `ForcedPairings`,
`DoNotPairPairs`, `AvoidSameTeam`/`AvoidSameClub` toggles, `Location`,
`DefaultPairingKind`, `DefaultRatingType`, `Email`, `Phone` (except Email /
Phone which DO round-trip via raw JSON pass-through because they were
already in the source file).

If you add a new field, default it with a safe non-null value and document
whether it persists. Don't invent new SwissSys JSON keys without
discussing.

## SwissSys round-trip

`SwissSysTournamentWriter` is a **raw-JSON pass-through**: it reopens the
original file, patches only the keys it knows how to write, and preserves
everything else untouched. This means new fields you add to `Player` or
`Section` are invisible to the writer unless you extend it — which is often
desirable (session-only semantics).

## Avalonia conventions

- **`ViewLocator`** matches `ViewModelBase` only. If you add a new VM and
  it renders as a type-name string in the UI, you forgot to inherit from
  `ViewModelBase`.
- **Compiled bindings** are enabled (`AvaloniaUseCompiledBindingsByDefault`).
  Every `DataTemplate` needs `x:DataType` or `DataType`.
- **Clipboard** (Avalonia 12): use
  `TopLevel.GetTopLevel(this).Clipboard` + `DataTransfer` +
  `DataTransferItem.CreateText(text)` + `IClipboard.SetDataAsync(transfer)`.
  The older `SetTextAsync` / `DataObject` APIs don't exist in 12.
- **DataGrid**: always set `SortMemberPath` to the underlying
  numeric/string field (not to formatted text) so sort order is correct for
  scores and tiebreaks. Use `DataGridTemplateColumn` for editable cells
  (ComboBox, inline buttons, copy icons).
- **Section tabs** (Players, Pairings, Standings, Wall Chart, Byes) all
  follow the same UX: filter bar at top, DataGrid with
  sort/resize/reorder, optional "📄 Print as PDF" button. New tabs should
  match.

## PDF reports

- Reports live in `FreePair.Core.Reports.PdfReportBuilder` — one static
  method per tab.
- **Default orientation is landscape** (`ApplyPageDefaults(landscape: true)`
  is the default). Only pass `landscape: false` for genuinely narrow
  reports.
- Every report gets the shared header (title / section / time control /
  location / date range) and footer (timestamp + "Page X of Y").
- The App-side code-behind writes PDFs via the `StorageProvider` save
  dialog, routes errors into `TournamentViewModel.ErrorMessage`, and
  success messages into `SaveStatus`.

## Testing

- All tests live in `tests/FreePair.Core.Tests` (xUnit). There is no
  `FreePair.App.Tests` project by design — UI behaviour is tested by
  exercising the mutations layer through the sample `.sjson` fixtures.
- Sample files are in `tests/FreePair.Core.Tests/Data/` — reference via
  `TestPaths.SwissSysSample(name)`.
- Before committing a feature: **build clean + all tests green**. The
  current baseline is ~298 tests; adding a feature should add tests, not
  regress any.

## Git / branch / commit conventions

- **Feature branches**: short lowercase kebab-case name of the feature
  (`eventconfig`, `player-contact-info`, `pdf-reports`). Create from
  `main` (or the most recent feature branch if you're building on top).
- **Commit messages**: imperative, prefixed by branch name, with a long
  body describing rationale + surface area + test impact. Multi-line
  bodies are welcomed — `git show` is the spec.
  Example:
  ```
  pdf-reports: default PDF page orientation to landscape for more horizontal room

  Players, Pairings, Standings, and Prizes were previously rendered
  portrait; Wall Chart was already landscape. Every TD-facing report
  tends to be wide … Flipped the default on ApplyPageDefaults from
  landscape:false to landscape:true. The parameter is preserved so
  callers can opt back into portrait for any narrow single-column
  report we add later.
  ```
- **Merges**: `git merge --no-ff <branch>` with a summary commit that
  lists each landing commit in the body. Keep feature boundaries visible.
- Push every commit; don't batch many commits before pushing.

## When in doubt

Grep the git log for an analogous feature and mirror its pattern.
`git log --oneline --all | Select-String <keyword>` gives a fast
overview. Commit bodies are verbose specifically to enable this.
