# FIDE (Dutch) pairing rules — FreePair reference

A faithful paraphrase of the FIDE Swiss pairing rules that FreePair is
capable of citing, indexed to what the app can actually tell you about a
pairing. The source is **C.04.3 — FIDE (Dutch) System**, approved by the
FIDE Council on 28 October 2025 and applied from 1 February 2026, together
with the parts of C.04.1 (Basic Rules for Swiss Systems) it depends on.

This is *paraphrase* — it explains how FreePair interprets and reports each
rule. For an authoritative source, consult the FIDE Handbook itself at
<https://handbook.fide.com>.

## Scope, and an honest limit

FreePair pairs FIDE sections with
[bbpPairings](https://github.com/BieremaBoyzProgramming/bbpPairings),
running the **Dutch** system. The Handbook also defines the Dubov
(C.04.4.1), Burstein (C.04.4.2) and Lim (C.04.4.3) systems; FreePair does
not use them and this document does not cover them.

**What the "Why this pairing?" dialog can and cannot tell you.**
bbpPairings reports, for every player, the state it had going into the
round — score bracket, colour preference, bye eligibility, recent floats —
and the assignment it settled on. It does **not** report which criterion
produced a particular pair, and the internal search that chooses between
candidate opponents inside a bracket is not described in its output at all.

So FreePair explains:

- which scoregroup each player was in, and whether the pair crossed groups;
- who floated, in which direction, and whether they had floated recently;
- what colour each player was due, how strongly, and whether they got it;
- who received the pairing-allocated bye, and who was barred from it.

FreePair does **not** claim to explain *why this opponent rather than
another one of the same score*. That decision belongs to the engine's
weighted matching, and nothing in its output supports an answer. A citation
that looked authoritative but was guessed would be worse than no citation
at all, so the dialog stays silent on it.

**Quality criteria are minimised, not guaranteed.** Article 2.4 asks the
engine to comply "as much as possible", in descending priority. An unmet
colour preference or a repeated downfloat is therefore a legitimate result
— it means something of higher priority won — not a bug. The dialog
surfaces these precisely so that a TD being challenged can say the engine
chose it deliberately.

## How to read this document

Each entry has a fixed shape:

| Section | Meaning |
|---|---|
| **Plain statement** | What the rule says, in TD-readable language. |
| **What FreePair can see** | The evidence in the engine's output that supports citing it. |
| **Annotation today** | The `PairingReason` and `FideRule:` value emitted when it fires. |
| **See also** | Cross-references inside this document. |

### Citation-anchor convention

Every rule has a stable Markdown anchor of the form `rule-<number>`, with
the article number's dots replaced by hyphens:

- `1.7.1` → [`#rule-1-7-1`](#rule-1-7-1)
- `2.4.9` → [`#rule-2-4-9`](#rule-2-4-9)
- `5.2.2` → [`#rule-5-2-2`](#rule-5-2-2)

Dots are **replaced**, not stripped. The USCF reference strips them
(`29E5a` → `rule-29e5a`), which works for that numbering but would collapse
`1.7.1` to `rule-171` here — unreadable, and liable to collide with a
hypothetical `17.1`. **Never rename an anchor once published**; it is a
contract with installed builds and external deep links.

Where the Handbook labels a criterion in brackets — `[C2]`, `[C14]` — that
label is given alongside the article number. The labels matter because
bbpPairings names its own checklist columns after them.

---

## 1. Definitions

### Rule 1.2 — Order  <a id="rule-1-2"></a>

**Plain statement.** For pairing purposes players are ranked by score
first, then by Tournament Pairing Number (TPN) ascending.

**What FreePair can see.** The engine lists players in this order within
each bracket.

**See also.** [1.3.1](#rule-1-3-1).

---

### Rule 1.3.1 — Scoregroups  <a id="rule-1-3-1"></a>

**Plain statement.** A scoregroup is all the players on the same score.
A *pairing bracket* is the group actually being paired: the players of one
scoregroup, plus any players left unpaired by the bracket above. A bracket
is *homogeneous* if everyone in it has the same score, and *heterogeneous*
otherwise.

**What FreePair can see.** The engine's checklist separates players into
their brackets, and each bracket carries its score. Two players in the same
bracket were paired within their own scoregroup.

Note that the score used is the score **with acceleration**, where
acceleration is in use, not the raw score.

**Annotation today.** `FideSameBracket`, `FideRule: "1.3.1"`.

**See also.** [1.4.1](#rule-1-4-1), [1.4.2](#rule-1-4-2).

---

### Rule 1.4.1 — Downfloaters and moved-down players  <a id="rule-1-4-1"></a>

**Plain statement.** A downfloater is a player who cannot be paired inside
their own bracket and is moved to the next one down. In the bracket they
arrive in they are called a *moved-down player* (MDP).

**What FreePair can see.** A pair whose two players hold different scores.

**See also.** [1.4.2](#rule-1-4-2), [2.4.9](#rule-2-4-9).

---

### Rule 1.4.2 — Which player gets which float  <a id="rule-1-4-2"></a>

**Plain statement.** When two players on different scores play each other,
the higher-ranked of the two receives a **downfloat** and the lower-ranked
an **upfloat**.

**What FreePair can see.** The two players' scores, which determine the
direction directly.

**Annotation today.** `FideCrossBracketFloat`, `FideRule: "1.4.2"` — naming
both players, their scores, and which float each takes.

**See also.** [1.4.3](#rule-1-4-3), [2.4.9](#rule-2-4-9),
[2.4.10](#rule-2-4-10).

---

### Rule 1.4.3 — A bye is also a downfloat  <a id="rule-1-4-3"></a>

**Plain statement.** A downfloat is also given to any player who receives
the pairing-allocated bye, or who scores more than a loss in a round
without playing.

**What FreePair can see.** The bye assignment, and the engine's own float
columns, which show a downfloat for the bye-taker in the following round.

This is the reason a player who took a bye is treated as having floated
when later rounds are paired — it is not an accounting quirk.

**Annotation today.** `FidePairingAllocatedBye`, `FideRule: "1.4.3"`.

**See also.** [1.5](#rule-1-5), [2.1.2](#rule-2-1-2).

---

### Rule 1.5 — Pairing-allocated bye (PAB)  <a id="rule-1-5"></a>

**Plain statement.** If the number of players to be paired is odd, one
player is left unpaired. That player receives the pairing-allocated bye: no
opponent, no colour, and as many points as a win, unless the tournament's
own rules say otherwise. The value must be the same for every PAB in the
event.

**What FreePair can see.** The engine reports the bye directly.

**Annotation today.** `FidePairingAllocatedBye`, `FideRule: "1.5"` (or
[1.4.3](#rule-1-4-3) when the bracket detail is available).

**See also.** [2.1.2](#rule-2-1-2), [2.3.1](#rule-2-3-1).

---

### Rule 1.6 — Colour difference  <a id="rule-1-6"></a>

**Plain statement.** A player's colour difference is their number of games
with White minus their number of games with Black.

**What FreePair can see.** The colour history the engine prints for each
player — one letter per **played** game. Byes and forfeits contribute
nothing, which is why a player's history can be shorter than the round
number.

**See also.** [1.7.1](#rule-1-7-1).

---

### Rule 1.7.1 — Absolute colour preference  <a id="rule-1-7-1"></a>

**Plain statement.** A player has an **absolute** colour preference when
their colour difference is greater than +1 or less than −1, **or** when
they had the same colour in the two most recent rounds they played. The
preference is for White when the difference is below −1 or the last two
games were Black; for Black when the difference is above +1 or the last two
games were White.

This is the strongest form. Two non-topscorers with the same absolute
preference may not be paired together at all — see [2.1.3](#rule-2-1-3).

**What FreePair can see.** The engine marks preference strength explicitly.

**See also.** [1.7.2](#rule-1-7-2), [2.1.3](#rule-2-1-3),
[5.2.2](#rule-5-2-2).

---

### Rule 1.7.2 — Strong colour preference  <a id="rule-1-7-2"></a>

**Plain statement.** A player has a **strong** colour preference when their
colour difference is exactly +1 (preferring Black) or −1 (preferring
White).

**See also.** [1.7.3](#rule-1-7-3), [2.4.8](#rule-2-4-8).

---

### Rule 1.7.3 — Mild colour preference  <a id="rule-1-7-3"></a>

**Plain statement.** A player has a **mild** colour preference when their
colours are level. The preference is simply to alternate from the colour
they had in their previous game.

**See also.** [2.4.7](#rule-2-4-7).

---

### Rule 1.7.4 — No colour preference  <a id="rule-1-7-4"></a>

**Plain statement.** A player who has not played a game has no colour
preference. Their opponent's preference is granted instead.

**What FreePair can see.** The engine marks these players distinctly. In
round 1 this is **everybody**, which is why the dialog reports that no
colour was due rather than claiming a preference was satisfied.

**Annotation today.** `FideNoColorPreference`, `FideRule: "1.7.4"`.

**See also.** [5.2.1](#rule-5-2-1).

---

## 2. Pairing criteria

### Rule 2.1.1 — [C1] No player meets another twice  <a id="rule-2-1-1"></a>

**Plain statement.** Two players shall not play each other more than once.
This is an **absolute** criterion: no pairing may violate it.

**What FreePair can see.** Each player's full list of previous opponents,
alongside the opponent just assigned.

**See also.** [2.1.2](#rule-2-1-2), [2.1.3](#rule-2-1-3).

---

### Rule 2.1.2 — [C2] No second bye  <a id="rule-2-1-2"></a>

**Plain statement.** A player who has already received a pairing-allocated
bye, or who has already scored as much as a win in a round without playing,
shall not receive the pairing-allocated bye again. Absolute.

**What FreePair can see.** The engine reports bye eligibility per player —
this is the column it labels `C2`, after this criterion.

This is what answers "why did *that* player get the bye and not the one
below them?". The dialog names the players who were barred.

**Annotation today.** `FideByeIneligible`, `FideRule: "2.1.2"`.

**See also.** [1.5](#rule-1-5), [2.3.1](#rule-2-3-1).

---

### Rule 2.1.3 — [C3] Two absolute preferences may not meet  <a id="rule-2-1-3"></a>

**Plain statement.** Two non-topscorers who both have the same absolute
colour preference shall not be paired together. Absolute.

Topscorers — players above 50% when the final round is paired — are
excepted, because keeping the leaders correctly paired matters more than
their colours at that stage.

**See also.** [1.7.1](#rule-1-7-1), [5.2.2](#rule-5-2-2).

---

### Rule 2.3.1 — [C5] Minimise the bye-taker's score  <a id="rule-2-3-1"></a>

**Plain statement.** The pairing-allocated bye should go to a player as low
in the standings as possible.

**What FreePair can see.** The bye-taker's bracket.

**See also.** [1.5](#rule-1-5), [2.1.2](#rule-2-1-2).

---

### Rule 2.4.7 — [C12] Minimise unmet colour preferences  <a id="rule-2-4-7"></a>

**Plain statement.** Minimise the number of players who do not get their
colour preference.

A *quality* criterion: minimised as far as possible, not guaranteed. A
player can be denied their colour because a higher-priority criterion
required it.

**What FreePair can see.** Each player's due colour and the colour actually
assigned.

**Annotation today.** `FideColorPreferenceNotGranted`, `FideRule: "2.4.7"`,
when the unmet preference was mild.

**See also.** [2.4.8](#rule-2-4-8), [5.2.1](#rule-5-2-1).

---

### Rule 2.4.8 — [C13] Minimise unmet *strong* preferences  <a id="rule-2-4-8"></a>

**Plain statement.** Minimise the number of players who do not get their
**strong** colour preference. Ranked above [C12](#rule-2-4-7), so a strong
preference is given up only when a mild one cannot be sacrificed instead.

**Annotation today.** `FideColorPreferenceNotGranted`, `FideRule: "2.4.8"`,
when the unmet preference was strong or absolute.

**See also.** [1.7.2](#rule-1-7-2), [2.4.7](#rule-2-4-7).

---

### Rule 2.4.9 — [C14] Avoid a repeated downfloat  <a id="rule-2-4-9"></a>

**Plain statement.** Minimise the number of resident downfloaters who also
received a downfloat in the **previous** round.

**What FreePair can see.** The engine reports each player's float direction
for the previous round — the column it labels `C14`, after this criterion.

**Annotation today.** `FideRepeatDownfloat`, `FideRule: "2.4.9"`. When a
player downfloated in both of the last two rounds, this criterion is cited
rather than [2.4.11](#rule-2-4-11), because it has the higher priority.

**See also.** [1.4.2](#rule-1-4-2), [2.4.11](#rule-2-4-11).

---

### Rule 2.4.10 — [C15] Avoid a repeated upfloat  <a id="rule-2-4-10"></a>

**Plain statement.** Minimise the number of MDP opponents who received an
upfloat in the previous round.

**Annotation today.** `FideRepeatUpfloat`, `FideRule: "2.4.10"`.

**See also.** [2.4.12](#rule-2-4-12).

---

### Rule 2.4.11 — [C16] Avoid a downfloat repeated from two rounds back  <a id="rule-2-4-11"></a>

**Plain statement.** Minimise the number of resident downfloaters who also
received a downfloat **two rounds** before. Ranked below
[C14](#rule-2-4-9): a float in the round just gone weighs more than one
before that.

**What FreePair can see.** The engine's float column for two rounds back,
labelled `C16` after this criterion.

**Annotation today.** `FideRepeatDownfloat`, `FideRule: "2.4.11"`.

**See also.** [2.4.9](#rule-2-4-9).

---

### Rule 2.4.12 — [C17] Avoid an upfloat repeated from two rounds back  <a id="rule-2-4-12"></a>

**Plain statement.** Minimise the number of MDP opponents who received an
upfloat two rounds before.

**Annotation today.** `FideRepeatUpfloat`, `FideRule: "2.4.12"`.

**See also.** [2.4.10](#rule-2-4-10).

---

## 5. Colour allocation

### Rule 5.2.1 — Grant both preferences  <a id="rule-5-2-1"></a>

**Plain statement.** For each pair, in descending priority, first: if the
two players want opposite colours, give both what they want.

**What FreePair can see.** Both players' due colours and the assignment.

**Annotation today.** `FideBothColorPreferencesGranted`,
`FideRule: "5.2.1"`.

**See also.** [5.2.2](#rule-5-2-2).

---

### Rule 5.2.2 — Grant the stronger preference  <a id="rule-5-2-2"></a>

**Plain statement.** If both players want the same colour, grant the
stronger preference. If both preferences are absolute — which can only
happen between topscorers, see [2.1.3](#rule-2-1-3) — grant the wider
colour difference.

**What FreePair can see.** Both players' preference strengths.

**Annotation today.** `FideStrongerColorPreferenceGranted`,
`FideRule: "5.2.2"` — naming which player had the stronger claim, so it is
clear the other's preference was outranked rather than overlooked.

**See also.** [1.7.1](#rule-1-7-1), [1.7.2](#rule-1-7-2).

---

### Rules 5.2.3–5.2.5 — Remaining tiebreaks  <a id="rule-5-2-3"></a>

**Plain statement.** If the preferences are equally strong, then in order:

1. **5.2.3** — alternate colours back to the most recent round in which one
   player had White and the other Black.
2. **5.2.4** — grant the colour preference of the higher-ranked player.
3. **5.2.5** — if the higher-ranked player has an odd TPN, give them the
   colour drawn by lot before round 1; otherwise the opposite.

**What FreePair can see.** FreePair does not currently distinguish these
three from one another in the dialog; a pair decided at this depth is
reported through [5.2.2](#rule-5-2-2) or as an unmet preference. Naming the
exact tiebreak would require replaying the colour history of both players,
which the engine's output does not directly support.

**See also.** [5.2.2](#rule-5-2-2).
