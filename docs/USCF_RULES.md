# USCF pairing rules � FreePair reference

A complete, faithful paraphrase of every USCF *Official Rules of Chess*
(7th edition, V. 7) rule that touches Swiss-system pairings, indexed
to its enforcement status in FreePair. This document is the
authoritative reference for:

- the engine's `UscfRule:` citations (rendered in the **"Why this
  pairing?"** dialog),
- the SwissSys-fidelity test corpus (what we measure against, and what
  divergences are accepted),
- TD-facing release notes and support questions ("does FreePair
  enforce the 80-point rule?"),
- the Phase-C engine roadmap (each ? planned row is a candidate
  feature branch).

This is *paraphrase* � it explains how FreePair interprets and applies
each rule. For an authoritative source, consult the USCF rule book
itself.

## How to read this document

Each rule entry has six sections in a fixed order:

| Section | Meaning |
|---|---|
| **Status** | One of the coverage symbols below. |
| **Plain statement** | What the rule says, in TD-readable language. |
| **Worked example** | (optional) The rule book's example, paraphrased. |
| **FreePair coverage today** | What the engine actually does � honest, even when it's a gap. |
| **Annotation today** | (optional) Which `PairingReason` and `UscfRule:` string the engine emits when the rule fires. |
| **See also** | Cross-references inside this document. |

### Coverage symbols

| Symbol | Meaning |
|---|---|
| ? enforced | Implemented in the engine; annotation cites it. |
| ? partial | Some cases honoured, some missed � read the entry for what's in and what's out. |
| ? planned | Not enforced today. A `PairingReason` enum value may exist but is not yet wired. Phase C will land it. |
| ?? TD discretion | The rule is a TD option, not an algorithm. FreePair surfaces it as a setting, mutation, or manual override � the engine does not auto-decide. |
| ? deferred | Recognised but outside the foreseeable engine roadmap. Rationale documented in the entry. |

### Citation-anchor convention

Every rule has a stable Markdown anchor of the form `rule-<number>`,
lower-case, with letters preserved and dots / spaces stripped.
Examples:

- `29E5a` ? [`#rule-29e5a`](#rule-29e5a)
- `28L2a` (variation) ? [`#rule-28l2a`](#rule-28l2a)
- `Variation 29E4b` ? [`#rule-29e4b`](#rule-29e4b)

The "Why this pairing?" dialog generates these links mechanically by
`citation.ToLowerInvariant()`. **Never rename an anchor once
published** � it's a contract with future UI builds and external
deep-links.

### Phased rollout

This document is the **Phase A** deliverable of the USCF-deep-dive
work: it lays an authoritative foundation and catalogues every gap.
Subsequent phases close those gaps:

- **Phase B** � citation-correctness pass: every `UscfRule:` string
  in `UscfPairer.cs` is reconciled to the rule numbers in this
  document. No engine behaviour changes � just labels.
- **Phase C** � engine gap closure, one branch per ? row, each with
  SwissSys-fidelity test deltas and its own release note.
- **Phase D** � when coverage is comprehensive, polish this doc into
  a TD-facing reference accessible from the app's Help menu.

See [USCF_RULES_COVERAGE.md](USCF_RULES_COVERAGE.md) for the
at-a-glance status board that drives the Phase-C backlog.

---

## Quick lookup

The single source of truth for "is this rule enforced, and where?".
Click any rule number to jump to the full entry.

| Rule | Title | Coverage | Engine site | Citation today |
|---|---|---|---|---|
| [22A](#rule-22a) | Games forfeited due to nonappearance | ?? TD discretion | `TournamentMutations.SetPairingResult` | � |
| [22B](#rule-22b) | Full-point byes (defers to 28L) | ? | `UscfPairer.PairRoundN` bye picker | `28L` (over-general ? `28L2`) |
| [22C](#rule-22c) | Half-point byes | ?? TD discretion | `Player.RequestedByeRounds` + pool filter | � |
| [22C1](#rule-22c1) | Availability | ?? TD discretion | � | � |
| [22C2](#rule-22c2) | Deadline for bye requests | ?? TD discretion | � | � |
| [22C3](#rule-22c3) | Byes and class prizes | ?? TD discretion | � | � |
| [22C4](#rule-22c4) | Irrevocable byes | ?? TD discretion | � | � |
| [22C5](#rule-22c5) | Cancellation of irrevocable byes | ?? TD discretion | � | � |
| [22C6](#rule-22c6) | Full-point byes after half-point byes (? 28L4) | ? enforced | `UscfPairer` bye picker (HPB exclusion + escape) | `28L4` |
| [27A1](#rule-27a1) | Avoid players meeting twice (highest priority) | ? | `UscfPairer.HasPlayed` / `IsForbiddenPair` | `29B` (wrong ? `27A1`) |
| [27A2](#rule-27a2) | Equal scores | ? | `UscfPairer.PairRoundN` score-group enumeration | � |
| [27A3](#rule-27a3) | Upper half vs. lower half | ? | `UscfPairer.PairPool` SLIDE | `28C` (wrong ? `27A3` / `28J`) |
| [27A4](#rule-27a4) | Equalizing colors | ? | `UscfPairer.TopGetsWhite` + `TryReduceColorConflicts` | `29D1` (wrong ? `29E4 step 2`) |
| [27A5](#rule-27a5) | Alternating colors | ? | `UscfPairer.TopGetsWhite` step 3 | `29D2` (wrong ? `29E4 step 3`) |
| [28A](#rule-28a) | Pairing cards or program | ?? TD discretion | `SwissSysMapper` (data import) | � |
| [28B](#rule-28b) | Numbering late entrants | ?? TD discretion | � | � |
| [28C](#rule-28c) | Ratings of players | ?? TD discretion | `Player.Rating` field | � |
| [28C1](#rule-28c1) | Multiple US Chess ratings | ?? TD discretion | � | � |
| [28C2](#rule-28c2) | Foreign or FIDE ratings | ?? TD discretion | � | � |
| [28D](#rule-28d) | Players without US Chess ratings | ?? TD discretion | � | � |
| [28D1](#rule-28d1)�[28D7](#rule-28d7) | Unrated handling (verified / claimed / label / calculated / no info / improper) | ?? TD discretion | � | � |
| [28E](#rule-28e) | Assigned ratings for rated players | ?? TD discretion | � | � |
| [28E1](#rule-28e1)�[28E3](#rule-28e3) | Rating level / cause / notification | ?? TD discretion | � | � |
| [28F](#rule-28f) | Validity of wall-chart ratings | ?? TD discretion | � | � |
| [28G](#rule-28g) | Old ratings | ?? TD discretion | � | � |
| [28H](#rule-28h) | Revising ratings after tournament begins | ?? TD discretion | � | � |
| [28H1](#rule-28h1)�[28H3](#rule-28h3) | Removal / reassignment / refund | ?? TD discretion | � | � |
| [28I](#rule-28i) | Opponents of expelled players | ? deferred | � | � |
| [28I1](#rule-28i1)�[28I3](#rule-28i3) | Expulsion timing / extra rated games | ? deferred | � | � |
| [28J](#rule-28j) | The first round | ? | `UscfPairer.PairRoundOne` | `28C` (wrong ? `28J`) |
| [28K](#rule-28k) | Late entrants | ?? TD discretion | `AppendPlayer` + late-entrant numbering | � |
| [28L](#rule-28l) | Full-point byes (chapter umbrella) | ? | `UscfPairer.PairRoundN` bye picker | `28L` (over-general ? `28L2`) |
| [28L1](#rule-28l1) | Explanation and display | ? | `Round.Byes` + wall-chart rendering | � |
| [28L2](#rule-28l2) | Determination (who gets the bye) | ? | `UscfPairer.PairRoundN` bye picker | `28L` (over-general ? `28L2`) |
| [28L2a](#rule-28l2a) | Variation: bye to higher-rated for colour | ? planned | � | � |
| [28L3](#rule-28l3) | Players ineligible for full-point byes | ? enforced | bye picker (no double FPB + no FPB after forfeit win) | `28L3` |
| [28L4](#rule-28l4) | Full-point byes after half-point byes (? 22C6) | ? enforced | bye picker (HPB exclusion + "unless all others byed" escape) | `28L4` |
| [28L5](#rule-28l5) | New players in four-round events | ? planned | � | � |
| [28M](#rule-28m) | Alternatives to byes | ?? TD discretion | � | � |
| [28M1](#rule-28m1) | The house player | ?? TD discretion | � | � |
| [28M2](#rule-28m2)�[28M4](#rule-28m4) | Cross-round / cross-section / extra-rated alternatives | ? deferred | � | � |
| [28N](#rule-28n) | Combined individual-team tournaments | ? partial | `UscfPairer.ShareTeam` + plus-two escape | � |
| [28N1](#rule-28n1) | Plus-two method | ? enforced | `IsPlusTwoOrAbove` + `PairPool` two-pass | � |
| [28N2](#rule-28n2)�[28N4](#rule-28n4) | Variations (never-pair / point-threshold / TD discretion) | ?? TD discretion | � | � |
| [28O](#rule-28o) | Scoring | ?? TD discretion | `Round.PairingResult` + wall chart | � |
| [28O1](#rule-28o1) | Computer wall charts | ? | `WallChartViewModel` + `PdfReportBuilder` | � |
| [28P](#rule-28p) | Unplayed games | ?? TD discretion | `SetPairingResult` (Forfeit / Withdraw mutations) | � |
| [28Q](#rule-28q) | Pairing unfinished games | ?? TD discretion | � | � |
| [28Q1](#rule-28q1) | Modified Kashdan system | ? deferred | � | � |
| [28Q2](#rule-28q2) | Temporary adjudications | ?? TD discretion | � | � |
| [28R](#rule-28r) | Accelerated pairings in the first two rounds | ? deferred | � | � |
| [28R1](#rule-28r1)�[28R3](#rule-28r3) | Added score / adjusted rating / sixths | ? deferred | � | � |
| [28S](#rule-28s) | Reentries | ? deferred | � | � |
| [28S1](#rule-28s1)�[28S5](#rule-28s5) | Reentry rematch / colour / score-carryover rules | ? deferred | � | � |
| [28T](#rule-28t) | Variation: players may request a non-pairing | ?? TD discretion | `TournamentMutations.AddDoNotPair` | � |
| [29A](#rule-29a) | Score groups and rank | ? | `UscfPairer.PairRoundN` group enumeration | � |
| [29B](#rule-29b) | Order of pairing score groups | ? | `UscfPairer.PairRoundN` descending loop | � |
| [29C](#rule-29c) | Method of pairing score groups | ? | `UscfPairer.PairPool` | � |
| [29C1](#rule-29c1) | Upper half vs. lower half | ? | `UscfPairer.PairPool` SLIDE | � |
| [29C2](#rule-29c2) | Other adjustments (transpositions / interchanges) | ? | `UscfPairer.TryFindNonRematchMatching` + `TryCrossHalfInterchange` | `28L1` / `28L3` (wrong ? `29C2`) |
| [29D](#rule-29d) | The odd player | ? | `UscfPairer.PairRoundN` drop-selection | � |
| [29D1](#rule-29d1) | Determination | ? | `UscfPairer.PairRoundN` drop loop | `29C` (over-general ? `29D1a`) |
| [29D2](#rule-29d2) | Multiple drop downs | ? enforced | single-player-drop accumulator + forced-merge | `29D1a`/`27A1` |
| [29E](#rule-29e) | Color allocation (chapter umbrella) | ? | `UscfPairer.TopGetsWhite` + `TryReduceColorConflicts` | `29E` (over-general ? specific sub-rule) |
| [29E1](#rule-29e1) | Unplayed games (don't count for colour) | ? | `TopGetsWhite` ignores bye / forfeit cells | � |
| [29E2](#rule-29e2) | First-round colors | ? | `UscfPairer.PairRoundOne` initial-colour rule | `29E1` (wrong ? `29E2`) |
| [29E3](#rule-29e3) | Due colors in succeeding rounds | ? | `TopGetsWhite` due-colour logic | � |
| [29E3a](#rule-29e3a) | Due colors defined | ? | `TopGetsWhite` + `PreferredColor` | � |
| [29E4](#rule-29e4) | Equalization, alternation, and priority of color | ? | `TopGetsWhite` steps 1�5 | `29D1` / `29D2` / `29D` (wrong ? `29E4`) |
| [29E4a](#rule-29e4a) | Variation: priority based on plus/even/minus | ? planned | � | � |
| [29E4b](#rule-29e4b) | Variation: alternating priority | ? planned | � | � |
| [29E4c](#rule-29e4c) | Variation: priority based on lot (last round) | ?? TD discretion | � | � |
| [29E4d](#rule-29e4d) | Variation: priority based on rank (old rule) | ? deferred | � | � |
| [29E5](#rule-29e5) | Colors vs. ratings (umbrella) | ? enforced | `IsRatingCapCompliant` (80/200 caps) | `29E5` |
| [29E5a](#rule-29e5a) | **The 80-point rule** | ? enforced | `IsRatingCapCompliant` | `29E5a` |
| [29E5b](#rule-29e5b) | **The 200-point rule** | ? enforced | `IsRatingCapCompliant` | `29E5b` |
| [29E5b1](#rule-29e5b1) | Variation: 200pt for two-extra-blacks | ? planned | � | � |
| [29E5c](#rule-29e5c) | Evaluating transpositions (smaller of two diffs) | ? enforced | `IsRatingCapCompliant` | `29E5c` |
| [29E5d](#rule-29e5d) | Evaluating interchanges (one diff; prefer transposition) | ? partial | `TryReduceColorConflicts` two-pass (interchange pass gated off; rematch interchange live) | � |
| [29E5e](#rule-29e5e) | Comparing transpositions to interchanges | ? partial | interchange pass gated off | � |
| [29E5f](#rule-29e5f) | Colors in a series (no three in a row) | ? partial | `TopGetsWhite` (no hard cap; alternation only) | � |
| [29E5f1](#rule-29e5f1) | Variation: last-round exception | ?? TD discretion | � | � |
| [29E5g](#rule-29e5g) | Unrateds and color switches (exempt from 80/200) | ? enforced | `IsRatingCapCompliant` | `29E5g` |
| [29E5h](#rule-29e5h) | Variation: equalization priority over ratings | ?? TD discretion | � | � |
| [29E6](#rule-29e6) | Color adjustment technique | ? | `TryReduceColorConflicts` (branch-and-bound) | � |
| [29E6a](#rule-29e6a) | The Look Ahead method | ? partial | `TryReduceColorConflicts` minimises conflicts globally | � |
| [29E6b](#rule-29e6b) | Variation: the Top Down method | ? deferred | � | � |
| [29E7](#rule-29e7) | Examples of transpositions and interchanges | � (informational) | � | � |
| [29E8](#rule-29e8) | Variation: team pairings over colour equalization | ? planned | � | � |
| [29F](#rule-29f) | Last-round pairings with unfinished games | ?? TD discretion | � | � |
| [29G](#rule-29g) | Re-pairing a round | ?? TD discretion | `TournamentMutations.DeleteLastRound` + re-pair | � |
| [29G1](#rule-29g1)�[29G3](#rule-29g3) | About-to-start / already-started / selective re-pairing | ?? TD discretion | � | � |
| [29H](#rule-29h) | Unreported results | ?? TD discretion | `SetPairingResult` (TD picks an option) | � |
| [29H1](#rule-29h1)�[29H10](#rule-29h10) | Eleven options for unreported results | ?? TD discretion | � | � |
| [29I](#rule-29i) | Class pairings | ? deferred | � | � |
| [29I1](#rule-29i1)�[29I2](#rule-29i2) | Full / partial class pairings | ? deferred | � | � |
| [29J](#rule-29j) | Unrateds in class tournaments | ? deferred | � | � |
| [29K](#rule-29k) | Converting small Swiss to round robin | ? deferred (RR is a separate subsystem) | � | � |
| [29L](#rule-29l) | Using round robin table in small Swiss | ? deferred | � | � |
| [29L1](#rule-29l1) | Variation: 1 vs. 2 pairings | ? deferred | � | � |
| [29M](#rule-29m) | Recommendations (odd-round count preferred) | � (informational) | � | � |

---

## Chapter 22 � Unplayed games

The bye chapter. Brief on its own, but heavily cross-referenced from
28L (Full-point byes) and 28M (Alternatives to byes). 22B is essentially
a one-line statement that defers to 28L for who, when, and eligibility.
22C carries the half-point-bye policy detail (availability windows,
deadlines, last-round irrevocability).

### Rule 22A � Games forfeited due to nonappearance  <a id="rule-22a"></a>

**Status.** ?? TD discretion

**Plain statement.** A player who does not appear for a game, or
appears too late, gets **zero points**; the opponent gets **one
point**. On pairing sheets and wall charts the forfeit is circled or
marked **F**; computer wall charts may use **X** for the winner and
**F** for the loser.

**FreePair coverage today.** Forfeit recording is a TD action via
`TournamentMutations.SetPairingResult` with a `PairingResultKind` of
`WhiteForfeit` / `BlackForfeit` / `DoubleForfeit`. The rendered wall
chart shows the **F** marker per USCF convention. The engine treats
forfeit cells as **unplayed** for colour purposes (see
[29E1](#rule-29e1)). What constitutes "too late" is a separate rule
under chapter 13 and is the TD's call.

**See also.** [28P](#rule-28p) (unplayed games), [29E1](#rule-29e1)
(unplayed games don't count for colour).

---

### Rule 22B � Full-point byes  <a id="rule-22b"></a>

**Status.** ? enforced (defers to [28L](#rule-28l) for the algorithm)

**Plain statement.** If a round has an odd number of players and no
house player ([28M1](#rule-28m1)) is available to fill in, exactly
**one player** receives a full-point bye worth one point.

**FreePair coverage today.** The pairing engine detects an odd active
pool in `UscfPairer.PairRoundN` and assigns the bye through the
[28L2](#rule-28l2) bye picker. The bye is recorded on the
`Round.Byes` collection and posted as a circled `1` on the wall chart
(see [28L1](#rule-28l1)).

**Annotation today.** `PairingReason.ByeAssigned`, `UscfRule: "28L2"`
(Phase B narrowed from the chapter umbrella `"28L"`).

**See also.** [28L](#rule-28l) (umbrella), [28L2](#rule-28l2)
(determination), [28M](#rule-28m) (alternatives).

---

### Rule 22C � Half-point byes  <a id="rule-22c"></a>

**Status.** ?? TD discretion

**Plain statement.** For player convenience, the director **may**
allow half-point (�) byes for missed rounds. Whether they're offered
at all is the organizer's call, announced in pre-tournament publicity.

**FreePair coverage today.** Half-point byes are a per-player
TD-managed setting on `Player.RequestedByeRounds`. A player with an
HPB request for the upcoming round is removed from the active pairing
pool by `TournamentMutations.PairNextRound` *before* the engine runs,
so the engine never sees them. The wall chart shows `�` for the round
per USCF convention.

**See also.** [22C1](#rule-22c1) through [22C6](#rule-22c6) (policy
details), [28L4](#rule-28l4) (FPB-after-HPB precedence � verbatim
duplicate at [22C6](#rule-22c6)).

---

### Rule 22C1 � Availability  <a id="rule-22c1"></a>

**Status.** ?? TD discretion

**Plain statement.** Half-point byes may be offered in the **first
half** of a tournament, or for the **middle round** of an odd-round
event, with or without advance notice. Half-point byes in the **second
half** should be disclosed in pre-tournament publicity. Emergencies
may justify an exception.

**FreePair coverage today.** FreePair does not enforce a per-round
availability window � any round may be marked as a half-point bye
target by the TD. The expectation is that the TD honours their own
announced policy.

---

### Rule 22C2 � Deadline for bye requests  <a id="rule-22c2"></a>

**Status.** ?? TD discretion

**Plain statement.** Half-point bye requests should be made **at
least one hour before** the bye round, unless the director sets a
different cutoff.

**FreePair coverage today.** No timestamping of bye requests. The TD
records an HPB whenever they enter it; the rule is honoured by TD
practice, not by software gating.

---

### Rule 22C3 � Byes and class prizes  <a id="rule-22c3"></a>

**Status.** ?? TD discretion

**Plain statement.** **Recommendation:** if class prizes are likely
to be won with even or minus scores, half-point byes should be
unavailable or limited to one per player in such classes.

**FreePair coverage today.** No per-class HPB cap. TD enforces this
manually when accepting requests.

---

### Rule 22C4 � Irrevocable byes  <a id="rule-22c4"></a>

**Status.** ?? TD discretion

**Plain statement.** Half-point byes for the **final round** must be
declared **irrevocably** before the player begins their first game
(or second, if the organizer so announces). The deadline must appear
in pre-tournament publicity. **Recommendation:** treat *all*
second-half HPBs as irrevocable, and post the list of scheduled byes
near the wall charts.

**FreePair coverage today.** No irrevocability flag on
`Player.RequestedByeRounds`. The TD may add or remove an HPB at any
time; honoring 22C4 is a manual workflow.

---

### Rule 22C5 � Cancellation of irrevocable byes  <a id="rule-22c5"></a>

**Status.** ?? TD discretion

**Plain statement.** If the director agrees, a player **may** cancel
an irrevocable half-point bye � but if the player wins the resulting
game, the result is recorded as a **draw** for prize purposes (the
rating game still scores normally).

**FreePair coverage today.** Not modelled. Recording a "win-counted-
as-draw-for-prizes" exception would require dual-score tracking on
`PairingResult`, which the current schema does not support. TD
workaround: enter the result as a draw and document the rationale
elsewhere.

---

### Rule 22C6 � Full-point byes after half-point byes  <a id="rule-22c6"></a>

**Status.** ? enforced (verbatim duplicate of [28L4](#rule-28l4))

**Plain statement.** A full-point bye should **not** be assigned to a
player who has previously taken or committed to a half-point bye
**unless** all other players in the score group have already had a
bye or a no-show forfeit win. This rule appears in two places in the
USCF rule book � here as 22C6 and again as [28L4](#rule-28l4) � with
identical text. Treat them as one rule.

**FreePair coverage today.** The `UscfPairer.PairRoundN` bye-picker
now excludes **every** prior-bye holder from the preferred tiers via
`HadAnyBye` � which covers a prior full-point bye, a forfeit win, a
**taken** half-point bye in round history (`Result == 'H'`, previously
invisible to the picker), and a TD-scheduled bye. When no never-byed
player remains, the **escape clause** fires: the bye falls to the
lowest-rated half-point-bye holder, still honouring 28L3 (no second
full-point bye, no forfeit-win holder). This implements both halves
of 22C6 � the HPB-holder exclusion AND the "unless all others have
already had a bye" escape. The change is corpus-neutral: no corpus
round byes an HPB-holder, so the escape path is covered by unit tests
rather than the SwissSys-fidelity harness.

**See also.** [28L4](#rule-28l4) (the same rule under chapter 28).

---

## Chapter 27 � Swiss system fundamentals

The five priority rules that govern every pairing decision. Rule 27
states these in priority order � when two rules conflict, the lower-
numbered one wins, *except* for explicit overrides documented in 29E5
(colours vs. ratings) and 29E5f / 29E5h (colour-in-a-series and the
TD variation that lets equalization beat score-group integrity).

### Rule 27A1 � Avoid players meeting twice (highest priority)  <a id="rule-27a1"></a>

**Status.** ? enforced

**Plain statement.** A player may **not** play the same opponent
more than once in a tournament. This is the **single highest
priority** pairing rule and only yields when the number of rounds
equals or exceeds the number of players. Forfeit-only prior pairings
(opponent didn't appear) **do not** count as a prior game � those
two players **may** be re-paired.

**FreePair coverage today.** `UscfPairer.HasPlayed` reads the prior
rounds' opponent history. `IsForbiddenPair` calls `HasPlayed` and
gates every matcher (`TryFindNonRematchMatching`,
`TryCrossHalfInterchange`). When the natural slide creates rematches,
the cascade runs: bottom-half transpositions
([29C2](#rule-29c2)) ? cross-half interchanges
([29C2](#rule-29c2)) ? forced merges (entire pool floats down) ?
finally accepts the rematch as `FallbackRematchAccepted`. The
forfeit-exception clause is honoured by `HasPlayed` since forfeit
results are filtered out of the played-opponents set.

**Annotation today.** When a transposition or interchange resolves
the rematch: `TranspositionAvoidRematch` / `CrossHalfInterchange`.
When all else fails: `FallbackRematchAccepted`, `UscfRule: "27A1"`
(Phase B fixed the previously-emitted `"29B"`, which is "order of
pairing", not "no rematches").

**See also.** [29B](#rule-29b) (unrelated rule despite the name
collision in older citations), [29C2](#rule-29c2) (the swap mechanism),
[27A2](#rule-27a2) (equal scores � yields to 27A1).

---

### Rule 27A2 � Equal scores  <a id="rule-27a2"></a>

**Status.** ? enforced

**Plain statement.** Players with **equal scores are paired
whenever possible**. Pairings begin by sorting players into score
groups (highest first) and pairing each group internally.
Exceptions: accelerated pairings round 2 ([28R](#rule-28r), deferred),
and the 29E5f / 29E5h colour-priority overrides.

**FreePair coverage today.** `UscfPairer.PairRoundN` enumerates
score groups in descending score order; `PairPool` does the
top-vs-bottom slide *within* each group. Floaters move from higher
to lower score groups only � never the reverse (see [29B](#rule-29b)).

---

### Rule 27A3 � Upper half vs. lower half  <a id="rule-27a3"></a>

**Status.** ? enforced

**Plain statement.** Within a score group, the upper half by ranking
(rating) is paired against the lower half. Highest top-half player
meets highest bottom-half player, second-highest meets second-
highest, and so on (the "natural slide"). Subject to the
[29E5](#rule-29e5) overrides for colour.

**FreePair coverage today.** `UscfPairer.PairPool` builds the
top/bot arrays and pairs `top[i]` with `bot[i]`. The natural slide
is the default; transpositions and interchanges only fire when
forced by rematch ([27A1](#rule-27a1)), team avoidance, or colour
conflicts ([29E](#rule-29e)).

**Annotation today.** `NaturalSlide` for the unchanged slide;
`TranspositionAvoidRematch` / `CrossHalfInterchange` /
`ColorConflictReduction` when the slide gets disturbed.
`UscfRule: "28D"` for the round-1 natural pairing � wrong; Phase B
will correct to `"27A3"` (or `"28J"` for the round-1 specific case).

---

### Rule 27A4 � Equalizing colors  <a id="rule-27a4"></a>

**Status.** ? enforced

**Plain statement.** Players should receive each colour **the same
number of times** whenever practical, and **not the same colour
more than twice in a row**. In odd-round events the excess of one
colour over the other is limited to one. Equalization yields to
27A1�27A3 by default, but 29E5 may override the priority in specific
sub-cases.

**FreePair coverage today.** `UscfPairer.TopGetsWhite` applies
equalization as step 2 of its five-step colour priority (the full
list is documented in [29E4](#rule-29e4)). When a score group's
natural slide would create same-colour-due conflicts,
`TryReduceColorConflicts` searches for a transposition that reduces
the conflict count. The "no three in a row" cap is implicit in the
equalization logic rather than enforced as a hard rule � see
[29E5f](#rule-29e5f) for the gap.

**Annotation today.** `ColorEqualization`, `UscfRule: "29E4"`
(Phase B fixed the previously-emitted `"29D1"`, which is the odd-
player determination rule).

---

### Rule 27A5 � Alternating colors  <a id="rule-27a5"></a>

**Status.** ? enforced

**Plain statement.** Players should receive **alternating colours**
whenever practical. When a player has played equal whites and
blacks, the due colour is the opposite of what they had in the most
recent round. Yields to 27A1�27A4.

**FreePair coverage today.** `TopGetsWhite` step 3 alternation
check: when both players have equal colour totals, prefer giving
white to whoever was black most recently. The per-colour recency
tiebreaker (added on the ManualTesting branch � see commit
`3230c08`) handles the subtler case where both players were the
same colour last round.

**Annotation today.** `ColorAlternation`, `UscfRule: "29E4"`
(Phase B fixed the previously-emitted `"29D2"`, which is "multiple
drop downs").

---

## Chapter 28 � Swiss system pairings, procedures

The procedural backbone � pairing cards (28A), rating handling
(28C�28H), the first round (28J), bye assignment (28L), alternatives
to byes (28M), team avoidance (28N), scoring (28O), unplayed games
(28P), unfinished games (28Q), accelerated pairings (28R), and
reentries (28S). The pairing-relevant rules are 28J, 28L, 28M1, 28N.
Most of 28C�28H is rating-administration TD policy that FreePair
inherits from the imported SwissSys data.

### Rule 28A � Pairing cards or program  <a id="rule-28a"></a>

**Status.** ?? TD discretion

**Plain statement.** Before round 1, the TD prepares one pairing
card per player (or enters each into pairing software). The card
carries name, rating, US Chess ID, and team / school. Players are
ranked by rating descending; unrated players go to the bottom of
the group; each gets a **pairing number** used throughout the
tournament.

**FreePair coverage today.** SwissSys `.sjson` import handles
ranking and pairing-number assignment automatically (see
`SwissSysMapper`). The TD can adjust ratings and team codes through
the Players tab; the engine consumes the resulting `Section.Players`
collection.

---

### Rule 28B � Numbering late entrants  <a id="rule-28b"></a>

**Status.** ?? TD discretion

**Plain statement.** Late entrants get the next available
unassigned pairing number, asterisked to remind the TD that rating
(not pairing number) is what orders them within their score group.
A computer program may automatically reinsert them in proper order.

**FreePair coverage today.** Adding a player after pairing has
started is a TD action via the Players tab; FreePair assigns the
next sequential pairing number. The engine sorts by rating within
score groups, so late-entrant pairing numbers don't distort
pairings.

---

### Rule 28C � Ratings of players  <a id="rule-28c"></a>

**Status.** ?? TD discretion

**Plain statement.** The rating entered on a player's card is the
last-published US Chess rating from the rating list specified in
the tournament's pre-event publicity, unless the TD has assigned a
different rating ([28E](#rule-28e)). USCF crosstable or web ratings
*higher* than the last published rating are commonly accepted at
the TD's discretion.

**FreePair coverage today.** Rating is read from the imported
SwissSys file; the TD may edit it through the Players tab.

---

### Rule 28C1 � Multiple US Chess ratings  <a id="rule-28c1"></a>

**Status.** ?? TD discretion

**Plain statement.** When a player has been mistakenly issued more
than one US Chess rating, the TD should combine them weighted by
game count (worked examples in the rule book). The result is then
treated as the single rating for the tournament.

**FreePair coverage today.** Not modelled; the TD enters the
combined rating directly.

---

### Rule 28C2 � Foreign or FIDE ratings  <a id="rule-28c2"></a>

**Status.** ?? TD discretion

**Plain statement.** A player with a foreign / FIDE rating *and*
no established US Chess rating (or none published in two years, or
when the TD requests) **must** disclose it. Non-disclosure may
result in withholding of rating-based or unrated prizes, or refusal
of entry.

**FreePair coverage today.** Not enforced; rating disclosure is a
registration policy outside the software.

---

### Rule 28D � Players without US Chess ratings  <a id="rule-28d"></a>

**Status.** ?? TD discretion

**Plain statement.** Unrated players are eligible only for place
prizes and unrated prizes � *unless* an alternate rating-assignment
procedure (28D1�28D6) gives them a working rating.

**FreePair coverage today.** Prize eligibility is enforced by the
TD when configuring prize rules; the engine's role is to feed
correct ratings to the pairing logic.

---

### Rules 28D1�28D7 � Unrated handling family  <a id="rule-28d1"></a>  <a id="rule-28d2"></a>  <a id="rule-28d3"></a>  <a id="rule-28d4"></a>  <a id="rule-28d5"></a>  <a id="rule-28d6"></a>  <a id="rule-28d7"></a>

**Status.** ?? TD discretion

**Plain statement (summary).** Seven rules covering how to handle a
player without a published US Chess rating:

- **28D1** � verified non-US rating (FIDE / foreign / quick). Use it,
  optionally adjusted (Canada / Bermuda / Jamaica no adjustment; FIDE
  has three approved formulas; "most other nations" add 200; former
  Soviet / Philippines add 250; Brazil / Peru / Colombia are not
  eligible for sub-2200 class prizes due to historic unreliability).
- **28D2** � claimed but unverifiable non-US rating. May be assigned
  ([28E](#rule-28e)) but not below 2200 if class-prize-eligible.
- **28D3** � US Chess unofficial rating from label or printout (not
  yet in supplement). Use as-is; fewer than four games = unrated.
- **28D4** � TD calculates from prior US Chess results. May assign;
  if within 100 points of higher prize category, round up.
- **28D5** � TD assigns based on non-rated activity (club play,
  speed). Cannot be below 2200 if class-prize-eligible.
- **28D6** � no information available. Player is unrated, marked
  **NEW** on chart, not eligible for prizes based on assigned rating.
- **28D7** � improperly assigned rating violates 28D. If caught
  before prize award, the player loses eligibility based on it.

**FreePair coverage today.** Not modelled. The TD enters whatever
rating they decide and marks unrateds via `Player.Rating == 0`
which the engine treats as unrated for [28L2](#rule-28l2) bye
selection and [29D1](#rule-29d1) floater drops.

---

### Rule 28E � Assigned ratings for rated players  <a id="rule-28e"></a>

**Status.** ?? TD discretion

**Plain statement.** The TD **may** assign a rating to *any* rated
player. The assignment must not be **lower** than the player's last
published USCF (or foreign-equivalent) rating ([28E1](#rule-28e1)).
Causes ([28E2](#rule-28e2)) and notification rules
([28E3](#rule-28e3)) apply.

**FreePair coverage today.** Not modelled as a distinct workflow;
the TD edits `Player.Rating` directly.

---

### Rule 28E1 � Rating level  <a id="rule-28e1"></a>

**Status.** ?? TD discretion

**Plain statement.** An assigned rating must be ? the player's last
published US Chess rating (or its foreign equivalent).

---

### Rule 28E2 � Cause for assignment  <a id="rule-28e2"></a>

**Status.** ?? TD discretion

**Plain statement.** Reasonable cause includes: player has shown
significant superiority to class; player performs much better when
prizes are at stake; rating recently dropped due to statistically
unlikely results; player's behaviour in a prior tournament suggests
deliberate losing.

---

### Rule 28E3 � Notification  <a id="rule-28e3"></a>

**Status.** ?? TD discretion

**Plain statement.** Notify the player of an assigned rating in
advance if possible, so they can decide whether to enter. When the
cause arises during the event, this isn't always feasible.

---

### Rule 28F � Validity of wall-chart ratings  <a id="rule-28f"></a>

**Status.** ?? TD discretion

**Plain statement.** A properly-assigned rating that appears on the
wall chart **without disclaimer** is valid for both prizes and
pairing. To use an assigned rating for pairing but **not** prizes,
the TD posts a disclaimer next to the player's rating on the chart.

---

### Rule 28G � Old ratings  <a id="rule-28g"></a>

**Status.** ?? TD discretion

**Plain statement.** Old ratings of inactive players remain valid.
If the rating can't be located or confirmed, award class prizes
only after confirmation.

---

### Rule 28H � Revising ratings after tournament begins  <a id="rule-28h"></a>

**Status.** ?? TD discretion

**Plain statement.** For reasonable cause the TD may revise any
player's rating at any time. If the revision makes the player
ineligible for their current section, rules 28H1�28H3 apply.

---

### Rule 28H1 � Removal  <a id="rule-28h1"></a>

**Status.** ?? TD discretion

**Plain statement.** The player is removed from the section they
no longer qualify for.

---

### Rule 28H2 � Reassignment  <a id="rule-28h2"></a>

**Status.** ?? TD discretion

**Plain statement.** The TD may offer the player a slot in an
appropriate section, with half-point byes for the games missed.

---

### Rule 28H3 � Entry fee refund  <a id="rule-28h3"></a>

**Status.** ?? TD discretion

**Plain statement.** Refund policy depends on cause: if the player
caused the misassignment by providing false / misleading / incomplete
information, no refund is required; if the TD or staff made the
mistake, refund all or part of the fee (proportional to how many
rounds the player has lost prize chances on).

---

### Rule 28I � Opponents of expelled players  <a id="rule-28i"></a>

**Status.** ? deferred

**Plain statement.** When a player is removed from the section
because of a corrected rating ([28H](#rule-28h)), their prior
opponents' results need adjusting per 28I1�28I3.

**FreePair coverage today.** Not modelled. Withdrawals are handled
generically (subsequent rounds scored zero); the specific "expelled
mid-event with results adjustment" workflow is deferred.

---

### Rule 28I1 � Expulsion before last round paired  <a id="rule-28i1"></a>

**Status.** ? deferred

**Plain statement.** Use the same procedure as 28I2.

---

### Rule 28I2 � Expulsion after last round paired  <a id="rule-28i2"></a>

**Status.** ? deferred

**Plain statement.** Adjust earlier-opponent scores: a player who
**lost** to the expelled player gets a **half-point bye** instead; a
player who **drew** gets a **win by forfeit**.

---

### Rule 28I3 � Extra rated games  <a id="rule-28i3"></a>

**Status.** ? deferred

**Plain statement.** The actual results of each opponent vs. the
expelled player are transferred to an "extra rated games" chart for
US Chess rating purposes ([28M4](#rule-28m4)).

---

### Rule 28J � The first round  <a id="rule-28j"></a>

**Status.** ? enforced

**Plain statement.** Flip a coin to decide whether the highest- or
lowest-rated player on board 1 receives **white**. Order all
players by rating descending, split into two equal-size groups (top
half / bottom half), pair `top[i]` with `bot[i]`. Colours alternate
down through each half. If there's an odd number, the lowest-rated
**rated** player (not an unrated player) receives a one-point bye
([28L](#rule-28l)).

**FreePair coverage today.** `UscfPairer.PairRoundOne` implements
exactly this. The initial-colour decision uses the tournament's
configured first-board colour (per-section, since one coin toss
decides all sections per the rule book's TD TIP).

**Annotation today.** `RoundOneSlide`, `UscfRule: "28J"` (Phase B
fixed `"28C"` which is "ratings of players"). Colour annotation:
`ColorByInitialRule`, `UscfRule: "29E2"` (Phase B fixed `"29E1"` which
is "unplayed games").

---

### Rule 28K � Late entrants  <a id="rule-28k"></a>

**Status.** ?? TD discretion

**Plain statement.** TDs may accept late entrants past the
announced closing time. The late entrant either **forfeits** the
missed round (if the round cannot be paired in time) or takes a
**half-point bye** for that round (if HPBs are offered).

**FreePair coverage today.** Adding a player mid-event is a TD
action through the Players tab; FreePair offers both forfeit-loss
and half-point-bye paths for the missed round.

---

### Rule 28L � Full-point byes (chapter umbrella)  <a id="rule-28l"></a>

**Status.** ? enforced

**Plain statement.** This is the chapter header that introduces the
five bye-assignment rules 28L1�28L5. The substantive rules are
[28L2](#rule-28l2) (who gets it) and [28L3](#rule-28l3) /
[28L4](#rule-28l4) (who's ineligible).

**FreePair coverage today.** The engine now cites the specific
sub-rule per Phase B � `"28L2"` for the picker (was `"28L"`). The
`"28L3"` ineligibility check has landed (forfeit-win recipients are
now disqualified from the full-point bye); the `"28L4"` "unless all
others have already had a bye" escape clause is still pending.

---

### Rule 28L1 � Explanation and display  <a id="rule-28l1"></a>

**Status.** ? enforced

**Plain statement.** In a round with an odd number of players, one
player gets a full-point bye worth one point. The bye is **posted
as a win on the wall chart but circled** to indicate it wasn't
played; computer wall charts may simply print the word "bye". This
rule defines *how byes are displayed*, **not** how they're chosen
(that's [28L2](#rule-28l2)).

**FreePair coverage today.** `Round.Byes` carries the bye records;
wall-chart rendering (`WallChartViewModel` + `PdfReportBuilder`)
prints the result in the circled-win convention.

---

### Rule 28L2 � Determination (who gets the bye)  <a id="rule-28l2"></a>

**Status.** ? enforced

**Plain statement.** Round 1: lowest-rated **rated** player (not
unrated, not late entrant). Subsequent rounds: lowest-rated player
**in the lowest score group** (also not unrated). If no rated
player is eligible in the lowest group, fall back to an unrated
player who has played in a US Chess-rated event too recently to
have a published rating. If even that fails, a new player gets the
bye and is marked **NEW** on the pairing card / wall chart.

**FreePair coverage today.** Two paths in `UscfPairer`:

- `SelectPreAssignedBye` (early): when the natural lowest of the
  lowest score group is ineligible (prior bye, scheduled bye, etc.),
  pre-assign the next eligible candidate so downstream pairing
  doesn't have to re-balance.
- Last-group fallback in `PairRoundN`: four-tier preference order
  `rated + no prior + no scheduled` ? `no prior + no scheduled` ?
  `no prior` ? `absolute lowest`.

**Annotation today.** `ByeAssigned`, `UscfRule: "28L2"`
(Phase B narrowed from the chapter umbrella `"28L"`).

**See also.** [28L2a](#rule-28l2a) (variation), [28L3](#rule-28l3)
(ineligibility), [28L4](#rule-28l4) (FPB-after-HPB).

---

### Rule 28L2a � Variation: bye to higher-rated for colour  <a id="rule-28l2a"></a>

**Status.** ? planned

**Plain statement.** **Unannounced variation:** give the bye to a
**higher-rated** player if doing so improves the overall colour
allocation for the lowest score group, **subject to the 80-point and
200-point limits** in [29E5a](#rule-29e5a) / [29E5b](#rule-29e5b).

**FreePair coverage today.** Not implemented. The bye picker always
prefers the lowest eligible rated player. Phase C will land this
alongside the 80/200-point work since the variation depends on those
rules being enforced.

---

### Rule 28L3 � Players ineligible for full-point byes  <a id="rule-28l3"></a>

**Status.** ? enforced

**Plain statement.** Two ineligibility conditions:

1. A player must **not** be given a full-point bye **more than
   once** in the event.
2. A player who has **won an unplayed game** because the opponent
   failed to appear should **not** be given a full-point bye on
   top of that.

The rule book's TD TIP explicitly warns that *not all pairing
software enforces this automatically* � TDs are advised to check
each round.

**FreePair coverage today.** Both conditions are honoured in the
bye picker. The first via the `HasReceivedFullPointBye` filter; the
second via the `HasReceivedForfeitWin` filter, which disqualifies a
forfeit-win recipient from also receiving a full-point bye. The bye
is selected (and removed from the pool) before the rest of the
score group is paired, so an eligibility shift never double-books a
player. Citation is now `"28L3"`.

---

### Rule 28L4 � Full-point byes after half-point byes  <a id="rule-28l4"></a>

**Status.** ? enforced (verbatim duplicate of [22C6](#rule-22c6))

**Plain statement.** A full-point bye should **not** be awarded to
a player who has previously taken or committed to a half-point bye,
**unless all others in the score group have already had a bye or a
no-show forfeit win**. The rule book's TD TIP again warns TDs to
verify their software enforces this.

**FreePair coverage today.** Implemented in the `UscfPairer.PairRoundN`
bye picker. The preferred tiers exclude every prior-bye holder via
`HadAnyBye` (prior full-point bye, forfeit win, **taken** half-point
bye in round history, or TD-scheduled bye); `HasTakenHalfPointBye`
closes the gap where a half-point bye recorded in a played round was
previously invisible (only scheduled byes were checked). The escape
clause is explicit: when no never-byed player remains, the bye falls
to the lowest-rated half-point-bye holder, still excluding 28L3
ineligibles (no second full-point bye, no forfeit-win holder). The
annotation distinguishes the normal case from the escape case. The
change is corpus-neutral (no corpus round byes a half-point-bye
holder); the escape path is covered by unit tests.

**See also.** [22C6](#rule-22c6) (identical text under chapter 22),
[28L3](#rule-28l3) (the other half of ineligibility).

---

### Rule 28L5 � New players in four-round events  <a id="rule-28l5"></a>

**Status.** ? planned

**Plain statement.** In a **four-round event**, if only **new
players** are available for byes in the bottom score group, the TD
**may** give the bye to a player **one score group above** instead
� but should not do so if the bye recipient has a substantial
chance of winning a prize. Using [28M](#rule-28m) alternatives is
preferable to bye-ing a new player.

**FreePair coverage today.** Not implemented. The bye picker
prefers lowest rated regardless of "new player" status (FreePair
doesn't distinguish "new player" from "unrated"). Phase C will
require modelling `Player.IsNewPlayer` (?4 career games per
[28D3](#rule-28d3) / [28D4](#rule-28d4)) and a 4-round-event check.

---

### Rule 28M � Alternatives to byes  <a id="rule-28m"></a>

**Status.** ?? TD discretion

**Plain statement.** Byes deprive a player of an expected game.
Four alternatives can avoid them: house player ([28M1](#rule-28m1)),
cross-round pairing ([28M2](#rule-28m2)), cross-section pairing
([28M3](#rule-28m3)), and extra rated games ([28M4](#rule-28m4)).

**FreePair coverage today.** House player is supported as a
TD-managed permanent player (see [28M1](#rule-28m1)); the other
three alternatives are deferred.

---

### Rule 28M1 � The house player  <a id="rule-28m1"></a>

**Status.** ?? TD discretion

**Plain statement.** A spectator (or a permanently-designated
non-prize-eligible player) may volunteer to play whoever would
otherwise get the bye. The house player is **paired normally when
the field is odd**, not paired when even, and may take half-point
byes themselves. The "permanent house player" version is
recommended over per-round volunteers. A US Chess-rated commercial
computer may be used as a house player only if announced in advance
(rule 36C).

**FreePair coverage today.** The TD adds a player and may flag them
as house-player-only via a notes convention; per-round inclusion is
handled by adding / removing them from the active pool. There's no
first-class `IsHousePlayer` flag � Phase C consideration.

---

### Rules 28M2 / 28M3 / 28M4 � Cross-round / cross-section / extra-rated alternatives  <a id="rule-28m2"></a>  <a id="rule-28m3"></a>  <a id="rule-28m4"></a>

**Status.** ? deferred

**Plain statement.**

- **28M2 cross-round pairing.** The player expecting a bye waits
  for a low-board game to finish; the loser plays the bye-player as
  *their* next round's game early. The pairing counts in the
  current round for one player and next round for the other.
- **28M3 cross-section pairing.** When multiple sections each have
  a bye, the lower-section bye-player retains the bye while playing
  a rated game against the higher-section bye-player. The
  higher-section player's game counts for score *and* rating; the
  lower-section player's bye stands.
- **28M4 extra rated games.** A separate "extra rated games"
  section reports games to US Chess for rating without affecting
  any tournament's standings.

**FreePair coverage today.** Not modelled. These are
multi-section / multi-round TD workflows that would require
significant `TournamentMutations` and reporting extensions.

---

### Rule 28N � Combined individual-team tournaments  <a id="rule-28n"></a>

**Status.** ? partial

**Plain statement.** Scholastic Swiss events often combine
individual and team awards. Players are paired individually; team
standings come from summing the top *N* finishers per school. The
TD **should try to avoid pairing teammates** against each other,
but an absolute prohibition can unfairly advantage strong-team
players. The [28N1](#rule-28n1) plus-two method codifies a graceful
approach.

**FreePair coverage today.** `UscfPairer.ShareTeam` provides team
avoidance: when the "Avoid pairing players from the same team"
TD setting is on, teammates are treated as forbidden pairs in the
same cascade as rematches (transposition ? interchange ? forced
merge) **below plus-two**. At plus-two or above the
[28N1](#rule-28n1) score-threshold escape applies, so an
unavoidable teammate pairing is accepted in-group rather than
distorting the pairings.

**Annotation today.** `TranspositionAvoidSameTeam` when the
avoidance fires a swap; `SameTeamPairingAccepted` (`UscfRule:
"28N1c"`) when a plus-two teammate pairing is accepted; otherwise
`UscfRule: "29C2"` (Phase B fixed `"28L1"`, which is "bye
explanation and display").

---

### Rule 28N1 � Plus-two method  <a id="rule-28n1"></a>

**Status.** ? enforced

**Plain statement.** Three sub-rules:

- **(a)** <a id="rule-28n1a"></a>If a score group **can** be paired internally without
  teammates facing each other, **always do so**.
- **(b)** <a id="rule-28n1b"></a>For score groups **below plus-two** (less than two more
  wins than losses), if no teammate-free pairing exists, **raise
  or lower** teammates into the nearest appropriate score group.
- **(c)** <a id="rule-28n1c"></a>For score groups **at plus-two or above**, **do not**
  remove players from their score group just to avoid teammates.

**FreePair coverage today.** Implemented (Phase C). Same-team
avoidance is the 28N1 preference, not an absolute. A teammate-free
pairing is always preferred (28N1a); below plus-two an unavoidable
teammate pairing is escaped by floating into the nearest group
(28N1b); at plus-two or above the teammate pairing is accepted
in-group rather than removing players from the group (28N1c). The
plus-two test is `2*score - roundsElapsed >= 2`; an accepted
teammate board is tagged `SameTeamPairingAccepted` (cite `28N1c`).
Gated behind a per-group `allowSameTeam` flag (off below plus-two
and for non-team events), so it is corpus-neutral.

---

### Rules 28N2 / 28N3 / 28N4 � Variations  <a id="rule-28n2"></a>  <a id="rule-28n3"></a>  <a id="rule-28n4"></a>

**Status.** ?? TD discretion

**Plain statement.**

- **28N2** � never pair teammates *unless* in the last round, the
  leader would otherwise have to play down a score group.
- **28N3** � modify 28N1's plus-two threshold to a different score.
- **28N4** � TD decides per-pairing when to allow teammate matches
  to maximize fairness.

**FreePair coverage today.** Not exposed as configurable variations.
The TD must work around them by toggling team avoidance on / off
per round or by manually forcing pairings.

---

### Rule 28O � Scoring  <a id="rule-28o"></a>

**Status.** ?? TD discretion

**Plain statement.** The TD records game results on pairing cards
(or in software) and posts them on wall charts as quickly as
possible.

**FreePair coverage today.** `Round.PairingResult` carries the
result; `WallChartViewModel` renders it.

---

### Rule 28O1 � Computer wall charts  <a id="rule-28o1"></a>

**Status.** ? enforced

**Plain statement.** Computer-generated wall charts may print
updated charts each round, saving time on colour / opponent entry.
**Recommendation:** still update scores manually as soon as possible
� don't make players wait hours for computer-aggregated chart
updates.

**FreePair coverage today.** Live wall-chart updates and on-demand
PDF export via `PdfReportBuilder.WallChart`.

---

### Rule 28P � Unplayed games  <a id="rule-28p"></a>

**Status.** ?? TD discretion

**Plain statement.** A player who fails to appear within **one
hour** of round start (or by the end of the first time control,
whichever comes first) gets a **forfeit loss** and the opponent
gets a **forfeit win**. The player is dropped from the tournament
unless they present an acceptable excuse; subsequent games are
scored zero. A player may also withdraw by notifying the TD, in
which case remaining games are scored zero. **Unplayed games are
not US Chess rated** � but a game in which both players make moves
*is* rated even if it ends in time-forfeit, and is **not** marked
with an **F**.

**FreePair coverage today.** Forfeit and withdrawal handled by
`TournamentMutations.SetPairingResult` (Forfeit) +
`SetPlayerWithdrawn`. Subsequent rounds for a withdrawn player are
not paired (player is removed from the active pool); historical
scores are preserved.

---

### Rule 28Q � Pairing unfinished games  <a id="rule-28q"></a>

**Status.** ?? TD discretion

**Plain statement.** Finish all games before pairing the next round
when possible. When not possible, the TD has two main options:
[28Q1](#rule-28q1) (Modified Kashdan) and [28Q2](#rule-28q2)
(temporary adjudication).

**FreePair coverage today.** Not modelled as a workflow; the TD
manually enters a placeholder result and adjusts later.

---

### Rule 28Q1 � Modified Kashdan system  <a id="rule-28q1"></a>

**Status.** ? deferred

**Plain statement.** Director instructs the player on move to seal
(rule 18A) and announces: any player who offers a draw before next-
round pairings begin will be paired as having drawn; otherwise paired
as having won.

---

### Rule 28Q2 � Temporary adjudications  <a id="rule-28q2"></a>

**Status.** ?? TD discretion

**Plain statement.** Adjourn the unfinished game; pair both players
based on a TD-assigned tentative result (draw / win+loss /
win+draw if winning chances are asymmetric). Consult strong
unaffected players if needed.

**FreePair coverage today.** TD enters the tentative result
manually.

---

### Rule 28R � Accelerated pairings in the first two rounds  <a id="rule-28r"></a>

**Status.** ? deferred

**Plain statement.** In tournaments where the player count far
exceeds 2 raised to the round-count, multiple perfect scores are
likely. Accelerated pairings effectively add one or two extra
rounds without playing them, by temporarily inflating top-half
scores during the first two rounds. Most effective in one-section
events or mixed-class fields.

**FreePair coverage today.** Not implemented. The user
explicitly deferred this work in the Phase A discussion. TDs who
need accelerated pairings can edit player rounds manually or use
SwissSys for round 1�2 then import.

---

### Rules 28R1 / 28R2 / 28R3 � Methods  <a id="rule-28r1"></a>  <a id="rule-28r2"></a>  <a id="rule-28r3"></a>

**Status.** ? deferred

**Plain statement.**

- **28R1 added score method** � mentally add 1 point to top-half
  scores for rounds 1 and 2, pair normally.
- **28R2 adjusted rating method** � more elaborate; divide into
  quarters A1/B1/C1/D1, pair A1�B1 and C1�D1 round 1, then regroup
  using winners/non-winners with 100-point bonuses for draws.
- **28R3 (variation) sixths** � same principle on six sub-groups
  instead of four; for events with a very small round-to-player
  ratio.

---

### Rule 28S � Reentries  <a id="rule-28s"></a>

**Status.** ? deferred

**Plain statement.** Tournaments with alternate starting schedules
(e.g. 3-day vs 2-day merging at round 2) often allow a player who
lost or drew early to "reenter" the later-starting schedule, with
the earlier games abandoned or treated as byes. Sub-rules 28S1�28S5
govern rematches, colours, score carryover.

**FreePair coverage today.** Not modelled. FreePair treats each
section as a single linear sequence of rounds with no multi-schedule
merging. TDs running reentries today work around this by
post-processing or manual data entry.

---

### Rules 28S1 / 28S2 / 28S3 / 28S4 / 28S5 � Reentry handling  <a id="rule-28s1"></a>  <a id="rule-28s2"></a>  <a id="rule-28s3"></a>  <a id="rule-28s4"></a>  <a id="rule-28s5"></a>

**Status.** ? deferred

**Plain statement.**

- **28S1** � a reentry vs. a non-reentry counts as the same
  player; do not re-pair them (rule [27A1](#rule-27a1)).
- **28S2** � two reentries who previously faced each other while
  playing their *original* entries **may** be paired again (each
  side is a new entry from the other's perspective).
- **28S3** � reentries are treated as having **no colour history**;
  the original entry's colours are disregarded.
- **28S4** � half-point byes may stand in for missed games when
  reentering the same schedule; 28S1/S2/S3 still apply.
- **28S5** � by default, the reentry carries the *better* (or
  *best*, if multiple reentries) score forward; the organizer may
  declare in advance that only the latest score counts.

---

### Rule 28T � Variation: players may request a non-pairing  <a id="rule-28t"></a>

**Status.** ?? TD discretion

**Plain statement.** Individual players may request that they **not
be paired** against each other in any tournament. The TD may be
unable to honour the request when pairing constraints make it
impossible.

**FreePair coverage today.** `TournamentMutations.AddDoNotPair`
records a player-pair constraint that the TD can apply at any
point. Today this is honoured by `IsForbiddenPair` (same cascade
as rematch); per the existing doc, full annotation-level
recognition is a planned Phase C item (`PairingReason.DoNotPair` is
declared but not yet emitted).

---

## Chapter 29 � Swiss system pairings, subsequent rounds

The pairing chapter for rounds 2+. Covers score-group structure
(29A�29C), odd-player handling (29D), colour allocation (29E with
its rich 29E5 sub-family), and TD-discretion situations: last-round
unfinished games (29F), re-pairing (29G), unreported results (29H),
class pairings (29I), small Swisses (29K�29L), and overall
recommendations (29M).

The **29E5 family** is the heart of correct USCF pairing � it
defines the 80-point and 200-point rating-difference limits on
colour-driven swaps, the evaluation rules for transpositions vs
interchanges, and the no-three-in-a-row series rule. Several of
these are currently ? planned in FreePair; closing them is the
top Phase C priority.

### Rule 29A � Score groups and rank  <a id="rule-29a"></a>

**Status.** ? enforced

**Plain statement.** A **score group** is the set of players with
the same score (even if there's only one). Players in each group
are paired against each other ([27A2](#rule-27a2)) unless they've
already played ([27A1](#rule-27a1)), are odd players ([29D](#rule-29d)),
or must play floaters from another group. Combined individual-team
tournaments may pair across groups to avoid teammates
([28N](#rule-28n)). **Rank** is determined first by score (higher =
higher rank) and then by rating within a score group. *Score group
determines rank when players are paired outside their score group*
� this matters for colour decisions when a floater meets a
lower-group player.

**FreePair coverage today.** `UscfPairer.PairRoundN` groups by
score (descending) and ranks within each by rating (descending).
The cross-group rank rule is honoured implicitly because
`TopGetsWhite` uses `ComputeScore` rather than pool position.

---

### Rule 29B � Order of pairing score groups  <a id="rule-29b"></a>

**Status.** ? enforced

**Plain statement.** Pair score groups in **descending rank order**
(highest first, lowest last). If late-round games in some groups
are still unfinished, the TD may pair around them and come back �
taking care to provide for any odd players. **Floaters always move
down**, never up.

**FreePair coverage today.** `UscfPairer.PairRoundN` walks
`scoreGroups` (built in descending order) once; the `floatDown`
accumulator carries floaters forward only. The "pair around
unfinished games" workflow is a TD action (manual edit + repair),
not engine-driven.

**See also.** Phase B fixed the previously-emitted `"29B"` citation
for the no-rematch fallback. The correct citation is [27A1](#rule-27a1),
which the engine now emits.

---

### Rule 29C � Method of pairing score groups  <a id="rule-29c"></a>

**Status.** ? enforced

**Plain statement.** This is the chapter umbrella that introduces
the two substantive sub-rules: [29C1](#rule-29c1) (upper vs lower
half) and [29C2](#rule-29c2) (other adjustments � transpositions
and interchanges).

**FreePair coverage today.** `UscfPairer.PairPool` implements both.

---

### Rule 29C1 � Upper half vs. lower half  <a id="rule-29c1"></a>

**Status.** ? enforced

**Plain statement.** If a score group has an even number of
players, order them by rank, divide in half, and pair upper half
against lower half **in consecutive order** � top-of-upper plays
top-of-lower, etc. (In a group of 20, player 1 plays player 11,
player 2 plays player 12, and so on.)

**FreePair coverage today.** `UscfPairer.PairPool` SLIDE
implementation. Identical to [27A3](#rule-27a3) � 29C1 is the
chapter-29 restatement for round 2+ contexts.

---

### Rule 29C2 � Other adjustments  <a id="rule-29c2"></a>

**Status.** ? enforced

**Plain statement.** Transpositions (swap within the upper or
lower half) **are made** to:

1. Avoid pairing players who have already played ([27A1](#rule-27a1)).
2. Give as many players as possible their equalizing or due colours
   ([29E](#rule-29e), [29E5](#rule-29e5)).

An **interchange** (swap between bottom-of-upper and top-of-lower)
is also permissible. Both are subject to the 29E5a/29E5b
rating-difference limits when driven by colour.

**FreePair coverage today.** `TryFindNonRematchMatching` handles
the no-rematch transpositions (single-swap and multi-swap exhaustive
search); `TryCrossHalfInterchange` handles the interchange when no
bottom-half-only solution works. `TryReduceColorConflicts` handles
the colour-driven swap search. **None of these enforce the 29E5
rating-difference limits** � see [29E5a](#rule-29e5a) /
[29E5b](#rule-29e5b) / [29E5c](#rule-29e5c) for the gap.

**Annotation today.** `TranspositionAvoidRematch` /
`CrossHalfInterchange` / `ColorConflictReduction` /
`TranspositionAvoidSameTeam`. Phase B fixed all four to cite
`"29C2"` (previously `"28L1"` / `"28L3"` / `"29E"` respectively,
which were either bye rules or chapter umbrellas).

---

### Rule 29D � The odd player  <a id="rule-29d"></a>

**Status.** ? enforced

**Plain statement.** Often some players can't be paired within
their score group � guaranteed when the group is odd, possible
when players have already faced each other or are otherwise
restricted (teammates, do-not-pair, etc.). At least one player
**floats down** to a lower group. The first priority (after
avoiding restricted pairings) is to keep players as close to their
score group as possible.

**FreePair coverage today.** `UscfPairer.PairRoundN` runs a
drop-selection loop that picks the floater per [29D1](#rule-29d1)
and carries them into the next group via `floatDown`. When the
*entire* lower group is unpairable (rematches exhaust all swaps),
the engine triggers a **forced merge** that floats the whole pool
down.

---

### Rule 29D1 � Determination  <a id="rule-29d1"></a>

**Status.** ? enforced

**Plain statement.** Three sub-rules:

- **(a) Default.** The **lowest-rated rated** player (not unrated)
  is treated as the odd player and paired against the highest-rated
  player in the next lower group they haven't played. Verify the
  remaining members can still be paired, the floater hasn't already
  played the next group's whole roster, and the colour consequences
  are acceptable.
- **(b) When (a) fails.** Try the next-lowest-rated player as the
  odd player, *or* pair the odd player against a lower-ranked
  player in the next group. **When deciding which switch to make**,
  consider **only** the rating difference of the players being
  switched. **There is no rating limit** on switches needed to
  keep score groups intact � however, switches made to **correct
  colours** must stay within the [29E5](#rule-29e5) limits.
- **(c) All-unrated group.** When the entire score group is unrated,
  an unrated player **must** be designated as the floater.

**FreePair coverage today.** `UscfPairer.PairRoundN` drop loop
implements (a) by default. The drop selection considers candidates
in order `rated lowest ? next rated ? unrated last`, runs each
through `TryReduceColorConflicts` to compute the achievable colour
balance, and prefers the natural drop unless ?2 conflicts could be
eliminated by an alternative drop. This implements (a) and a
colour-friendly variant of (b); the **pure-keep-groups-intact
escape with no rating limit** is implicitly honoured because the
matchers themselves don't enforce rating limits. (c) is honoured
because unrateds participate in the drop loop.

**Annotation today.** `FloaterDropNatural` or
`FloaterDropColorFriendly`, `UscfRule: "29D1a"` (for natural) / `"29E5"`
(for colour-friendly). Phase B narrowed both from the over-general
`"29C"` / `"29E"` umbrellas.

**See also.** [29E5](#rule-29e5) (the cited rating-limit
exception), [29D2](#rule-29d2) (multi-group drops).

---

### Rule 29D2 � Multiple drop downs  <a id="rule-29d2"></a>

**Status.** ? enforced

**Plain statement.** Sometimes the floater must jump **multiple
score groups** to find a valid opponent. Rule preferences:

- A pairing that drops a player *one or more* groups is **preferred
  over** one that drops *two or more* players one or more groups.
  (Can be relaxed in low score groups for legal pairing.)
- The floater is normally paired against the highest-rated player
  they haven't met from the **next lower group**. Pairing against a
  somewhat-lower-rated player is acceptable for colour reasons but
  only within [29E5](#rule-29e5) transposition rules.

Three worked-example patterns: one odd player who's played the
whole next group; two odd players in group 1 who've already met;
two odd players from separate groups (the higher-score floater is
paired first).

**FreePair coverage today.** The drop design already honours 29D2's
primary preference. Each odd score group drops exactly **one**
player (the lowest-rated, per 29D1a) into a `floatDown` accumulator;
that single floater is merged into the next group, and if it still
cannot be paired rematch-free it **re-floats hop-by-hop**, so a lone
floater can jump multiple groups while only one player ever drops.
The two-odd-players-have-already-met pattern is handled by the
forced-merge path (USCF 27A1), which floats the residual group only
on a genuine rematch deadlock. This is verified by the regression
test `Floater_drops_multiple_score_groups_when_next_group_is_all_rematches`
(a lone 2.0 floater that has played the whole 1.0 group drops all the
way into the 0.0 group). The remaining nuance � forced-merge floating
a whole residual group is slightly broader than the rule strictly
requires � only fires when no legal rematch-free pairing exists, so
it does not violate the preference in practice.

---

### Rule 29E � Color allocation (chapter umbrella)  <a id="rule-29e"></a>

**Status.** ? enforced (umbrella; sub-rules vary)

**Plain statement.** The TD assigns colours to all players. The
objective:

- In a tournament with an **even number of rounds**: give each
  player white and black the **same number of times** whenever
  possible.
- In a tournament with an **odd number of rounds**: each player
  should receive **no more than one extra** white or black above
  even allocation.

Besides equalization, after round 1 the TD tries to **alternate
colours**, giving as many players as possible their *due* (correct
or expected) colour round by round. Due colour is **usually** the
opposite of the most recent round � but not always. Example: a
player with WWB has due colour **black** in round 4 (equalization
priority over alternation).

**FreePair coverage today.** `TopGetsWhite` implements the
full colour-decision logic across [29E2](#rule-29e2) through
[29E4](#rule-29e4); `TryReduceColorConflicts` implements
[29E6a](#rule-29e6a)-style global conflict minimisation. The 29E5
rating-difference limits are the major gap.

**Annotation today.** Eight colour-related `PairingReason` values
emit specific `UscfRule:` strings post-Phase B � `"29E2"` for first-
round colour, `"29E4"` for the four-rule equalization/alternation/
rating priority chain, `"29E5"` for colour-conflict reduction, and
`"29E5f"` for the no-three-in-a-row absolute. See the individual
rule entries for which `PairingReason` maps to which citation.

---

### Rule 29E1 � Unplayed games  <a id="rule-29e1"></a>

**Status.** ? enforced

**Plain statement.** **Unplayed games**, including byes and
forfeits, **do not count for colour**.

**FreePair coverage today.** `TopGetsWhite` filters out cells with
no actual played colour when computing balance. Bye / scheduled-bye
/ forfeit-win / forfeit-loss / zero-point-bye are all excluded from
`whites` / `blacks` totals.

**Annotation today.** Implicit � no separate annotation emits. The
existing `ColorByInitialRule` annotation incorrectly cites `"29E1"`
when it should cite [29E2](#rule-29e2).

---

### Rule 29E2 � First-round colors  <a id="rule-29e2"></a>

**Status.** ? enforced

**Plain statement.** After the coin toss decides who plays white
on board 1 (rule [28J](#rule-28j)), all top boards in **all
sections** are assigned white based on that single coin toss. Other
players in each section are assigned alternating colours per
[28J](#rule-28j).

**FreePair coverage today.** `UscfPairer.PairRoundOne` honours the
per-section initial-colour setting and alternates down through each
half.

**Annotation today.** `ColorByInitialRule`, `UscfRule: "29E2"`
(Phase B fixed the previously-emitted `"29E1"`, which is the
"unplayed games" rule).

---

### Rule 29E3 � Due colors in succeeding rounds  <a id="rule-29e3"></a>

**Status.** ? enforced

**Plain statement.** As many players as possible are given their
**due colour** each round, **so long as the pairings conform to
the basic Swiss system rules** (27A1�27A5 priority).

**FreePair coverage today.** `TopGetsWhite` chooses the due-colour
recipient; `TryReduceColorConflicts` searches for transpositions
that increase the number of players getting their due colour.

---

### Rule 29E3a � Due colors defined  <a id="rule-29e3a"></a>

**Status.** ? enforced

**Plain statement.** The due-colour definition has three branches:

- **Unequal whites/blacks** ? due colour is whichever **equalizes**
  the balance (more whites played ? due black, and vice versa).
- **Equal whites/blacks** ? due colour is the **opposite of the
  most recent round** (alternation).
- **No prior actual games** (all byes / forfeits) ? due colour is
  **neither**; either may be assigned.

Forfeit-game colours **do not** count in deciding due colour
([29E1](#rule-29e1)).

**FreePair coverage today.** `PreferredColor` helper implements
the three branches. `TopGetsWhite` consumes the result.

---

### Rule 29E4 � Equalization, alternation, and priority of color  <a id="rule-29e4"></a>

**Status.** ? enforced

**Plain statement.** **Equalization beats alternation.** First,
give as many players as possible the colour that *equalizes* their
whites/blacks total. After that, give as many players as possible
the *opposite* colour from last round (alternation).

**When pairing two players due the same colour**, apply rules 1�5
in order until one decides:

1. **Rule 1.** If one has had unequal whites/blacks and the other
   equal, the **unequal** player gets due colour. (Example: WBW
   gets black over BxW.)
2. **Rule 2.** If both unequal, the one with the **greater total
   imbalance** gets due colour. (Example: WWBW gets black over xWBW.)
3. **Rule 3.** If both equal (or both equally out of balance), and
   they had **opposite** colours in the previous round, give each
   the opposite of what they had. (Example: WWB vs WBW � first gets
   white because of round-2 alternation; both had different round-3
   colour and equal totals; the latest-differing-round wins.)
4. **Rule 4.** If both equal (or both equally out of balance) and
   they had the **same** colour in the previous round, look at the
   **latest round in which their colours differed**; assign the
   opposite of what was played that round. (Examples: WBWB gets
   white over BWWB because the first had black in round 2, the
   latest differing round. BWxBW gets white over BWBxW because the
   first had black and the second had no colour in round 4, the
   latest differing.)
5. **Rule 5.** If all sequences are identical, the **higher-ranked
   player** (higher score, then higher rating) gets due colour.
   Per the TD TIP, rule 5 takes effect only when rules 1�4 don't
   decide.

**FreePair coverage today.** `TopGetsWhite` implements steps 1�5
as documented in the existing engine comments. Two non-obvious
tiebreakers were added on the `ManualTesting` branch (commit
`3230c08`): equal-imbalance same-preference gate, and per-colour
recency tiebreaker.

**Annotation today.** `ColorEqualization` (rule 2),
`ColorAlternation` (rule 3), `ColorByRating` (rule 5),
`ColorByInitialRule` (fallback / first round). Phase B fixed all
four to cite `"29E4"` (previously `"29D1"` / `"29D2"` / `"29D"` /
`"29E1"`, which were odd-player and unplayed-games rules).
Step 1 (29E5f no-three-in-a-row absolute) emits
`ColorStreakAbsolute` with `UscfRule: "29E5f"` (Phase B fixed the
previously-emitted invented `"29D5"`).

---

### Rule 29E4a � Variation: priority based on plus/even/minus  <a id="rule-29e4a"></a>

**Status.** ? planned

**Plain statement.** When applying rule 5 (above), the
higher-ranked player gets priority in **plus and even** score
groups; the **lower-ranked** player gets priority in **minus** score
groups. Rationale: minimizes colour problems in both extremes of
the score distribution.

**FreePair coverage today.** Not implemented. `TopGetsWhite`
always uses higher-ranked for rule 5. Phase C may add this as an
optional configuration once the TD-variations infrastructure exists.

---

### Rule 29E4b � Variation: alternating priority  <a id="rule-29e4b"></a>

**Status.** ? planned

**Plain statement.** When applying rule 5 within a score group
with multiple same-colour-due situations, **alternate** which side
gets due colour: first higher-rated, second lower-rated, third
higher-rated, etc.

**FreePair coverage today.** Not implemented. Phase C consideration.

---

### Rule 29E4c � Variation: priority based on lot (last round)  <a id="rule-29e4c"></a>

**Status.** ?? TD discretion

**Plain statement.** In the **last round only**, the TD may let
opponents with equal entitlement choose colours by lot after all
necessary equalization / alternation pairings are made. **If
adopted, must be used for all such cases without exception.**

**FreePair coverage today.** Not modelled as a software option.
The TD can manually swap colours via the pairings interface to
implement this.

---

### Rule 29E4d � Variation: priority based on rank (old rule)  <a id="rule-29e4d"></a>

**Status.** ? deferred

**Plain statement.** This was the **old main rule in the 4th
edition** of the rule book � rule 4 above does **not** apply; the
higher-ranked player simply gets due colour whenever both have
equal whites/blacks (or are equally out of balance) and had the
same colours in the preceding two rounds. The TD TIP notes this
variation may still be used by some directors / pairing programs.

**FreePair coverage today.** Not supported. The current main rule
(29E4 step 4) is implemented; this variation isn't.

---

### Rule 29E5 � Colors vs. ratings (umbrella)  <a id="rule-29e5"></a>

**Status.** ? partial

**Plain statement.** Correct Swiss pairings consider **both colours
and ratings**, so the TD should not distort either unduly. To
improve colours, the TD may use either a **transposition** or an
**interchange**. (See [29E5e](#rule-29e5e) for which to prefer.)

- **Transposition** = changing the order of players within the
  upper half *or* within the lower half.
- **Interchange** = switching a player from the **bottom of the
  upper half** with a player from the **top of the lower half**.

The rule's umbrella TD TIP: arithmetic for transpositions and
interchanges applies only to the **first natural pairing** (after
the TD has already swapped players who faced each other before, are
teammates, or family) **before** any colour swaps are made compared
to the final pairing **after** all colour swaps. In other words,
the natural-pairing baseline includes 27A1 swaps already; rating-
difference math only applies to the colour-driven changes on top.

**FreePair coverage today.** `TryReduceColorConflicts` performs
the search but minimises by **conflict count + board-distance
disturbance**, *not* by rating difference. The rating-cap rules
[29E5a](#rule-29e5a)�[29E5b](#rule-29e5b) are **not enforced**.
Consequently FreePair may accept a swap that USCF would reject as
overshooting the 80 or 200 point limits.

**Annotation today.** `ColorConflictReduction`, `UscfRule: "29E5"`
(Phase B narrowed from the chapter umbrella `"29E"`). Phase C will
further distinguish capped / uncapped and alternation- vs equalization-
driven sub-cases when the rating-cap rules land.

---

### Rule 29E5a � The 80-point rule  <a id="rule-29e5a"></a>

**Status.** ? enforced (`IsRatingCapCompliant`; pinned by `Rating_cap_29E5a_*`)

**Plain statement.** Transpositions and interchanges made for the
purpose of **maximising the number of players who receive their
*due* (alternation) colour** should be limited to players with a
**pre-tournament rating difference of 80 points or less**.

**Worked example** (rule book, paraphrased).
> Round 3 pairing `WB vs WB` would give one of these players a
> second straight black. That's only **moderately undesirable**,
> and does **not justify** a switch of over 80 rating points.

**FreePair coverage today.** Not enforced. `TryReduceColorConflicts`
performs a branch-and-bound search that may exchange players with
arbitrary rating differences as long as the conflict count drops
and the board-distance disturbance is acceptable. In score groups
where SwissSys would refuse a swap on rating-distance grounds,
FreePair currently makes it.

**Annotation today.** `ColorConflictReduction`. Phase C will add a
sub-distinction (e.g. `ColorConflictReductionCapped` /
`ColorConflictReductionRejected`) and emit the rating diff in the
annotation text.

**See also.** [29E5b](#rule-29e5b) (the 200-point sibling),
[29E5c](#rule-29e5c) (how to compute transposition rating diffs �
"smaller of two"), [29E5e](#rule-29e5e) (when to prefer transposition
over interchange), [29E5g](#rule-29e5g) (unrated exemption).

---

### Rule 29E5b � The 200-point rule  <a id="rule-29e5b"></a>

**Status.** ? enforced (`IsRatingCapCompliant`; pinned by `Rating_cap_29E5b_*`)

**Plain statement.** Transpositions and interchanges made for the
purpose of **minimizing the number of players who receive one
colour two or more times more than the other** (equalization
conflicts � the *more serious* kind) should be limited to players
with a **pre-tournament rating difference of 200 points or less**.

The rule book's TD TIP notes that experienced TDs see *fewer
player complaints* about violations of 29E5b for **white** than for
**black** � i.e., giving an extra unwanted *white* is less
disruptive than an extra unwanted *black*. The variation 29E5b1
formalises this: 200-point limit applies specifically to **avoiding
two-extra-blacks**.

**Worked example** (rule book, paraphrased).
> Round 4 pairing `BWB vs BWB` would give one of these players
> **black for the third time**. That's **highly undesirable**,
> justifying a switch limit of **200 points**.

**FreePair coverage today.** Not enforced. Same gap as
[29E5a](#rule-29e5a). Phase C will add the second-tier (200pt)
budget and distinguish equalization-driven swaps from
alternation-driven ones.

**See also.** [29E5a](#rule-29e5a) (80pt sibling),
[29E5b1](#rule-29e5b1) (variation),
[29E5h](#rule-29e5h) (variation: drop both limits entirely).

---

### Rule 29E5b1 � Variation: 200pt for two-extra-blacks  <a id="rule-29e5b1"></a>

**Status.** ? planned

**Plain statement.** Restrict the 200-point rule specifically to
the **two-extra-blacks** case (player who would get a third black
beyond an equalized allocation). Other equalization conflicts use
a tighter cap or none at all.

**FreePair coverage today.** Not modelled. The main 29E5b is
prerequisite; this is a refinement on top.

---

### Rule 29E5c � Evaluating transpositions  <a id="rule-29e5c"></a>

**Status.** ? enforced (`IsRatingCapCompliant`; pinned by `Rating_cap_29E5c_*`)

**Plain statement.** All transpositions are evaluated based on the
**smaller of the two rating differences** involved.

**Worked example** (rule book, paraphrased).
> Round 3 with `2000 WB vs 1800 WB` on board 1 and `1980 BW vs
> 1500 BW` on board 2. Both boards have colour conflicts.
> Trading the 1800 for the 1500 *looks like* a 300-point switch
> (violating the 80-point rule). But the same final pairings come
> from trading the 2000 for the 1980 � only a 20-point switch.
> Although the physical operation in pairing cards is to swap the
> lower-half players, the **arithmetic** uses the smaller
> difference: **20 points**, well within the 80-point limit.
>
> Resulting pairings: `2000 white vs 1500` and `1800 white vs 1980`,
> counted as a 20-point switch.

**Special case: cascading transpositions.** In larger groups, a
permissible transposition may generate additional knock-on
transpositions, not all of which satisfy 29E5a / 29E5b. The TD may
**strictly observe** the limits or **be flexible** � exceeding
limits "somewhat" is acceptable if colours improve substantially.

**FreePair coverage today.** Not enforced. Phase C target.

---

### Rule 29E5d � Evaluating interchanges  <a id="rule-29e5d"></a>

**Status.** ?? implemented but **gated off by default** (no corpus
delta when enabled � see below)

**Plain statement.** For an **interchange**, the TD considers only
**one** rating difference (not the smaller-of-two from
[29E5c](#rule-29e5c)): the difference between the two players being
exchanged across the upper-half / lower-half boundary.

Although interchanges are acceptable within the 80 / 200-point
limits, they **violate the basic principle** [27A3](#rule-27a3)
(upper vs lower half) and **tend to catch players by surprise** �
players in contention for prizes are especially vocal about them.
**Interchanges should not be used if adequate transpositions are
possible.**

**FreePair coverage today.** `TryCrossHalfInterchange` is called
only when no bottom-half-only transposition resolves the rematch
constraint � which honours the "interchange last" preference for
rematch-driven swaps. For **colour-driven** swaps, the colour
reducer (`TryReduceColorConflicts`) now contains a full two-pass
implementation: pass 1 enumerates bottom-half transpositions
(preferred), pass 2 enumerates cross-half interchanges, and the
`ColorInterchangeReduction` annotation cites this rule with a
clickable deep link in the "Why this pairing?" dialog. **Pass 2 is
gated off by default** (`EnableCrossHalfInterchangePass = false` in
`UscfPairer.cs`). A full-corpus A/B showed enabling it is a
**net -3 regression**: on the sections where it fires, SwissSys
*tolerates* the residual colour conflict rather than interchanging
across the half boundary (consistent with the "interchanges catch
players by surprise / last resort" wording above). The machinery,
annotation, and unit tests ship so the path is ready the moment a
SwissSys-faithful discriminator for "take the interchange vs
tolerate the conflict" is established; until then the engine
under-applies this rule deliberately rather than diverge from
SwissSys.

---

### Rule 29E5e � Comparing transpositions to interchanges  <a id="rule-29e5e"></a>

**Status.** ? partial — blocked on the gated cross-half interchange pass

**Plain statement.** **Decision rule:**

- A transposition that satisfies [29E5a](#rule-29e5a) (?80pt) is
  **preferred to any interchange**, provided it is *at least as
  effective* in minimizing colour conflicts.
- When [29E5b](#rule-29e5b) is in play (200pt budget � many
  equalization conflicts), prefer an interchange with a **smaller
  rating switch** than the transposition, **unless** the
  transposition satisfies 29E5a (?80pt) � in which case prefer
  the transposition.

**Worked example 1** (rule book, paraphrased).
> `2050 WBW vs 1850 WBW` and `1870 BWB vs 1780 BWB`, round 4.
> Both boards have colour conflicts.
> - Interchange swap (1870 ? 1850) = **20-point switch**.
> - Transposition swap (1850 ? 1780) = smaller-of-two = **70-point
>   switch**.
> Both satisfy 29E5a's 80pt limit. The **transposition (70pt) is
> preferred** even though the interchange (20pt) is smaller �
> because the transposition stays inside the 80pt rule.
> Final pairings: `1780-2050` and `1870-1850`.

**Worked example 2** (rule book, paraphrased).
> Same setup as example 1 but with the bottom player rated 1750
> instead of 1780. Now:
> - Interchange swap (1870 ? 1850) = **20-point switch**.
> - Transposition swap (1850 ? 1750) = **100-point switch** �
>   exceeds 80pt rule but allowed under 29E5b's 200pt budget
>   (two-extra-blacks).
> Since the transposition violates 29E5a, the 80pt-priority gate
> doesn't apply. The **interchange (20pt) is preferred** because
> it's smaller.
> Final pairings: `1870-2050` and `1750-1850`.

**FreePair coverage today.** Not enforced � the transposition /
interchange preference is hard-coded (transposition first for
rematch, no interchanges for colour) rather than rule-derived.
Phase C will implement the proper decision tree.

---

### Rule 29E5f � Colors in a series (no three in a row)  <a id="rule-29e5f"></a>

**Status.** ? enforced (soft cap with rule-book escape clauses)

**Plain statement.** **No player shall be assigned the same colour
three times in a row**, *unless* there is no other reasonable way
to pair the score group, *or* the same-colour-three-times is
necessary to **equalize colours** for the rest of the field.

**FreePair coverage today.** `TryReduceColorConflicts` now scores
candidate pairings lexicographically by
`(colour-conflicts, forced-three-in-a-row pairs, transposition
distance)`. A *forced three-in-a-row pair* is one where both
players have the same two-game streak in the same direction �
neither side can take the opposite colour, so pairing them is
guaranteed to give one player a third same-colour game. Because
colour-conflict reduction is the higher-priority dimension, both
rule-book escape clauses fall out naturally:

* **"Necessary to equalise colours."** A pairing that introduces
  a forced 3-in-a-row but eliminates a colour conflict still wins,
  because colour conflicts outrank 3-in-a-row in the lex order.
* **"No other reasonable way to pair the group."** When every
  alternative ties on conflicts and 3-in-a-row count (e.g. the
  only legal pairing of the last two players in a sub-group), the
  natural pairing wins on the disturbance dimension and the
  3-in-a-row is accepted with an explicit annotation.

When the cap binds and the natural pairing changes, the engine
selects the transposition that avoids the 3-in-a-row. When no
such transposition exists, the natural pairing is kept and
annotated so the TD can see exactly why.

**Annotation today.** Two paths:
* `ColorStreakAbsolute` (citing `"29E5f"`) when the engine
  detects a two-in-a-row pattern on **one** player and assigns
  the opposite colour � the common case, handled by
  `TopGetsWhite` long before reduction.
* `ColorThreeInRowAccepted` (citing `"29E5f"`) when the engine
  was unable to avoid a forced 3-in-a-row pair and surfaces the
  rule-book escape clause to the TD in the "Why this pairing?"
  dialog.

---

### Rule 29E5f1 � Variation: last-round exception  <a id="rule-29e5f1"></a>

**Status.** ?? TD discretion

**Plain statement.** **Except for the last round**, when it may be
necessary to pair tournament or class leaders, players shall not
be assigned the same colour in three successive rounds. The
exception lets the TD force a same-colour-three-in-a-row pairing
when it's the only way to pair the leaders against each other in
the final round.

**FreePair coverage today.** The 29E5f cap implemented in
[29E5f](#rule-29e5f) is a *soft* cap that already accepts a
3-in-a-row when there is no alternative — which covers the
last-round-leaders scenario automatically (the TD's manual
force-pair or single-candidate score group both result in "no
alternative," so the natural pairing wins by default). No
separate round-number switch is needed.

---

### Rule 29E5g � Unrateds and color switches  <a id="rule-29e5g"></a>

**Status.** ? enforced (`IsRatingCapCompliant`; pinned by `Rating_cap_29E5g_*`)

**Plain statement.** If a player is **switched to or from an
unrated opponent** to improve colour allocation, this is **not in
violation** of the 80 or 200-point rules. The rationale: an
unrated player has no meaningful rating to apply the cap against.

**FreePair coverage today.** Currently moot since the rating caps
aren't enforced at all. Phase C will need this exemption built
into the same code path that adds the caps.

---

### Rule 29E5h � Variation: equalization priority over ratings  <a id="rule-29e5h"></a>

**Status.** ?? TD discretion

**Plain statement.** **Variation:** equalization of colours has
**priority** over rating differences. The [29E5a](#rule-29e5a)
80-point and [29E5b](#rule-29e5b) 200-point rules **do not apply**.
TD TIP: more successful at club / local events than at large
state / national tournaments.

**FreePair coverage today.** Effectively what FreePair does today
(no rating caps). When Phase C lands the caps, this variation
becomes a configurable "off-switch" the TD can flip per tournament.

---

### Rule 29E6 � Color adjustment technique  <a id="rule-29e6"></a>

**Status.** ? enforced (via the Look Ahead method)

**Plain statement.** The order in which pairings are switched to
improve colours can affect both the final pairings and the time it
takes to arrive at them. Two methods: the **Look Ahead** method
([29E6a](#rule-29e6a), preferred � more accurate and easier) and
the **Top Down** method ([29E6b](#rule-29e6b), variation � often
inferior pairings, time wasted on adjustments that don't reduce
conflicts).

**FreePair coverage today.** `TryReduceColorConflicts` implements
a Look-Ahead-style global search.

---

### Rule 29E6a � The Look Ahead method  <a id="rule-29e6a"></a>

**Status.** ? partial

**Plain statement.** Two-branch algorithm:

- **If half or fewer of the group is due the same colour:** start
  with the top pairing, work down, correcting as many colour
  conflicts (both players due the same colour) as possible. Unless
  [29E5a](#rule-29e5a) / [29E5b](#rule-29e5b) limits or
  [27A1](#rule-27a1) rematch constraints block, all colours will
  balance.
- **If more than half the group is due the same colour:** *avoid
  pairings in which neither player is due for that colour*. Such
  pairings represent wasted opportunities � both players in that
  pair could have been productively matched against players forced
  to play the opposite colour. The TD examines the natural pairings,
  applies any 27A1 swaps via the minimum-rating-change rule
  ([29E5a](#rule-29e5a) / [29E5b](#rule-29e5b)) **while avoiding
  neither-player-due pairs**, then checks the tentative pairings;
  any remaining neither-due pairs are corrected by transpositions to
  higher / lower boards with minimum rating differences.

**FreePair coverage today.** `TryReduceColorConflicts` does
global branch-and-bound over bottom-half permutations, minimising
total conflict count � which approximates the Look Ahead
philosophy but doesn't explicitly check the "more than half due
same colour" branch nor the "avoid neither-due pairs" heuristic.
For most real-world groups the engine reaches a Look-Ahead-equivalent
result, but in edge cases the two algorithms can diverge. Phase C
target: add the half-the-group due-colour check and the explicit
neither-due-pair avoidance.

---

### Rule 29E6b � Variation: the Top Down method  <a id="rule-29e6b"></a>

**Status.** ? deferred

**Plain statement.** Start with board 1, correct colours by
exchanging the top board's bottom-half player with the highest-
rated lower-half player whose colour fits � subject to
[29E5a](#rule-29e5a) / [29E5b](#rule-29e5b). Move down board by
board, correcting in the same manner. *Often produces inferior
pairings and wastes time on adjustments that don't reduce conflicts.*

**FreePair coverage today.** Not implemented. The rule book itself
recommends against this method; FreePair uses Look Ahead.

---

### Rule 29E7 � Examples of transpositions and interchanges  <a id="rule-29e7"></a>

**Status.** � (informational, no enforcement)

**Plain statement.** The rule book provides five worked examples
covering: simple natural pairings (Example 1), the
"swap-bottom-or-swap-top to achieve the same final pairing" pattern
(Examples 2 and 3), interchange-over-transposition when the
interchange is smaller (Example 4), and odd-group-floater across
score groups (Example 5). Several of these are paraphrased into the
[29E5c](#rule-29e5c) / [29E5d](#rule-29e5d) / [29E5e](#rule-29e5e)
entries above; Example 5 is referenced from [29D2](#rule-29d2).

**FreePair coverage today.** N/A � pedagogical only.

---

### Rule 29E8 � Variation: team pairings over colour equalization  <a id="rule-29e8"></a>

**Status.** ? planned

**Plain statement.** **Unannounced variation:** in a combined Swiss
individual + team tournament, avoiding teammate pairings shall
take **priority over** colour equalization.

**FreePair coverage today.** Today team avoidance fires the same
swap cascade as rematch (transposition ? interchange ? forced
merge) and runs **before** the colour-conflict reducer � so in
practice teammate avoidance already trumps colour for the *first*
pairing decision. But the colour reducer can then choose
transpositions that prefer colour balance over leaving teammate-
adjacent natural pairings undisturbed, so the priority isn't
absolute. Phase C target: a `TeammateAdjacency` veto that
overrides colour-only swaps.

---

### Rule 29F � Last-round pairings with unfinished games  <a id="rule-29f"></a>

**Status.** ?? TD discretion

**Plain statement.** Every reasonable effort should be made to
finish all games before pairing the last round. If finishing would
unduly delay round start, the TD may pair last round and watch
unfinished games carefully to prevent result-arranging for prizes.

**FreePair coverage today.** Not modelled � manual TD workflow.

---

### Rule 29G � Re-pairing a round  <a id="rule-29g"></a>

**Status.** ?? TD discretion

**Plain statement.** Sub-rules 29G1 / 29G2 / 29G3 cover when
re-pairing is appropriate.

**FreePair coverage today.** `TournamentMutations.DeleteLastRound`
+ `PairNextRound` is the workflow.

---

### Rules 29G1 / 29G2 / 29G3 � Re-pairing variants  <a id="rule-29g1"></a>  <a id="rule-29g2"></a>  <a id="rule-29g3"></a>

**Status.** ?? TD discretion

**Plain statement.**

- **29G1 round about to start.** Player withdraws as pairings near
  completion. If time allows, redo all pairings; otherwise **ladder
  down** � the withdrawn player's opponent plays a 1.5-point player,
  whose opponent plays a 1-point player, etc., until a bye is
  reassigned. The TD tries to ladder within rating range and same
  due colour. A house player may substitute.
- **29G2 round already started.** TD may adjust pairings but
  shouldn't cancel games where black has completed move 4.
- **29G3 selective re-pairing.** Some games already started, some
  haven't: tell started games to continue and re-pair waiting
  players as a separate group.

**FreePair coverage today.** Workflow is fully manual via
`DeleteLastRound` + `PairNextRound`. The ladder-down heuristic is
not automated.

---

### Rule 29H � Unreported results  <a id="rule-29h"></a>

**Status.** ?? TD discretion

**Plain statement.** Sometimes both players fail to report a
result. The TD has eleven sub-options (29H1�29H10) to balance
equity, speed of next-round pairing, and tournament integrity.

**FreePair coverage today.** TD picks an option and enters the
result via `SetPairingResult`; FreePair has no opinion.

---

### Rules 29H1�29H10 � Options for unreported results  <a id="rule-29h1"></a>  <a id="rule-29h2"></a>  <a id="rule-29h3"></a>  <a id="rule-29h4"></a>  <a id="rule-29h5"></a>  <a id="rule-29h6"></a>  <a id="rule-29h7"></a>  <a id="rule-29h8"></a>  <a id="rule-29h9"></a>  <a id="rule-29h10"></a>

**Status.** ?? TD discretion

**Plain statement (summary).**

- **29H1 ejection** � one or both players ejected (only for repeat
  offenders).
- **29H2 double forfeit of next round** � both removed from next
  round, forfeited.
- **29H3 double forfeit of unreported game** � both scored as
  losses; the real result, when learned, goes to an "extra rated
  games" chart ([28M4](#rule-28m4)).
- **29H4 half-point byes next round** � both get HPBs (if HPBs are
  offered in the event).
- **29H5 guess the winner** � pair higher-rated as win, lower as
  loss (statistically usually correct).
- **29H6 pair as win + draw** � higher rated as win, lower as draw
  (penalises non-reporting; guarantees wrong pairing but bounded).
- **29H7 pair as double win** � both paired as having won. (Better
  for class tournaments / even-or-minus scores / early rounds.)
- **29H8 multiple missing results** � omit all non-reporters from
  next round, pair them against each other once results learned.
  Caveat: don't reward a prize-contender with an unusually easy
  pairing.
- **29H9 results reported after pairings done** � guidance on
  whether to redo pairings (depends on which 29H5/H6/H7 option was
  used and whether the guess was right).
- **29H10 computer pairings** � redoing pairings on computer takes
  only minutes; delaying the round becomes more viable than with
  hand pairings � but remember the human delay from moving boards.

**FreePair coverage today.** All TD-driven via `SetPairingResult`;
FreePair has no automated picker among the options.

---

### Rule 29I � Class pairings  <a id="rule-29i"></a>

**Status.** ? deferred

**Plain statement.** In tournaments with significant class prizes,
**class pairings** may be used in the **last round** (if announced
in advance) to avoid scenarios where a class-prize contender
plays a higher-rated player who isn't (which invites collusion).
**Use only when no class-eligible player can win more than first
in the class.**

**FreePair coverage today.** Not implemented. The TD can manually
force pairings, but there's no first-class "class pairing" mode.

---

### Rules 29I1 / 29I2 � Class pairing methods  <a id="rule-29i1"></a>  <a id="rule-29i2"></a>

**Status.** ? deferred

**Plain statement.**

- **29I1 full-class pairings.** Treat the class as a separate Swiss
  tournament; pair internally. Odd class member is paired as
  normally as possible outside the class.
- **29I2 partial class pairings.** Pair class-eligible players with
  each other; treat the rest of the field normally. Useful when
  software doesn't natively support class pairings.

---

### Rule 29J � Unrateds in class tournaments  <a id="rule-29j"></a>

**Status.** ? deferred

**Plain statement.** In events restricted to a max rating with
unrateds allowed, if two or more unrateds have plus scores in the
same score group, the TD **may** pair them against each other.
Makes undeserved prize-winning harder.

**FreePair coverage today.** Not implemented as a switchable
behaviour. Engine treats unrateds the same as other players for
pairing.

---

### Rule 29K � Converting small Swiss to round robin  <a id="rule-29k"></a>

**Status.** ? deferred (round robin is a separate FreePair subsystem)

**Plain statement.** A 5-round Swiss with 6 entries or 3-round
Swiss with 4 entries **may** be converted to a round robin. Works
for quick one-day tournaments; often works poorly for multi-day.
Withdrawals distort round robins more than Swisses.

**FreePair coverage today.** Section conversion isn't a runtime
operation. FreePair has a separate `RoundRobinScheduler` for
sections configured as round robin from the start.

---

### Rule 29L � Using round robin table in small Swiss  <a id="rule-29l"></a>

**Status.** ? deferred

**Plain statement.** Better than [29K](#rule-29k): keep the event
as a Swiss but use round robin pairing numbers from Chapter 12 to
minimise repeat pairings. Pair round 1 as a Swiss, assign round
robin pairing numbers based on those pairings, then for each
subsequent round select the round robin table line where the top
player in the top score group gets the proper Swiss opponent.
Colours assigned per Swiss rules (not the round robin table).

**FreePair coverage today.** Not implemented. This is a hybrid
methodology outside the current Swiss engine's design.

---

### Rule 29L1 � Variation: 1 vs. 2 pairings  <a id="rule-29l1"></a>

**Status.** ? deferred

**Plain statement.** Pair 1 vs 2, 3 vs 4, 5 vs 6, etc. in round 1
(no upper-half / lower-half slide). Subsequent rounds: rank within
score groups by rating, pair in groups of two starting with the
top two of the top group. Easier to administer than 29L for small
fields. Hybrid of club "ladder" and Swiss System.

---

### Rule 29M � Recommendations  <a id="rule-29m"></a>

**Status.** � (informational)

**Plain statement.** Some disparity in colour allocation is
**inevitable** in the Swiss system because score has priority over
colour. **Tournaments with an even number of rounds cause the most
problems** (when a disparity exists it's larger). **Odd-round
tournaments** keep more players happy and are easier to pair
because the expected 3-2 / 4-3 colour allocations are easier to
maintain.

**FreePair coverage today.** N/A � this is meta-advice for
tournament organizers. The Settings tab's tournament-info section
could surface this as a recommendation when an organizer is
choosing a round count.

---

## Appendix � TD-discretion overrides (outside the USCF rule book)

These are tools that USCF allows TDs to apply but does not
prescribe an algorithm for. FreePair surfaces each as a setting,
mutation, or per-pairing override. They're documented here for
completeness � annotations and citations should distinguish them
from rule-book pairing decisions.

### Same-team avoidance

**Status.** ? enforced

**What it is.** A per-tournament setting that prevents pairing two
players whose `Team` field matches (case-insensitive, non-blank).
Models the scholastic "avoid pairing teammates" practice
([28N](#rule-28n)). Treated as a binary forbidden pair � when
violated by a natural slide, the engine runs the same swap cascade
as a rematch.

**FreePair coverage today.** `UscfPairer.ShareTeam` + `IsForbiddenPair`.
The cascade emits `TranspositionAvoidSameTeam` /
`CrossHalfInterchange` annotations.

**Known limitation.** None for 28N1 itself. The plus-two threshold
is fixed at the rule-book default of 2; the 28N3 TD-variable
threshold and the 28N2 last-round exception are not yet exposed as
configurable variations.

### Same-club avoidance

**Status.** ? partial

**What it is.** Same idea as team avoidance but on a `Club` field.

**FreePair coverage today.** Recognised in `EventConfigViewModel`
settings; the engine treats it as identical to team avoidance
(`ShareTeam` checks both). Phase C: separate `ShareClub` predicate
and `Player.Club` field.

### Forced pair (TD lock)

**Status.** ? partial

**What it is.** TD locks two specific players together for a
specific round, overriding whatever the engine would have chosen.

**FreePair coverage today.** `TournamentMutations.AddForcedPairing`
records the constraint. Applied by short-circuiting that pair out
of the engine input before pairing runs. The annotation layer does
**not** yet emit a `PairingReason.ForcedPair` entry � the
annotation appears as a `NaturalSlide` with the lock invisible to
the "Why this pairing?" dialog. Phase C: wire the emission.

### Do-not-pair (TD lock)

**Status.** ? partial

**What it is.** TD blocks two specific players from ever being
paired in this event � independent of [27A1](#rule-27a1) (they
might not have played yet). Used for family members, close
friends, prior conflicts.

**FreePair coverage today.** `TournamentMutations.AddDoNotPair`
records the constraint; `IsForbiddenPair` honours it. Annotation
layer also doesn't yet emit `PairingReason.DoNotPair`. Phase C
mirror of forced-pair work.

### Scheduled half-point bye / zero-point bye

**Status.** ? enforced

**What it is.** Per-player request for a half-point or zero-point
bye in specific rounds. Honored by removing the player from the
active pool before pairing, and by [22C6](#rule-22c6) /
[28L4](#rule-28l4) preventing a subsequent full-point bye
(partially).

**FreePair coverage today.** `Player.RequestedByeRounds`
collection + `Section`-level zero-point-bye list. The engine sees
only the active subset. No inline annotation today; planned as
`PairingReason.ScheduledBye` in Phase C.

### Withdrawal

**Status.** ? enforced

**What it is.** Permanent removal of a player from the section
for all subsequent rounds. Their played history is preserved;
remaining rounds score zero.

**FreePair coverage today.** `TournamentMutations.SetPlayerWithdrawn`
flips a flag the active-pool builder honours. No inline annotation
today; planned as `PairingReason.WithdrawalRebalance` in Phase C.

---

## See also

- **[USCF_RULES_COVERAGE.md](USCF_RULES_COVERAGE.md)** � the
  at-a-glance status board: one row per rule, sortable, with the
  Phase-C branch name for each ? planned gap. This is the working
  document that drives the engine roadmap.
- **[USCF_ENGINE.md](USCF_ENGINE.md)** � engine architecture
  overview, CLI cheat sheet, what the verification harness covers.
- **[USCF_DISCRETIONARY_DIVERGENCES.md](USCF_DISCRETIONARY_DIVERGENCES.md)** �
  catalogue of accepted / deferred mismatches between FreePair and
  the SwissSys regression corpus, all tied back to the rules above.

For an authoritative source, consult the **USCF *Official Rules of
Chess*, 7th edition** (V. 7, 8-21-20), chapters 22, 27, 28, 29.



