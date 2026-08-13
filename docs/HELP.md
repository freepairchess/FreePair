# FreePair user guide

**Applies to FreePair v0.69.20260813**

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
under **Settings → Auto-update → Check for updates on startup**, or use
**Check now** at any time. If you want early builds, tick **Include
pre-release builds (beta channel)** — otherwise you only see stable
releases.

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

### Reviewing before you commit

Pairings are shown for review before they become final. In this preview
you can:

- See every board with colours and ratings.
- Swap colours on a board.
- Convert a pairing to a half-point bye.
- Force a specific pairing, or forbid one.

Nothing is written to the event until you accept.

### Why this pairing?

Every board has a **?** button. It opens the **Why this pairing?**
dialog, which explains the board in plain language:

- The **pairing reason** — why these two players were put together.
- The **colour assignment** — why each player got the colour they did.
- The **round context** — floats, byes and anything else that shaped the
  round.

Each line ends with the USCF rule that drove the decision, as a clickable
citation that opens the rules reference at that exact rule. This is the
fastest way to answer a player who is questioning their pairing.

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

### Wall chart

The **Wall Chart** tab shows the traditional cross-table: every player's
round-by-round result, opponent and colour.

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

## Settings

Settings apply to the application, not to one event.

- **Display** — theme, and the size of app text and grid text.
- **Tournament files** — where events are saved, and whether you are
  prompted for a folder each time.
- **Pairing** — pairing-engine behaviour, and re-showing the
  pairing-engine notice.
- **Pairing Engines** — the USCF and FIDE engine binaries. These are
  bundled with the installer and normally need no attention.
- **Online player database** — the service used to verify IDs and
  ratings.
- **Online publishing** and **NA Chess Hub URL** — publishing defaults
  and the service address.
- **Printing** — ASCII-only output.
- **Auto-update** — update checks and the release channel.

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

This guide describes FreePair **v0.69.20260813**. It is updated whenever a
change affects what you see or do.

The copy that ships with the app is the one that matches your installed
version. An online copy is published with each release; if you are
reading that one, check the version above matches the FreePair you are
running.

