# FreePair user guide

**Applies to FreePair v0.71.20260817**

FreePair is a chess tournament pairing program for tournament directors.
It imports SwissSys event files, pairs Swiss and round-robin sections,
tracks results and standings, prints the paperwork, and produces the
USCF rating report at the end.

This guide describes what you see on screen and the order you are likely
to do things in. If you are looking for *why* the pairing engine made a
particular decision, that is a different document: open the **Why this
pairing?** dialog on any board and follow the rule citation, or read the
USCF rules reference that ships alongside this guide.

---

## Getting started

### What FreePair does

FreePair runs an event end to end:

- Open or create an event, with one or more **sections**.
- Build each section's **roster**, verifying USCF/FIDE IDs and ratings.
- **Pair** each round, review the pairings before committing them, and
  intervene where the rules allow.
- Record **results**, and watch standings, tiebreaks and the wall chart
  update as you go.
- **Print** pairing sheets, standings, wall charts and prize lists.
- **Publish** pairings and results online, and file the rating report.

Two pairing engines are available. The **USCF (FreePair) engine** handles
USCF-rated Swiss events and is the default. The **FIDE (BBP) engine**
implements the FIDE Dutch system for FIDE-rated events. Round-robin
sections use FreePair's own scheduler and neither engine.

### Installing and updating

The installer bundles everything FreePair needs, including the .NET
runtime and both pairing engines. There is nothing to install
separately.

FreePair can check for new versions on startup. Turn this on or off
under **Settings → Updates → Check for updates on startup**, or use
**Check now** at any time. If you want early builds, tick **Include
pre-release builds (beta channel)** — otherwise you only see stable
releases.

When an update is found, a banner appears across the top of the window.
**Release notes** opens that release's page in your browser, so you can
read the whole thing and keep it open while you decide. **Update now**
downloads it and restarts FreePair. There is nothing to save first —
FreePair writes your event as you work.

Your current version is shown in Settings, and in the main window title
bar.

### The main window

The window has two tabs:

- **Tournament** — everything to do with the event you have open.
- **Settings** — application-wide preferences that apply to every event.

The **Theme** picker sits at the top right and applies immediately. The
**Help** button beside it opens this guide.

If an event is open, the title bar shows the event name and the file it
is saved to.

---

## Creating and opening events

### Creating a new event

Use **➕ Create New Event**. You will be asked for the event name, dates
and location, and then for at least one section. An event with no
sections cannot be paired, so it is normal to create the first section
immediately.

### Opening an existing event

**🔁 Re-Open Saved Event** offers the events you have worked on recently.
You can also browse the file system for a SwissSys `.sjson` file, or
browse events published to NA Chess Hub.

FreePair reads SwissSys files and writes them back in the same format,
preserving anything it does not itself manage. An event created in
SwissSys can be opened in FreePair and vice versa.

### Event details

The event's **Overview** covers the name, dates, location, time control
and the defaults new sections inherit. Time control and location appear
on printed reports, so they are worth filling in even though pairing does
not use them.

### Sections

Each section is paired independently and has its own roster, rounds,
standings and prizes.

- **➕ Add section** creates one. You choose the number of rounds, the
  pairing kind (Swiss or round robin) and which rating is used.
- **🗑 Delete section…** removes one, with a confirmation that tells you
  how many rounds and results you are about to lose.
- **Copy section** duplicates a section's setup, which is useful when an
  event has several similarly configured sections.
- **⧉ Open in Window** pops a section out into its own window, so you can
  watch two sections side by side.

Sections are laid out so that no two share a physical board number.
FreePair recommends starting board numbers automatically; you can
override them on the section's Overview tab or with **Renumber section
starting boards**.

---

## Players

### Adding players to a roster

The **Roster** tab holds the players in a section. You can add players
one at a time, look them up in the online player database, or pull a
whole roster from NA Chess Hub.

Adding a player after the event has started is supported. FreePair works
out what the late entrant should be scored for the rounds already played
and shows you the result before you commit.

### Verifying IDs and ratings

**ID and Rating** verifies USCF and FIDE IDs against the online player
database and fills in the current ratings.

**Roster Update** re-pulls live ratings for the whole section. Two
warnings exist here on purpose:

- Once a round has been paired, refreshing ratings can change the
  pairing basis mid-event, so FreePair asks first.
- Around the start of a month, a rating supplement may be about to
  change; FreePair points this out so you can decide which supplement
  the event should use.

Both need an internet connection.

### Optional columns

**⚙ Optional Columns** chooses which columns the roster shows. By default
columns with no data are hidden so the grid stays readable; turn on the
"show empty columns" option when you are about to fill them in. The
choice is saved with the section.

**Title** is shown by default. On a FIDE-rated section it decides the
order of players on equal ratings, so it is worth seeing next to the
pair numbers rather than having to go looking for it. Untitled players
leave the cell blank. Turn it off in the chooser if you do not want it.

### The roster sorts itself before round 1

Until the first round is paired, the roster keeps itself in seeding
order. Add a player and they drop into their proper place rather than
onto the end; correct someone's rating, or give them a title, and they
move accordingly. Equal ratings are broken by the section's own rules —
including the title step on FIDE sections — so what you see is the
order the event will actually pair on.

Refreshing ratings re-sorts the roster the same way.

**Once a round is paired this stops**, and pair numbers are fixed for
the rest of the event. They have to be: the rounds already played refer
to players by those numbers, so renumbering would change who is
recorded as having played whom. Editing a player after that point still
works — the number just stays put.

### Check-in

Some events pair only players who have physically arrived. Turn on
**Players must check in before pairing** for a section, then use
**Check-In Player** and **Undo Check-In**. Players who have not checked
in are left out of the pairing pool.

### Teams

For team events, **Team section** enables team handling, and
**Team Setup** and **Team Lineup** define the teams and board order.
FreePair can avoid pairing teammates against each other; where the rules
require it, that preference is relaxed rather than broken.

### Merging sections

Large opens often run one event on two schedules — a 3-day and a 2-day
that starts a day later and plays its early rounds faster — and join them
into a single field partway through. **⚖ Merge Sections** does that. It
is on **Event Operations**, and on a section's **Pairing Operations**
where that section starts already ticked.

The dialog lists every section with its pairing engine, pairing rule,
scheduled rounds, rounds paired so far and player count. Tick the ones to
combine.

Sections can only merge when they agree on the things that would
otherwise contradict each other: the same pairing engine, the same
pairing rule, the same number of scheduled rounds, and — once any of them
has been paired — the same number of rounds played. If a combination is
refused the dialog says exactly which differences are in the way, so you
can see whether it is fixable.

**Every result must be in.** A section with a game still unscored cannot
be merged, and the dialog names the section and the rounds concerned. The
merged section pairs its next round straight away, and that pairing reads
scores, colours and float history from the rounds before it — so one
missing result does not simply leave a gap on the wall chart, it pairs
somebody against the wrong opponent. A forfeit counts as a result;
an unplayed game does not. The **Results** column shows each section's
state before you tick anything.

**Different time controls are fine.** A 2-day schedule playing quicker
early rounds is the reason this exists.

Everything is carried across: rosters, pairings, results and byes.
Pairing numbers are re-issued across the combined field, and every
reference to them moves too, so games stay attached to the players who
played them.

**Players entered on more than one schedule** are found for you, matched
by USCF or FIDE ID where they have one and by exact name otherwise — a
nine-round open on three schedules can have the same player entered three
times, having lost their way through the first, re-entered on the second
and finally started the third.

They are listed in a table: one row per player, one column per section
being merged. The row shows their **name, ID and rating** so you can
satisfy yourself these really are one person before withdrawing anybody.
**Matched on** tells you what the match rests on — an ID is good
evidence, a name alone is a guess and is flagged.

In each section's column you will see either:

- **Not in this section** — they never entered it; or
- a **Withdrawn** tick box.

**Tick Withdrawn until exactly one section is left unticked.** That is
the section they carry on in. The last column, **How to continue**, says
what the ticks currently mean:

- _Continues in merged section as a player from {Section}_ — settled.
- _Will withdraw from merged section_ — every section ticked. Right for
  someone who withdrew from every schedule and never came back.
- _Can't continue — active in multiple sections_ — more than one section
  is unticked. Merge stays blocked until you resolve it, because two
  active records would put the same person on two boards in the next
  round, against two different opponents.
- _They are different players_ — the **Different people** box is ticked.

**These tick boxes read one way.** They describe the merged section and
nothing else. A box starts ticked where that section already has the
player withdrawn, because that is your own most recent statement about
them and is nearly always still what you want — but it is only a starting
point. Unticking it carries that appearance forward into the merged
section **without** reinstating them in the section they left; ticking a
box withdraws them from the merged field **without** touching their
record in the section they played. The original sections are kept
untouched and marked deleted, and the merge never writes back to them.

Because the file usually already says which appearance is live, most rows
in this table need nothing from you.

If the match is wrong — two juniors who share a name, or two players
issued one ID — tick **Different people**. They then all carry on as
normal players, and nobody is withdrawn. The Withdrawn boxes on that row
grey out, because with no duplicate to resolve there is nothing left for
them to say.

Every appearance that is not carrying on is renamed
`{Player}-{Section}-Withdrew` and marked withdrawn. **Nothing is lost
either way**: every appearance stays in the merged section and keeps the
games it played, so those games still count and still rate.

The suffix is only for players who appear more than once — it exists to
explain why one person has several rows. Somebody who simply withdrew
from a single section keeps their name unchanged.

The merged section takes its settings — tiebreaks, prizes, time control,
first board, acceleration — from whichever source section you choose,
defaulting to the first.

**The original sections are kept but marked deleted.** They stay
available to look at and to restore, but they are left out of the USCF
and FIDE rating reports and out of publishing, because their games are
now being reported by the merged section and would otherwise be counted
twice.

### Moving players between sections

**⇄ Move players…** transfers players between sections, carrying their
history with them. This is the correct way to handle a player who was
entered in the wrong section.

---

## Pairing rounds

### Before you pair

Check that the roster is complete, IDs are verified, byes are requested
and — if you use it — check-in is done. Pairing is reversible, but it is
much less disruptive to get the pool right first.

### Pairing a round

**Pairing Operations** pairs the next round for the section. To pair
every section at once, use the pair-all-sections action from the event
page.

For the first round you may be asked who plays White on board 1, and how
the field should be seeded. Later rounds are determined by the pairing
rules and need no input.

The seeding prompt offers **Re-sort** — marked *(Recommended)* and
pre-selected, so pressing Enter takes it — or **Keep current order**.
Take the re-sort. The pairing rules assume the engine's own seeding
order, and the button names which one applies: *Re-sort (FIDE order)* or
*Re-sort (USCF order)*. Let FreePair number the field from the rules
rather than adjusting pair numbers by hand; a hand-edited order can
produce boards the rules would not have chosen, and it is the order the
engine is given. **Keep current order** exists for the narrow case where
the numbers were assigned elsewhere and must be reproduced exactly —
matching an arbiter-assigned starting list on an imported event, for
instance.

If you accept the re-seed, players are numbered strongest first — and
what breaks a tie between players on the **same** rating depends on the
section's engine:

- **USCF sections** fall straight to alphabetical order by name
  (USCF 28E).
- **FIDE sections** put **title** in between: GM, IM, WGM, FM, WIM, CM,
  WFM, WCM, then untitled, and only then alphabetical
  (FIDE C.04.2 2.2). The women's titles genuinely do interleave with the
  open ones — a WGM seeds above an FM — which looks odd but is what the
  rule says.

Only FIDE titles count for that ordering. A national title such as **NM**
is kept and shown on the roster, but seeds as untitled, because the FIDE
rule does not rank it and inventing a position for it would change who
plays whom.

This matters more than it sounds: in round 1 everyone is on zero and
nobody is owed a colour, so the seeding order is the **only** thing
deciding the boards.

### Pairing as quads

**Pair as quads** divides a section into four-player round-robin groups,
seeded by rating: the top four, the next four, and so on. Each quad is
paired immediately — all three rounds at once, since in a four-player
all-play-all every game is known from the start. Any remainder becomes a
Swiss group you pair normally.

**A section of exactly four players is simply converted into a quad.**
It keeps its name, no new sections appear, and nothing is deleted — the
section already *is* the quad, so there is nothing to divide. Larger
sections do get split, and the original is kept but soft-deleted so its
players do not count twice; you can undo that by restoring it and
removing the generated sections.

Either way the players are re-seeded by rating first, because the
round-robin schedule is built around the top seed.

### Reviewing before you commit

Pairings are shown for review before they become final. In this preview
you can:

- See every board with colours and ratings.
- Swap colours on a board.
- Convert a pairing to a half-point bye.
- Force a specific pairing, or forbid one.

Each player's title is shown in front of their name — "GM Smith" — on
screen and on the printed pairing sheet. It is part of the player cell
rather than a column of its own, so it costs no extra width. The
pairing-columns chooser can turn it off per side.

Nothing is written to the event until you accept.

### Why this pairing?

Every board has a **?** button. It opens the **Why this pairing?**
dialog, which explains the board in plain language:

- The **pairing reason** — why these two players were put together.
- The **colour assignment** — why each player got the colour they did.
- The **round context** — floats, byes and anything else that shaped the
  round.

Each line ends with the rule that drove the decision, as a clickable
citation that opens the rules reference at that exact rule. This is the
fastest way to answer a player who is questioning their pairing.

**Which rule book you see depends on how the section is paired.** USCF
sections cite the USCF *Official Rules of Chess* and open the USCF
reference; FIDE sections cite the FIDE Handbook's Dutch system (C.04.3)
and open the FIDE reference. The citation says which — `(USCF 29E5 — …)`
or `(FIDE 5.2.1 — …)` — so there is no doubt which book you are quoting.
Both are installed with FreePair and work with no internet.

For a FIDE section the dialog explains:

- which score group each player was in, and whether the pair crossed
  groups — and if so, who floated up and who floated down;
- what colour each player was due and how strongly (absolute, strong or
  mild), and whether they got it;
- who received the pairing-allocated bye, and who was not allowed one
  because they had already had it;
- whether a player is floating in the same direction they floated in a
  recent round.

**One honest limitation for FIDE sections.** FreePair pairs them with the
bbpPairings engine, which reports each player's situation and the pairing
it chose, but not its internal search. So the dialog can tell you why a
player was in a particular group, why they got a particular colour, and
why they floated — but it cannot tell you why they were paired against
*that particular opponent* rather than another player on the same score.
Nothing FreePair could say about that would be more than a guess, so it
does not say it.

It is also worth knowing that some FIDE rules are *preferences*, not
hard limits. The rules ask the engine to keep unmet colour preferences
and repeated floats to a minimum, not to eliminate them. So a line
saying a preference was not granted, or that a player downfloated twice
running, is reporting a deliberate decision where something of higher
priority took precedence — not a mistake.

### The pairing bracket diagram

Below the explanation, a FIDE board shows the **pairing bracket** it was
paired in — the group of players the engine was working with at that
point, and the pairs it made from them.

- Players are listed **in the engine's seeding order** — strongest first.
  That is how the Dutch system assigns pairing numbers before round 1, and
  it is what the engine pairs from. It usually matches the order of the
  pair numbers you see on the roster, but not always: a player added
  mid-event takes the next free pair number rather than the one their
  rating would earn, so they appear in the bracket at their playing
  strength, not at the end.
- **S1** and **S2** are the two subgroups the Dutch system splits a
  bracket into.
- Players shown in **brown moved down** from a higher score group,
  with the score they brought with them. This is what explains a board
  where the two players are not on the same score.
- The badge after each rating is the colour that player was due:
  `W`/`B` absolute, `(W)`/`(B)` strong, `w`/`b` mild, `A` none yet.
- Any player who took the bye out of this bracket is named underneath.

**Every line is drawn the same way, and that is deliberate.** The engine
does not report how it chose between the possible opponents inside a
bracket, so the diagram shows you which pairs were made — not which were
"expected" and which were not. Any such marking would be a guess dressed
up as a fact.

USCF sections show a different diagram, the score-group switch diagram,
which reflects how the USCF engine pairs. The two are not
interchangeable.

### Adjusting pairings

After a round is paired you can still swap colours, force or forbid
pairings, and re-pair. Forced pairings and do-not-pair instructions apply
to the current session.

### Unpairing a round

A round can be deleted if you paired it too early or the roster was
wrong. FreePair tells you how many recorded results you are about to
lose, and takes a checkpoint first so the round can be recovered.

Deleting pairings for the whole event at once is possible, and is
deliberately harder to do by accident: the confirmation is red, and
**Cancel** is the default button, so pressing Enter backs out.

### PGN headers for recording games

**♟ PGN headers** on the Pairings tab writes a `.PGN` file containing the
game headers with **no moves** — choose **This round** or **All rounds**.
Open the file in a chess program, find the game, and type the moves in.

The point is that everything tedious is already filled in: event name,
section, date, time control, board number, both players' names, titles,
ratings, and their USCF, FIDE and CFC IDs. That metadata is identical for
every game of a round, and nobody should key it eighty times.

Files are named `{Event}_{Section}_Round{x}_Header.PGN`, or `AllRounds`
in place of the round for the whole-event file.

Games that have not been played yet carry a result of `*`, so you can
export before the round starts and fill games in as they finish. Byes are
not included — there is no game to record. A player with no rating gets
no rating tag rather than a zero, which would read as a rating of nought.

### Round-robin sections

Round-robin sections are scheduled rather than paired: every player meets
every other player, and the whole schedule is produced at once. The
pairing engines are not involved.

---

## Byes and withdrawals

The **Byes & Withdrawals** tab is where absences are managed.

- **Half-point byes** are requested in advance by the player. Record them
  and the player is withheld from that round's pairing pool.
- **Full-point byes** are assigned by FreePair when a section has an odd
  number of players. The rules decide who receives one; a player who has
  already had a full-point bye will not normally get another.
- **Zero-point byes** cover a player who is absent without a request.
- **Withdrawing** a player removes them from future rounds while keeping
  the games they have already played.

Byes are shown on the Roster, on the Pairings tab with a count, and on
the Standings, so a bye is hard to miss.

You can also assign a bye mid-event once a round is already paired.

---

## Results and standings

### Entering results

Results are entered on the **Pairings** tab, board by board. Standings,
tiebreaks and the wall chart update immediately.

### Standings and tiebreaks

The **Standings** tab ranks players by score and then by tiebreak.
**🏆 Tiebreaks…** chooses which tiebreak systems apply and in what order.
Tiebreak columns are sortable, and sort by the underlying value rather
than the displayed text.

Half-point and zero-point byes appear in their own columns, so you can
check them against your written list at a glance.

A **Title** column sits beside the rating, matching the roster.

**Round codes name the opponent's place in the standings.** `W7B` means
a win with Black against whoever is standing 7th — the row numbered 7 in
the **#** column, on the same sheet. That column is always shown, and is
deliberately separate from **Place**: places tie, so Place reads "2-6"
against the first of five players and is blank against the rest, while an
opponent reference has to name exactly one row. The filter box searches
it too, so typing `7` finds the player a code points at.

This is how US Chess prints its published crosstable and how NA Chess Hub
shows standings, so a player comparing the three sees the same number in
all of them.

**Team standings** carry the same **#** column, for the same reason: team
places tie just as player places do, and four teams level on match points
all show "1-4".

### Wall chart

The **Wall Chart** tab shows the traditional cross-table: every player's
round-by-round result, opponent and colour. It carries the same
**Title** column as the standings.

**Here the round codes name the opponent's pair number**, not their
standing. The wall chart is ordered by pair number and shows that column,
so a reference points at a row you can find on the page; the standings
are ordered by score, where a pair number would point at nothing visible.
The games are the same on both sheets — only the way the opponent is
identified differs.

Pair numbers are what the pairing engine and the rating report use, so
they are what appears in the file you send to US Chess. Nothing about
this display choice changes what is submitted.

On the printed versions of both sheets the title column appears only
when somebody in the section actually has a title — these tables are
already wide, and a column of blanks would squeeze the round codes for
nothing.

### Crosstable

Round robin, double round robin and quad sections get a **📄 Print
crosstable** button, on both the **Pairings** and **Wall Chart** tabs.
It produces the traditional all-play-all grid: one row and one column
per player, where the cell where a row meets a column is what that
row's player scored against that column's player — **1**, **½** or
**0** — followed by score and tiebreaks.

- **Rows and columns are both in pairing-number order**, and the
  opponent columns are headed by those same numbers, so the grid is
  read number against number.
- **The diagonal is shaded**, because a player cannot meet themselves.
  That is different from a blank cell, which means the game has not
  been played yet.
- **A double round robin shows both games in one cell**, in round
  order — so `1 ½` means a win in the first meeting and a draw in the
  second.
- **Title, name and rating share one column** — "GM He, Anthony 2500" —
  as on the pairing sheet. Whether the title and rating appear follows
  your pairing-column settings, so the two reports describe a player the
  same way. The score, colour and due-colour options are ignored here:
  colours vary by opponent on a crosstable, and the total already has
  its own column.

The button does not appear on Swiss sections. In a Swiss the players
never all meet, so a crosstable would be mostly empty; the wall chart
is the report for that format.

**The crosstable also shows in a window.** Open the Pairings or Wall
Chart tab in a window (**⧉ Open in Window**) and the crosstable appears
above the grid, with a **Show crosstable** tick box to hide it. It is on
by default for round robin and quad sections, because for those formats
it is usually the view you want — the round below is one slice of it.

### Prizes

The **Prizes** tab handles the prize fund. **✏ Edit prizes…** defines the
prizes, **🧮 Calculate prizes** distributes them according to the
standings, and **🗑 Clear prizes** starts over.

---

## Printing and reports

**📄 Print** produces a PDF of whatever you are looking at — pairings,
standings, wall chart, roster or prizes. Reports are landscape by default
because tournament tables are wide, and each carries the event name,
section, time control, location, dates, a page number and a timestamp.

**Print Setup** controls page options, and the grid and print fonts can
be adjusted independently, which is useful when a wall chart is one
column too wide for the page.

Under **Settings → Printing**, **Use ASCII-only output** replaces the ½
glyph with plain text for printers and downstream systems that cannot
render it.

### The event QR code

When the event has NA Chess Hub details filled in, pairings, standings
and wall chart reports print a small QR code in the top-right corner,
labelled **Scan for pairings**. It opens the event's page on a phone, so
a player can check where they are sitting without pushing to the front
of the crowd around the wall chart. The page needs no sign-in.

The QR appears **only** when the event has both a hub event ID and a
passcode. Strictly the page needs just the ID, but an ID with no
passcode is usually one typed in and never linked to a real hub event —
printing a code that leads nowhere is worse than printing none, because
the player has walked away before finding out. Local events simply get
no QR, which is not an error.

Roster, prizes, byes and the crosstable do not carry the QR. Those are
your documents rather than the ones players crowd around.

The same code appears in the **popped-out Pairings, Standings and Wall
Chart windows**, with a **Show event QR** tick box to hide it. TDs often
throw those windows onto a projector or a large screen, and a QR on the
big screen can be scanned from across the hall by a dozen people at
once.

---

## Saving your work

### FreePair saves as you go

There is no "unsaved changes" state to worry about. Every change is
written to your event file as you make it, and the **Save event** dialog
exists mainly to tell you where that file is.

That dialog also offers:

- **Rename (Save As)…** — give the event a new file name or folder. From
  then on every change goes to the new file; the old one is left exactly
  as it is.
- **Save a copy…** — write a frozen snapshot elsewhere while you keep
  working in the current file.
- **Earlier versions…** — open a checkpoint.

### Earlier versions and undo

Before each significant change — pairing or deleting a round, importing
players, deleting a section — FreePair quietly saves a checkpoint. You
can open one to compare against your current event, or go back to it.

**↶ Undo** and **↷ Redo** cover ordinary editing within a session.

---

## NA Chess Hub

### Roster sync

If your event is registered on NA Chess Hub, FreePair can pull the entry
list and keep it in step as players enter, withdraw or change section.

FreePair checks periodically and shows a red count beside any section
with changes waiting, and beside the Sections header for the event total.
Clicking a section's badge opens the review for that section; clicking
the header total opens the whole event. You review every change before
anything is applied.

Only changes you can act on are counted. A walk-up entrant who is not on
NA Chess Hub is a permanent difference, not a pending change, so it does
not sit there pinning the badge on forever. If a check fails, the last
known counts stay on screen rather than resetting to zero — "no changes"
and "could not check" are not the same thing.

Turn the alerts on or off, and set how often they run, under
**Settings → Alert when NA Chess Hub roster has updates to sync**.

### Publishing pairings and results

Publishing puts pairings and results on the event's public page so
players can follow along. You can set new events to publish
automatically under **Settings → Online publishing**.

### Cloud backup

**Save a copy (backup) to NA Chess Hub** stores a copy of the event file
in the cloud so you can open it from another computer. It is a backup,
not a publish — it does not make pairings visible to spectators.

It is a snapshot: if you keep working after saving one, the cloud copy is
out of date until you save another.

Optionally the copy can carry your display settings — theme, fonts, score
style, column choices, filters and pop-out window positions — so the
event looks and prints the same on another machine.

---

## Filing the USCF rating report

When the event is over, the USCF export produces the rating report.
FreePair fills in what it knows from the event and asks you for the
affiliate and TD details it cannot infer.

---

## Filing the FIDE rating report

**🌍 Export FIDE Rating Report** writes a TRF report for the selected
section, ready to send for FIDE rating. FreePair fills in everything the
event already knows and asks you for the rest: host city, federation,
chief arbiter and any deputies. Those are remembered for next time.

**This is not the same as "Export Sections to TRF."** Both write a
`.trf` file, but that one is *engine input* — what FreePair hands to the
pairing engine — and it contains only the players still being paired,
numbered the way the engine wants them. The rating report contains every
player who played a game, including those who withdrew, numbered the way
your wall chart numbers them, and carries the FIDE IDs, federations,
titles and birth dates FIDE rates people by.

If any player has no FIDE ID, the dialog lists them before you export.
You can still export — their games appear in the report — but **FIDE
cannot rate a player it cannot identify**, so it is worth fixing the IDs
first if you can.

FreePair knows which ID column is which. Event files store a player's
`ID` and sometimes `ID2` without recording which federation each belongs
to, so FreePair works it out during ID verification. The FIDE report
takes the FIDE ID from whichever column actually holds it, and the USCF
report does the same in reverse. Neither will guess: if verification has
not established what a column holds, the ID is left out rather than
filed against a stranger's record.

The dialog separates two things that look the same in the finished file:

- **No FIDE ID** — the player genuinely has none, or verification has
  confirmed their IDs are not FIDE ones. Nothing to fix in FreePair.
- **IDs not verified** — the player has an ID, but FreePair has not
  established what kind it is, so it will not be reported. **Cancel, run
  Verify IDs and ratings on the roster, then export again.** These
  players may well have a FIDE ID sitting in the file.

Ratings reported are FIDE ratings. If a player has a FIDE rating
recorded, that is what is sent; a player's USCF rating is never reported
to FIDE, and a player with no FIDE rating is reported as unrated.

---

## Settings

Settings apply to the application, not to one event. They are split
across tabs:

- **Files** — where events are saved, and whether you are prompted for
  a folder each time.
- **Pairing** — pairing-engine behaviour, re-showing the pairing-engine
  notice, and the USCF and FIDE engine binaries. The binaries are
  bundled with the installer and normally need no attention.
- **Display** — theme, the size of app text and grid text, and
  ASCII-only output for printing.
- **Online** — publishing defaults, the NA Chess Hub address, and the
  service used to verify IDs and ratings.
- **Updates** — update checks, the release channel, and your current
  version.

---

## Troubleshooting

**A dialog is too tall for my screen.** Dialogs scroll and can be
resized. The action buttons stay visible at the bottom.

**A rule citation opens the rules document at the top instead of the
rule.** The rules reference ships with the app and is opened in your
default browser. If it has been removed from the installation, FreePair
falls back to the online copy, which needs a connection.

**Ratings did not update.** Rating refresh needs an internet connection
and verified IDs.

**A player was paired against someone they already played.** Open **Why
this pairing?** on that board — the explanation names the rule and the
constraint that forced it. Genuine avoidable rematches are a bug worth
reporting.

**I paired the wrong round.** Delete the round. A checkpoint is taken
first.

---

## About this guide

This guide describes FreePair **v0.71.20260817**. It is updated whenever a
change affects what you see or do.

The copy that ships with the app is the one that matches your installed
version. An online copy is published with each release; if you are
reading that one, check the version above matches the FreePair you are
running.

