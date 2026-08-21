# FreePair user guide

**Applies to FreePair v0.75.20260821**

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

### Events from an online registry

Creating an event from an online registry, and opening a cloud-saved
one, both ask for an **Event ID** and a **Passcode**. Once you have
entered the event ID, the **Passcode** caption becomes a link to that
event's own page, where the passcode is shown — click it if you do not
have the passcode to hand, and copy it from there rather than hunting
through email. Both fields also have a 📋 button to paste from the
clipboard.

### Event details

The event's **Overview** covers the name, dates, location, time control
and the defaults new sections inherit. Time control and location appear
on printed reports, so they are worth filling in even though pairing does
not use them.

These settings are grouped into tabs, because there are more of them than
fit comfortably on one screen:

- **Basic** — title, dates and venue; the format, time control, rounds,
  half-point byes allowed, rating system, pairing rule and pairing
  engine; and the organizer's name and ID.
- **Team Event** — team and match size and the default board order. Turn
  on **Team Event** first; the rest of the tab stays hidden until you do.
- **Results Publishing** — the NA Chess Hub event ID and passcode, and
  whether to check NA Chess Hub for roster changes before pairing.
- **Starting Boards** — the first physical board number for each section.

Everything saves as you type, on whichever tab you are on.

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
FreePair works out each section's starting board from the ones before it
and **uses that recommendation automatically** — pairing a round does not
stop to ask. Sections therefore stack cleanly with no setup at all, which
is what most events want.

If you do set a board yourself — on the section's Overview tab, on the
event page's **Starting Boards** tab, or with **Renumber section starting
boards** — that number is yours and FreePair will not move it. A section
put in a different room on board 40 stays on board 40 for the rest of the
event. This is the distinction that matters: FreePair fills in the blanks
and never overrules a number you typed.

To be asked once per section each round instead, turn off **Always use
the recommended starting board** in Settings.

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

**Only national IDs are checked.** US Chess, FIDE and CFC IDs are digits
only, so an ID carrying letters is not one of them and is never looked
up as one — it is left unchecked and labelled as such rather than
guessed at. An ID with the shape of an NWSRS one is recognised and the
column is labelled **NWSRS**; use **Roster Update** to check those
against the NWSRS database instead. If a section was previously
mislabelled this way, running the check again corrects it.

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

**Checking in on site and checking in online.** If your event is on NA
Chess Hub, players can check themselves in there, and a roster sync
brings those check-ins across. Most players still check in with you at
the desk, and the website has no way of knowing that.

So a sync **never clears a check-in you made**. It reports the
difference — "checked in here, but not on NA Chess Hub" — as a note you
cannot apply, in the same way it reports a walk-up entrant the site has
never heard of. Your desk is the authority on who is standing in the
room.

If somebody is genuinely not there, clear their check-in on the roster.
That stays a deliberate click, because the two mistakes are not equal: a
player wrongly marked present is spotted at the board and costs a
minute, while a player wrongly marked absent is not paired at all, and
usually nobody finds out why until the round has started.

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

### Scholastic events rated by NWSRS

NWSRS is the NorthWest Scholastic Rating System used across Washington,
Oregon and Idaho. FreePair opens events rated `NW`, and events rated on
NWSRS alongside a national system (`USCF_NW`, `USCF_FIDE_NW`,
`CFC_FIDE_NW`, `USCF_CFC_FIDE_NW`).

**An NWSRS rating is on its own scale.** It is not a US Chess rating, not
a FIDE rating, and there is no conversion between them — the same child
is usually rated several hundred points lower on the NWSRS scale than on
the USCF one. FreePair never mixes them and never converts. The Roster
columns are named **NWSRS Rating**, **NWSRS ID**, **NWSRS School** and
**NWSRS Grade** for exactly this reason, and on an NWSRS-paired section
the pairing rating column reads **Rating [NWSRS]** so you can see at a
glance which scale you are looking at.

**Two forms of the ID, and only one of them is the player.** A child's
permanent NWSRS ID is four characters — `9717`. What coaches, wall
charts and the event file usually hand you is an eight-character form,
`TSTN9717`, made of the school code (`TST`), the grade letter (`N`) and
the permanent ID. **The first two parts change** — the school when a
child transfers, the grade every single school year — so the long form
is only good for one season. FreePair shows the last four as the ID and
matches players on them, which is why the same child appears once and
not twice when you open files from two different years.

**The grade is a letter, not an age.** `A` is kindergarten, `B` is grade
1, and so on up to `N`, which means beyond grade 12. FreePair shows it
the way you would say it — `K`, `1` to `12`, or `Adult`. A letter
outside `A`–`N` leaves the grade blank rather than guessing, because a
guessed grade puts a child in the wrong section.

**Most players have no NWSRS record, and that is normal.** Nobody is
held out of a pairing for it, and no warning is raised.

**Clicking an NWSRS ID opens that player's profile.** The ID cell is a
link to the player's page on the NA Chess Hub player database, the same
way a US Chess ID links to its ratings page. This is the quickest way to
confirm you have the right child when two of them share a name.

#### Which rating a section is paired on

Every section has a **Rating System** setting on its **Overview** tab.
It starts on **Inherit from event**, and the grey text beside it tells
you what that currently resolves to, so an inheriting section still
states what it inherits.

**A file that names a rating system per section shows it.** Newer
SwissSys files record a rating system on each section as well as on the
event. When yours does, the section opens showing that value rather
than **Inherit from event**, and the grey text reads **Pinned** — the
combo reports what is in the file, not what FreePair would have guessed.
Earlier versions ignored the per-section entry entirely, so a section
could state NWSRS in the file and still be seeded from US Chess ratings
without saying so. Choose **Inherit from event** if you would rather the
section follow the event setting from here on; that also clears the
entry from the file, so it stays cleared next time you open it.

**Set it per section when the sections differ.** A scholastic weekend
routinely runs NWSRS-only novice sections beside USCF-rated championship
ones. Before this setting existed FreePair had one answer for the whole
file, which meant one of those groups was seeded on the wrong numbers.

**The section list and the event list are the same list.** Whatever you
can choose for the event on **Event Config**, or when creating an event,
you can choose for a section — same systems, same order, same names. The
two used to disagree, with the event screen showing raw codes like
`USCF_CFC_FIDE_NW` and sections showing a shorter list of friendlier
names, which made it hard to tell you were even looking at the same
setting. The only difference now is the first row: an event can be
**Not recorded**, a section can **Inherit from event**.

**Choosing "NWSRS only" moves the columns.** Each player's NWSRS ID and
rating move into the pairing **ID** and **Rating** columns, and their
national ID and rating move out to **ID2** and **Rating2**. This is not
cosmetic: the Rating column is what the engine seeds and pairs on, so
until the NWSRS number is in it, an event advertised as NWSRS-rated is
being run on US Chess ratings. Nothing is lost by the move — pick a
different rating system and the national values come back.

You will most often not have to touch this at all. An event whose rating
type is `NW` arranges its columns this way when you open it, because the
file itself usually arrives with the national pair in front.

Two details worth knowing:

- **A player with no NWSRS ID yet keeps the rating they have.** Mid-season
  there are always children waiting on one. Emptying their pairing rating
  to make the section tidy would seed them bottom board for a clerical
  reason, so their existing values stay put and **Check Section** reports
  the mixed scale instead.
- **A player who has an NWSRS ID but no NWSRS rating is unrated**, and
  shows a rating of 0. They are genuinely unrated on the scale this
  section is run on, and their US Chess number cannot stand in for it.

**Dual-rated sections move nothing.** `USCF_NW` and the other
combinations pair on the national rating; their NWSRS values stay in the
secondary columns for display, which is all they are for.

**The setting locks once round 1 is paired.** It decides the ratings the
existing pairings were seeded from, and those cannot be retro-fitted —
you would be left with a wall chart whose first round was computed from
numbers the file no longer contains.

#### What Check Section and Check Event look for

Run **Check Section** or **Check Event** after opening an NWSRS file.
The audit reports and never blocks — every one of these is survivable,
and none of them stops you pairing:

- **NWSRS ids are the right shape** — an ID that is neither 4 nor 8
  characters. The warning quotes the four characters FreePair used, so
  you can see what it assumed.
- **NWSRS ids agree with school and grade** — the school and grade
  inside an 8-character ID disagreeing with the ones recorded beside it.
  This means the file was built from a previous school year. FreePair
  uses the entry's own school and grade for sectioning and the last four
  characters for identity, so nothing is lost — but the file disagrees
  with itself and you should hear that from us rather than from a
  parent.
- **NWSRS grades are readable** — a missing or out-of-range grade
  letter.
- **Ids are in the column they belong to** — an NWSRS ID sitting in a US
  Chess column, or the reverse. This is the classic symptom of a
  mis-built file, and it matters: left alone, that value would be
  submitted to US Chess as a member number.
- **One rating scale per section** — a rating from one system seeding a
  section measured on another. This is the one worth reading carefully,
  because it is invisible on the pairing sheet: every number still looks
  like a rating, but the seeding order means nothing.
- **Each NWSRS player entered once** — the same permanent ID on two
  entries, including when the two carry different eight-character forms.

One case is deliberately **not** warned about. If your event was set up
to seed on the higher of a player's two ratings and the NWSRS one is
higher, the seed will equal the NWSRS number — which is what you asked
for, and is indistinguishable from a mistake. Warning about it would
train you to click past this check.

#### Refreshing NWSRS ratings

**Refresh ratings** on the Roster tab now reaches NWSRS sections, which
it previously skipped entirely. The NWSRS rating is written only to a
column the file says is NWSRS, so it can never land in a US Chess, FIDE
or CFC seeding.

Unlike US Chess, there is no second place to look a child up: if their
ID is not in the imported NWSRS rating file, the answer is simply "no
record". The rating already on file is left exactly as it was, the
player is noted as skipped, and nothing is erased.

---

## Pairing rounds

### Before you pair

Check that the roster is complete, IDs are verified, byes are requested
and — if you use it — check-in is done. Pairing is reversible, but it is
much less disruptive to get the pool right first.

### Pairing a round

**Pairing Operations** pairs the next round for the section. To pair
every section at once, use the pair-all-sections action from the event
page. **Sync Roster with NACH** is on this menu as well as the Roster
tab's **Roster Update** menu, since checking for late entries is
normally the step just before pairing.

**If "Pair Next Round" is greyed out, hover over it.** The tooltip names
the specific thing standing in the way and where to fix it, rather than
leaving you to guess. The usual answers are:

- **The section is scheduled for 0 rounds.** Set **Rounds** on the
  section's **Overview** tab. An imported file can arrive with this
  unset, which is easy to miss because nothing else looks wrong — the
  players are all there and no round has been paired.
- **A round still has results outstanding.** Finish entering them on the
  **Pairings** tab; the tooltip says which round.
- **All the scheduled rounds are complete.** Raise **Rounds** on the
  **Overview** tab if the event is playing another.
- **The roster is empty, or has one player.** Add players on the
  **Roster** tab.
- **A player is soft-deleted.** Restore them or delete them permanently
  before round 1.

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

### Checking a round, a section, or the event

There are three ways in, all doing the same job at different sizes:

- **🔍 Check this round** — in the pairing preview, on the round you are
  about to accept.
- **🔍 Check Section** — Pairings tab → *Pairing Operations*. Every round
  this section has, plus the things no single round can answer.
- **🔍 Check Event** — *Event Operations*. Every section, plus the checks
  that compare them. Worth a click before you print or publish.

It looks for:

- The same player on two boards, or both paired and given a bye.
- A withdrawn or deleted player who still has a game.
- A pairing against somebody who is not on the section's roster, or a
  player paired with himself.
- An active player with neither a game nor a bye this round.
- A **rematch** — it names the round the two already met.
- A **third game in a row with the same colour** (USCF 29E5f). A bye in
  between does not break the run, because a bye is not a game.
- Two games on one board, or a board below the section's own range.
- A **second full-point bye**, or more half-point byes than the event
  allows.
- A pairing your section is set to avoid — same team, same club, or a
  do-not-pair request the engine could not honour.
- A round still missing results, and results entered for a later round
  while an earlier one is unfinished.
- The **same person entered in two sections** and still active in both.
- A section that has fallen behind the rest of the event.

#### It shows what it checked, not just what it found

The panel has two tabs. **Found** lists the problems, ranked **Must
fix**, **Check** and **Note**, worst first. **Checked** lists *every*
check with what it examined — "Clear — 24 pairings checked", "2 found —
48 colour assignments checked".

That second tab is the point on a healthy event. "No issues found" asks
you to take the software's word for it; it also hides the case that
should worry you most, which is a check that never ran. Each line says
so plainly when it did not apply — "Not checked — this section does not
require check-in", "Not checked — the event has only one section" — and
a count far smaller than the round in front of you is how you catch a
check that quietly did nothing.

The severity bands are meant honestly: "Must fix" is for things that
cannot be played as written, not for anything unusual. A list that
shouts about everything is one you would learn to ignore.

**It never blocks you, and it never changes anything.** Even with a
"Must fix" on screen you can commit the round and publish it — you may
have a good reason, and there is no time to argue with software at
9:02am with a room waiting. FreePair reports; you decide. Equally,
nothing is repaired for you: no colour is flipped and no pairing is
rewritten behind your back.

**Copy report** puts the verdict, everything found and the full list of
checks on the clipboard, which is the quickest way to send it to another
TD or keep it with your notes.

### Pairing sheet columns

Which columns appear, in what order and under what heading, is yours to
set. Open it from **Pairing Operations → Advanced options** to configure
the on-screen grid and the printed sheet side by side, or from
**Page setup → Columns…** when the report is the pairing sheet, to
adjust the printed sheet alone.

The two views want opposite things of the result columns, and FreePair
sets them up that way:

- **On screen** the **Result / Score Selector** is shown. That is the
  dropdown you enter results with.
- **On paper** the two **write-in** columns are shown instead — a blank
  box flanking each player, for the scorer to fill in by hand. The
  selector is left off, because on a blank sheet it prints a third,
  empty result column that belongs to neither side.

If you tick or untick one of those three against the recommendation,
FreePair explains why it is unusual and asks you to confirm. Every other
column is a matter of taste and is never questioned.

**Copy from grid** copies the grid's arrangement to the printed sheet,
but deliberately leaves the result columns as they are, so copying a
layout across cannot cost you the write-in boxes.

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

### Pairing quality

*Why this pairing?* explains one board. **📊 Pairing Quality** does the
same job for a whole round, a whole section, or the whole event — the
numbers you need when a player is unhappy and "the computer did it" is
not going to be enough.

Two ways in:

- **📊 Pairing Quality** — Pairings tab → *Pairing Operations*, for this
  section.
- **📊 Pairing Quality** — *Event Operations*, for every section at once.
  You get event-wide totals first, then each section underneath.

It reports on:

- **Colours.** How many players got the colour they were due, how many
  are level on colours, and how many are on a second or a third game of
  the same colour.
- **Floats.** Who was paired above their score and who below, and who
  floated the same way two rounds running.
- **Repeat pairings.** Any that had to be allowed, and how many were
  steered around.
- **Score groups.** How many boards were paired inside their own score
  group rather than across two.
- **Constraints relaxed.** The point of the whole thing — the rules that
  had to give, one by one, with the player names and the board.

**A zero here means measured and zero.** Each figure is shown against
what it was drawn from — "12 of 14 got the colour they were due", not
just "12" — so you can tell a clean round from a measurement that never
ran. Where something genuinely does not apply, you get a dash and a
reason rather than a nought: a round-robin section reports no floats,
because the schedule was fixed before a move was played and nobody
floated.

#### "Steered around" is a narrow count on purpose

The repeat-pairings figure counts pairs of players who went into the
round **on the same score**, had already played each other, and were not
paired together. Those are the repeats that were genuinely on the cards.
Two players who met in round 1 and are now three points apart were never
going to be paired anyway, and counting them would turn a useful number
into a meaningless one.

#### Where each figure came from

This is the part worth understanding before you quote the report at
anyone.

FreePair's pairing engines write notes as they pair — "no rematch-free
pairing existed in this score group", and so on — and those notes are
saved inside the event file. When a note is there, the report shows it
and marks the line **engine recorded**.

When it is not there, the report works the fact out from the pairings
themselves and marks the line **worked out afterwards**. Both are true;
one is the engine's own word and one is FreePair's reconstruction, and
the report will not blur them. You will see *worked out afterwards* on
everything in an event imported from other software, because another
program's reasons were never in the file to begin with. That is normal
and does not mean anything is wrong.

**It reports and nothing else.** It changes no pairing and stops you
doing nothing. A relaxed constraint is usually the rules working as
intended when something had to give — not a mistake.

**📄 Print as PDF** writes the whole report — totals, every round, and
every relaxed constraint — and **Copy report** puts the same text on the
clipboard for an email.

### Adjusting pairings

After a round is paired you can still swap colours, force or forbid
pairings, and re-pair. Forced pairings and do-not-pair instructions apply
to the current session.

### Unpairing a round

A round can be deleted if you paired it too early or the roster was
wrong. The confirmation is deliberately hard to dismiss by accident: it
is red, **Cancel** is the default button so pressing Enter backs out,
and the button that goes ahead is red too. If results have already been
entered for that round, the warning says how many — those have to be
typed in again from the score sheets if you re-pair. A checkpoint is
taken first, so the round can still be recovered from **Earlier
versions**.

Deleting pairings for the whole event at once works the same way.

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

**The Round list tells you where each round stands.** Every entry in the
**Round:** drop-down carries a mark:

- **✅** — every game in that round has a result.
- **⏳** — the round being played: some results are in, or none are yet
  but everything before it has finished.
- *no mark* — a round still ahead, waiting on the one before it.

This matters most in a round robin or quad, where the whole schedule is
paired at once and the list shows every round from the start. The
hourglass is the round to print, to call, and to collect results for;
the unmarked ones have not come round yet. Exactly one round is ever
marked with the hourglass, and once the last round is ticked the section
is done.

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

### Printing the whole event at once

Four items on **Event Operations** print every section in one go, so a
ten-quad scholastic does not mean ten trips through the section tabs and
ten save dialogs:

- **🖨 Print All Pairings**
- **🖨 Print All Standings**
- **🖨 Print All Wall Charts**
- **🖨 Print All Crosstables**

Each opens a chooser listing every section, ticked to start with. Untick
anything you do not want on the wall — the side-game, the practice
group. A section that cannot go in this particular report is listed but
disabled, with the reason under its status, so you can see FreePair left
it out on purpose rather than wondering where it went.

**Continue** then shows the same **Page setup** you get from any other
print button, on the same report — so an orientation or font size you
tuned on a section tab is the one the event-wide sheet uses. Cancel
there abandons the print but keeps any adjustment you made, because page
settings save as they change.

The file is written next to your event file, named after it — for example
`MyEvent-all-pairings.pdf` — and opens in your PDF viewer, where your
printer's own dialog takes over. Printing again overwrites it, since the
report is rebuilt from the event each time.

**Which round gets printed.** Sections in a mixed event are rarely on the
same round number: the quads may be on 3 while the Swiss is on 2. So the
pairings print defaults to **each section's latest paired round**, which
is what goes on the wall. Choosing a specific round number instead prints
that same round everywhere, and quietly skips any section that has not
reached it yet.

**Several crosstables per page.** A quad's crosstable is four rows, so one
to a page wastes most of the sheet — ten quads costs ten sheets. Set
**Crosstables per page** to 2, 3 or 4 and they stack down each landscape
page instead. The type is sized so the fullest page still fits, which
means asking for more per page gives you smaller print and less room to
write results in by hand. That is the trade; if you want the write-in
room, print one per page.

Only round robins, double round robins and quads have a crosstable at
all. A Swiss field never meets everyone, so those sections are listed as
unavailable here — print their **wall charts** instead, which show the
same games.

### The wallboard

**🖥 Wallboard** at the top right of the window, beside **Help** — or
**Event Operations → 🖥 Wallboard** — opens a full-screen display for a
projector or a spare monitor — the screen in the skittles room that
stops players crowding round the paper on the wall.

**Setting it up.** A short dialog opens first, and what you choose is
remembered for next time:

- **Sections** — which sections appear, and whether each shows pairings,
  standings, or both. Untick a side-game or a one-round exhibition to
  keep it off the screen entirely.
- **Pairing columns** — the wallboard's own, separate from the grid and
  the printed sheet. A projector read across a room has far less width
  than a sheet of paper, so fewer columns and larger type. **Rule lines
  between cells** is on by default and can be turned off here.
- **Standings columns** — place, player, rating, score, optionally a cell
  per round, and optionally the section's own tiebreaks. Both of the last
  two are off by default: at this size they crowd out the names for
  numbers most of the room is not reading.

  **Round results** put `W18W`, `D7B`, `H---` on the screen, the same
  cells as the Standings grid, with opponents cited by their *standings
  position* so a player can find the row they point at. It is the most
  expensive thing you can add — one narrow column per round, growing all
  event, on the screen with the longest names. Worth it in a four- or
  five-round event, where players read across their own row to check the
  board agrees with them; think twice in a nine-rounder.

  **Rule lines between cells** is on this tab too, and on by default.
- **Display** — rows on each screen, text size, and typeface.
- **Timing** — seconds on each screen.

**About the rule lines.** Each column tab carries its own tick, and both
start on. A wallboard is read from a distance and at an angle by
somebody hunting one name among sixty; ruled cells keep the eye on a row
all the way across a wide screen, which is why paper pairing sheets have
been ruled for a century. They matter more on standings than pairings,
and more again once round results are shown. Turn them off if you want a
cleaner board — it does photograph better — but check from the back of
the room first. **Restore default columns** puts them back.

Both column tabs have a **Restore default columns** button. Worth knowing
if the screen does not look the way this guide describes: your saved
choices are remembered from last time, and they win over any default
FreePair ships later. That button is how you get back.

**Closing the panel keeps your settings.** It says at the top that they
are remembered, and they are — **Close**, Escape and the window's own
close button all keep what you have ticked; the only difference is that
no wallboard opens. **Restore default columns** is the way to undo.

Set the text size by walking to the back of the room and reading the
screen from there — it is the only test that matters. A condensed
typeface fits a long name that would otherwise be cut, and a heavier one
survives a washed-out bulb; leave it on **System default** unless the
screen tells you otherwise.

If your choices would leave the screen blank — every section switched
off, or no pairing columns ticked — the dialog says so before you start,
rather than leaving you looking at an empty projector.

It then cycles by itself, updating as you pair and score, and needs no
attention once it is up.

A long section is split across several screens rather than shrunk or
scrolled, and the header says **Screen 2 of 3** so a player who arrives
part-way through knows to wait rather than assuming their name is
missing. Byes are listed with the boards, since a player with a bye is
looking for their name too.

**Which round it shows.** For a Swiss section, the round you last paired
— those are the board numbers the room is looking for, and an earlier
round still missing a result from a game that ran long must not pull the
screen backwards.

Round robins, double round robins and quads are different, because their
whole schedule is paired in one go. There the wallboard shows **the
first round that is not finished** — round 1 the moment the quad is
paired, then round 2 as soon as every round-1 result is in, and so on by
itself. Once the last round is complete it stays there, showing that
round with its results. Before, it showed the final round from the
moment the section was paired, which sent players to boards they were
not due at for another two hours.

**It uses columns you choose.** The wallboard has its own pairing and
standings columns, set in the dialog — deliberately not shared with the
printed sheet, because a projector read at thirty feet and a sheet of A4
read at arm's length have very different amounts of room.

**Pairings read outward from the result.** White's detail is printed
backwards — `[2.0 WB w] 1850 Stone, Andrew` — so that with the column
right-aligned it is the *name* that sits next to the result, not the
bracket. Black is printed the usual way on the other side. The row then
reads the way a pairing sounds when it is called: the two names either
side of the score, with ratings and colour history trailing off towards
the edges of the screen. The result column is given extra room on both
sides for the same reason — a score squeezed against a long name gets
read as part of it.

While it is open you can carry on pairing and scoring in the main
window. At the screen itself:

- **Esc** closes it.
- **Space** pauses, to leave a section up while people copy it down.
- **← →** step back and forward a screen.
- **F11** switches between full screen and an ordinary window.

**Two screens or one.** If you have a second display, the wallboard
opens full screen on it and leaves your working window alone. If you
have only one, it opens as an ordinary window you can move and resize —
a full-screen display covering the grid you are trying to enter results
into would be no use to anybody. Press **F11** when you are ready to go
full screen, and again to come back.

Standings only appear once a round is complete — before that they would
be a list of zeroes in seeding order.

### Putting the wallboard on a TV or on phones

**📺 Share…** — next to the Wallboard button at the top right — turns the
board into a web page that anything on the same network can open. It is
read-only: people can look, and cannot change the event.

Press **Start sharing** and FreePair shows an address like
`http://192.168.1.50:8080/` with a QR code beside it. Sharing stays on
until you turn it off or close FreePair, and the page updates itself as
you pair and score — nobody needs to refresh anything.

**It is the same board as the projector.** The shared page and the
full-screen wallboard read one set of settings, so they always show the
same sections, the same columns and the same rotation. **⚙ Wallboard
settings…** in this window opens the same setup panel described above.
Changes reach anyone watching within a few seconds; nobody has to
reload, and you do not need to stop and restart sharing.

What to do with the address depends on the screen:

- **A smart TV or a Fire TV stick** — open the TV's own web browser
  (Silk on a Fire TV, *Internet* on a Samsung, *Web Browser* on an LG)
  and type the address. Nothing to install.
- **A Chromecast, or a TV with Chromecast built in** — press **Open in
  my browser**, then use **Chrome or Edge's own Cast button** (⋮ menu →
  Cast) and pick the Chromecast. The browser does the casting; FreePair
  only supplies the page. This is also the route for Google TV and
  Android TV boxes, which have Chromecast built in but no browser.
- **Players' phones** — print the QR code and tape it up, or put it on
  the projector. Anyone on the network can then read the pairings from
  where they are sitting instead of crowding the wall.

**Why FreePair does not have its own Cast button.** Casting to a
Chromecast from a desktop program is not something the Chromecast
permits — Google publishes the necessary kit for phones and for Chrome,
and for nothing else. Amazon abandoned its equivalent years ago, and a
Fire TV was never a cast target in the first place. A web address, on
the other hand, is opened by Fire TV, Samsung, LG, every phone in the
room, *and* by Chrome, which then casts it for you. Handing you a URL
reaches more screens than a Cast button could.

**If nothing can reach it.** Most hotel and venue Wi-Fi deliberately
stops guests seeing each other, which blocks this — and blocks casting
too, so it is not something FreePair can work around. The reliable fix
is to stop using their network: turn on the laptop's own hotspot
(Windows: **Settings → Network & Internet → Mobile hotspot**) and join
the TV or the Chromecast to that. A phone hotspot does just as well. The
first time you start sharing, Windows may ask whether to allow FreePair
through the firewall — say yes, or nothing outside the laptop will
connect.

If the first address does not work, the window lists the others. A
laptop that is on Wi-Fi *and* in a dock has more than one, and only one
of them is on the same network as the TV.

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

The dialog is split into three tabs, because a save and a backup are
different things and mixing them in one list is what made TDs unsure
which file they were still working in:

- **Save** — where the event file is, and **Rename (Save As)…** if you
  want the event to carry on in a different file. Renaming leaves the old
  file exactly as it is; from then on every change goes to the new one.
- **Backup** — write a frozen snapshot, either to a folder on this
  computer or to NA Chess Hub. A backup never becomes the file you are
  working in.
- **Earlier Versions** — open one of the checkpoints FreePair takes
  automatically.

Every backup confirms what it did with **two links**: the backup it just
wrote, and the file you are *still working in*. Click either one and
FreePair shows it in its folder, or — for a cloud backup — opens the
event's file list on NA Chess Hub. Two links rather than one because the
question a backup raises is "which file am I in now?", and naming only
the backup answers half of it.

The backup link carries the caveat that matters: the snapshot is frozen
at the moment you took it and will not follow later changes.

Two things to know about the NA Chess Hub link: the copy there is a
backup, not a publish — it does not show pairings to spectators — and you
have to be **signed in as the event organizer or a TD for that event** to
see the file at all.

### Earlier versions and undo

Before each significant change — pairing or deleting a round, importing
players, deleting a section — FreePair quietly saves a checkpoint. You
can open one to compare against your current event, or go back to it.

**↶ Undo** and **↷ Redo** cover ordinary editing within a session.

### The decision log

**Event Operations → 📓 Decision Log** keeps a record of the
discretionary calls you make, with the reason you gave. It covers:

- Everything you change in the **pairing preview** — swapping colours,
  swapping boards, moving a player between seats, moving the full-point
  bye, turning a pairing into byes. A swap you made by overriding the
  rematch warning is recorded as exactly that.
- A **withdrawal** or a **reinstatement**.
- A **result changed after one had already been entered**.
- **Byes** granted, removed or changed.
- **Moving players** to another section.
- **Deleting** a round, a section's rounds, or a section.
- **Renumbering** starting boards.

When you make one of these calls FreePair asks you why. **You can always
skip.** A prompt you cannot dismiss only ever gets nonsense typed into
it, which is worse than a blank, and a reason can be added later: any
entry without one shows *"No reason given — click to add one"*, and
clicking it opens the box.

For the pairing preview, the note you type into the **TD manual
override** box *is* the reason — it is not asked twice. If you skipped
that note, or changed something that does not raise it, you are asked
once when you accept the round, covering everything still unexplained.
If you **cancel** the preview, nothing is recorded, because the round
never existed.

Ordinary work is deliberately not recorded. Entering a result for the
first time, editing a phone number, checking a player in — none of that
appears. A log of everything is a log nobody reads.

The log is **saved inside your event file**, so it survives closing the
event, a restart, or a crash — which is exactly when you would want it.

**Entries go away with the thing they describe.** Undo removes an entry
along with the change it undid, and deleting a round removes the
decisions you made about that round — the entry recording the deletion
says how many went with it. The log always tells you what your event
actually contains, and never claims something that is no longer there. A
log that is confidently wrong is worse at a hearing than no log at all.

**Copy log** puts the whole thing on the clipboard, and **Save as PDF…**
writes it beside your event file — the version to attach to an appeal,
send to a rating official, or keep with your own records. The PDF names
every decision, its time and place, and either the reason you gave or
"No reason given", so a reader can tell a decision you chose not to
explain from one the printout left out.

#### Turning the log off

It is on by default. To switch it off, either untick **Record my
decisions, and ask why** in **Event Details → Options**, or tick **Stop
recording decisions for this event** on any of the "why?" prompts — the
moment you decide you would rather not be asked is usually the moment
you are being asked.

Turning it off stops **every** "why?" prompt, including the one for a
hand-edited pairing, and removes **Decision Log** from Event Operations.
Hand-edited boards are still marked as a TD override in "Why this
pairing?", just without a note of your own.

It **does not delete anything already recorded**: turn it back on in
Event Details and your earlier decisions are still there.

The setting belongs to the event, not to FreePair, so it travels with the
file — a club that never wants the prompts sets it once per event, and an
event handed to another TD keeps whatever you chose.

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

Most of what is counted is something you can act on. A walk-up entrant
who is not on NA Chess Hub is a permanent difference, not a pending
change, so it does not sit there pinning the badge on forever. If a
check fails, the last known counts stay on screen rather than resetting
to zero — "no changes" and "could not check" are not the same thing.

**Bye requests that arrive too late** are the exception. If a player
asks on NA Chess Hub for a bye in a round you have already paired, it
cannot be granted as asked — the pairings for that round already exist.
FreePair reports it anyway, marked **Too late** and with no Apply
checkbox, because you need to know the player asked. The note tells you
what the player actually got for that round, which is often not the same
thing: a player who asks for a half-point bye after the round is paired
may already have been given a full-point bye by the pairing, or may have
played a game. Your options are to delete the round and pair it again,
or to tell the player the request came in after pairing.

A request that the file already satisfied is **not** reported. If the
player asked for a half-point bye and got exactly that, the two agree,
even though the request itself may no longer appear in the roster's HPB
column once the round has been scored.

Turn the alerts on or off, and set how often they run, under
**Settings → Alert when NA Chess Hub roster has updates to sync**.

**Play a sound when the count changes** sits under that option and is
off until you turn it on. It uses your computer's own notification
sound, so it follows the system volume and Do Not Disturb — handy when
you are working on pairings and not watching the badge. It plays in
either direction, since an entry being withdrawn is worth hearing about
too, and it stays quiet when your own sync clears the count.

### Publishing pairings and results

Publishing puts pairings and results on the event's public page so
players can follow along. You can set new events to publish
automatically under **Settings → Online publishing**.

### Cloud backup

**Save a copy (backup) to NA Chess Hub** stores a copy of the event file
in the cloud so you can open it from another computer. It is a backup,
not a publish — it does not make pairings visible to spectators.

To see the file on NA Chess Hub you must be signed in as the event
organizer or a TD for that event; it is not on the public event page.

It is a snapshot: if you keep working after saving one, the cloud copy is
out of date until you save another.

Optionally the copy can carry your display settings — theme, fonts, score
style, column choices, filters and pop-out window positions — so the
event looks and prints the same on another machine.

---

## Choosing which sections to report

All three rating exports — USCF, FIDE and NWSRS — start by asking which
sections to include.

**This is there so a rating report contains the event and nothing else.**
Most events collect a section or two that are not part of what is being
rated: a side-game for players knocked out early, a practice group, a
section you made to try a setting out. Before, the only way to keep those
out of a submission was to delete them first, which meant losing them
from your own records too.

**Everything that can be reported starts ticked**, so if you have nothing
to leave out, press Continue and you are done. Untick anything that
should not be submitted. Nothing is deleted and your event file is not
touched — the choice applies to this one export.

Sections you cannot tick are shown with the reason under their status:

- **Deleted** — its games are already being reported by whatever replaced
  it, such as the quads it was split into or the section it was merged
  with. Including it would submit those games twice.
- **No players** — there is nothing to report.

A section with **no results yet** can be included, but you will be warned
before you continue, because it is usually the practice group you meant
to leave out.

The FIDE report covers one section, so its chooser lets you pick exactly
one. It starts on the section you had open.

---

## Filing the USCF rating report

When the event is over, the USCF export produces the rating report.
FreePair asks which sections to include, fills in what it knows from the
event, and asks you for the affiliate and TD details it cannot infer.

The ID check that runs before the export looks only at the sections you
chose, so leaving a section out also stops it warning you about IDs in
it.

It writes **three files** next to your `.sjson`: `THEXPORT` (the event),
`TSEXPORT` (the sections) and `TDEXPORT` (the players and their games).
Each is a `.DBF` with your event name added on the end — for example
`TDEXPORT_Spring Open.DBF` — so several events can be exported into the
same folder without overwriting one another. Send all three; the rating
office reads them together, and the names before the underscore are what
it recognises them by, so do not rename them.

---

## Filing the FIDE rating report

**🌍 Export FIDE Rating Report** writes a TRF report for one section,
ready to send for FIDE rating. FreePair asks which section, fills in
everything the event already knows and asks you for the rest: host city,
federation, chief arbiter and any deputies. Those are remembered for next
time.

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

## Filing the NWSRS rating report

**🎒 Export NWSRS Report** on **Event Operations** writes the file NWSRS
needs to rate your event. It appears only when something in the event is
NWSRS-rated, so on a purely US Chess or FIDE event you will not see it.

**NWSRS rates from the event file itself.** There is no separate report
format to fill in, so the export asks you two things only: which sections
to include, and where to put the file. FreePair saves your event first,
then writes the report wherever you choose.

**The report is written in SwissSys's own format, and opens in SwissSys.**
It is not simply a copy of your event file. FreePair's file carries
things SwissSys does not use and does not expect, and a file with those
in it is refused outright — *File error: Program. (Unrecognized file
structure)*, or *Unable to load tournament file (section 1)*. So the
report is rebuilt to the shape SwissSys itself writes, and carries only
what SwissSys writes.

What that leaves out is everything that is FreePair's business rather
than the rating system's: your decision log, your column layouts, your
USCF affiliate details, the display settings, your tiebreak order, your
half-point-bye allowance. The decision log in particular is your own
reasoning about calls you made during the event, which is nobody's
business outside the tournament.

What it keeps is the event, the players and every game: each player's
ID, rating, club, grade and contact details, and their result in every
round exactly as recorded — same opponent, same colour, same board,
same result. Nothing about the games is recalculated on the way out.

**Your NWSRS IDs are put where NWSRS reads them.** When a section is
paired on NWSRS ratings, FreePair keeps the NWSRS ID and rating in the
main **ID** and **Rating** columns, because those are the numbers it
pairs from. SwissSys files are arranged the other way round — the US
Chess ID in **ID**, the NWSRS ID in **ID2** — and that second column is
the only place NWSRS looks for it. The report swaps them back for you.
You do not need to do anything about this, and your event file is not
changed; but it is worth knowing that the report and your event will
show those two columns the other way round.

**FreePair will not export a report with no games in it.** If no section
has a completed round, the export stops and says so rather than writing
a file full of players and empty results. That file would look right —
correct size, correct name, opens fine — and would tell NWSRS nothing.
If a section will not pair, hover **Pair Next Round** to see why; a
section scheduled for 0 rounds is the usual reason.

**A save dialog opens with the name already filled in.** The report is
named `NWSRSReport_<your event name>.sjson`, and the dialog starts in
your event's own folder, so pressing Save without changing anything puts
it beside the event. Spaces in your event name become underscores so the
file survives being mailed and unzipped. Afterwards FreePair opens the
folder with the file selected, ready to attach to an email.

**Keep the name FreePair suggests.** You can rename it in the dialog,
but the name is how NWSRS matches the submission to your event, so
changing it makes more work at their end, not less. The part worth
changing is the folder.

**The report is a `.sjson`, the same as your event, and opens in
SwissSys like any other tournament file.** That is what NWSRS expects to
receive. It does mean the submission and the working event look alike,
so take the moment to notice which one you have open — if you find
yourself entering results into `NWSRSReport_…`, you are in the snapshot,
and the real event has not moved. Saving the report somewhere other than
your working folder is the simplest way to avoid the question.

Export it after the last result is in. The report is a snapshot: if you
enter another result afterwards, export again — the file already written
will not update itself.

**Withdrawals still show, but not as a flag.** A player who withdrew is
in the report with the games they played and the byes they took, exactly
as your wall chart shows them. FreePair's own "withdrawn" marking is one
of the private entries that is left out, so the results tell the story
rather than a separate field.

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

This guide describes FreePair **v0.75.20260821**. It is updated whenever a
change affects what you see or do.

The copy that ships with the app is the one that matches your installed
version. An online copy is published with each release; if you are
reading that one, check the version above matches the FreePair you are
running.
