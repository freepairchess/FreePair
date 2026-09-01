# FreePair user guide

**Applies to FreePair v0.95.20260901**

FreePair is a chess tournament pairing program for tournament directors.
It opens and saves `.sjson` event files, pairs Swiss and round-robin
sections, tracks results and standings, prints the paperwork, and
produces the USCF rating report at the end.

This guide describes what you see on screen and the order you are likely
to do things in. If you are looking for *why* the pairing engine made a
particular decision, that is a different document: open the **Why this
pairing?** dialog on any board and follow the rule citation, or read the
USCF rules reference that ships alongside this guide.

**To find something quickly, click the search box above the contents list
and start typing.** The contents narrow to the sections that mention what
you typed, so you can see where the answer lives before reading any of
it, and every occurrence is highlighted. **Enter** steps through the
matches, **Shift+Enter** goes back, and **Esc** clears. If your hands are
already on the keyboard, pressing **/** anywhere on the page jumps
straight to the box. Searching changes nothing about your event; it only
looks through this page.

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

Each event picks its engine from its rating type, and a section can
override that choice on the **Event configuration** form or in the
section's own header. The override is only available until round 1 is
paired; after that the engine is locked for the rest of the event, so the
pairings you file in a rating report can always be reproduced. For a
USCF-rated event either engine's crosstable is accepted for rating
submission — the two differ only on edge cases such as colour repeats,
last-round prize protection and swap selection, and most weekend Swisses
pair identically.

### Installing and updating

The installer bundles everything FreePair needs, including the .NET
runtime and both pairing engines. There is nothing to install
separately.

FreePair can check for new versions on startup. Turn this on or off
under **⚙ Settings → Updates → Check for updates on startup**, or
use **Check now** at any time. If you want early builds, tick **Include
pre-release builds (beta channel)** — otherwise you only see stable
releases.

When an update is found, a banner appears across the top of the window.
**Release notes** opens that release's page in your browser, so you can
read the whole thing and keep it open while you decide. **Update
options** takes you straight to the update preferences. **Update now**
downloads it and restarts FreePair. There is nothing to save first —
FreePair writes your event as you work.

You can also install from **⚙ Settings → Updates**: press **Check now**,
and if there is an update an **Install update and restart** button
appears beside the result. Useful if you dismissed the banner, or if you
came here to deal with updates in the first place.

**✕** at the end of the banner puts the notice away for the rest of the
session. It is not a permanent answer: the banner comes back if a newer
version turns up later, so closing it during a round cannot leave you
unaware of some future release. If you want the checks themselves
stopped, that is **Check for updates on startup**, under Update options.

Your current version is shown in Settings, and in the main window title
bar. **Release notes**, beside **Check now**, shows what changed in the
version you are running — useful when a behaviour surprises you and you
want to know whether it was deliberate. The notes come from the published
releases feed, so the first press needs a connection; after that they are
already to hand.

### Going back to an earlier version

If a new version causes trouble, you do not have to live with it until
the next release. **⚙ Settings → Updates → Change version** lists every
FreePair release published for your platform — older ones included — and
will install any of them.

Press **List versions**, then pick one from the menu. Each entry says
where it stands: *running now*, *newer*, or *older*. Releases you have
used on this computer also show the date you last ran them, which is
usually how you will recognise the one you want — "the version I had
yesterday" is easier to remember than a number. FreePair starts keeping
that record the first time you run it, so the list fills up as you go.

**Every entry has its own Notes button.** Press it to read what changed
in that release without selecting it or installing anything. This is the
usual way to find the version you want: "the one before boards were
renumbered" is a better description of the release you are after than
any date, and you should not have to install a version to find out
whether it is the one.

A version you once ran that has since been withdrawn is still listed,
marked **no longer published**, but cannot be installed. It is shown so
you are told it is gone rather than left looking for it. Its **Notes**
button still works, but says only that the release has been withdrawn —
the notes went with it.

Going **back** asks you to confirm first, and the reason is worth
reading rather than clicking past. An older FreePair may not understand
an event file a newer one has saved. Anything the newer version added to
your event can be ignored, or lost, the next time the older one writes
the file. **Copy your `.sjson` somewhere safe before going back**,
especially part-way through an event. Moving *forward* again has no such
risk and asks nothing extra.

The download is a full one either way — going back does not reuse what
is already installed — so it needs the same connection an update does.
FreePair restarts into the version you chose. There is nothing to save
first.

### The main window

There are no tabs. The window shows the event you have open, and a single
toolbar runs across the top of it.

The event commands — **New Event**, **Open Event**, **Save Event**,
**Close Event**, **Event Operations**, **Undo** and **Redo** — run from
the left. The controls that belong to the program rather than to the
event sit at the right-hand end of the same row:

- **🖥** and **📺** put pairings and standings on a projector, a TV or
  people's phones.
- **🖨** prints the whole event: **All Pairings**, **All Standings**,
  **All Wall Charts** or **All Crosstables**, one PDF with a section to
  a page. These are the same four reports as **Event Operations →
  Print**; they are also up here because printing the pairings is the
  most repeated action of a round, and three levels of menu is a lot for
  something done every forty minutes. To print one section rather than
  the whole event, use the **Print as PDF** button on that section's
  tab.
- Those three are icons only, to keep the row narrow; hover any of them
  and the tooltip names it. All three are also in **Event Operations**
  under their full names — **Live Score Board (full screen)**, **Share
  Live Score Board** and **Print** — if you would rather find
  them by name.
- **⚙ Settings** opens the application preferences in a window of
  their own. See [Settings](#settings).
- **📖 Help** opens a short menu:
  - **About FreePair** — what the program is, **the version you are
    running**, and the address to send a feature request or a bug report
    to. If anyone ever asks you which version of FreePair you have, this
    is the answer — press **📋 Copy** beside it and paste. **📝 Change
    log** opens the list of every published release, newest first, in
    your browser.
  - **User Guide** — this guide. F1 still opens it directly, without
    going through the menu.
- **Theme** applies immediately. It is left out here rather than filed
  away in Settings because it is the one display choice you are
  likely to change on the spot, usually because of the light in the
  playing hall.

Live Score Board, Share and Print appear only once an event is open; Settings,
Help and Theme are always there.

**On a narrow window the toolbar drops the labels and keeps the icons.**
It does this rather than taking a second row, because the row it would
take comes out of the pairing grid — and the machines narrow enough to
trigger it are the same 11-inch laptops that have the least room to
spare. Nothing is removed: hover any icon and the tooltip names it, and
every button keeps its place, so a button you have learned the position
of is still in that position. Widen the window and the labels come back.

If an event is open, the title bar shows the event name and the file it
is saved to.

**The big tables scroll inside the tab, not with the page.** On Roster,
Pairings, Standings and Wall Chart the table stretches to the bottom of
the window and scrolls within itself, so the filter box and the toolbar
above it stay put while you work down a long list. Make the window
bigger — or move it to a larger monitor — and the table grows to use the
extra room rather than stopping at a fixed size.

This is also why those tabs stay quick on a large event. The table only
draws the rows you can actually see, so a section with three hundred
players opens and switches as fast as one with thirty. Overview, Prizes
and Norms are stacks of cards rather than one big table, so those pages
still scroll as a whole.

### Keyboard shortcuts

The commands used most often during a round have keys. The full list is
in **⚙ Settings → Shortcuts**, which is worth knowing about because
the moment you want it is usually the moment you are in a playing hall
with no internet.

| Key | What it does |
|---|---|
| `Ctrl+N` | Create a new event |
| `Ctrl+Shift+N` | Browse events on NA Chess Hub and create one from them |
| `Ctrl+Shift+R` | Create an event from an online registry by ID and passcode |
| `Ctrl+Shift+W` | Create an event from a published one at a web address |
| `Ctrl+Shift+H` | Create an event from one your NA Chess Hub account runs |
| `Ctrl+Shift+V` | Re-open one of your events saved on NA Chess Hub |
| `Ctrl+O` | Open a saved event from this computer |
| `Ctrl+R` | Open a recent event |
| `Ctrl+Shift+O` | Browse your cloud-saved events |
| `Ctrl+S` | Save the event |
| `Ctrl+W` | Close the event |
| `Ctrl+Z` | Undo the last change |
| `Ctrl+Y` | Redo it (`Ctrl+Shift+Z` does the same) |
| `F5` | Pair the next round of the selected section |
| `Shift+F5` | Pair the next round of every ready section |
| `Ctrl+Shift+Delete` | Delete that section's last round — asks first |
| `F6` | Sync all rosters with NA Chess Hub |
| `F7` | Publish pairings and results to NA Chess Hub |
| `F8` | Check the event over |
| `F9` | Pairing quality |
| `Ctrl+B` | Open the full-screen Live Score Board |
| `Ctrl+Shift+B` | Share the Live Score Board |
| `Ctrl+P` | Print what you are looking at |
| `Ctrl+Shift+P` | Print every section's pairings |
| `Ctrl+Shift+D` | Print every section's standings |
| `Ctrl+Shift+L` | Print every section's wall charts |
| `Ctrl+Shift+T` | Print every round-robin and quad section's crosstable |
| `Ctrl+,` | Open Settings |
| `F11` | Focus mode — give the table the whole window |
| `Esc` | Leave focus mode |
| `F1` | Open this guide |

A few of these are worth a sentence:

- **`Ctrl+W` closes the event, not the window.** FreePair runs one
  window, and the event is the thing you open and close all day.
- **Opening has three keys, because it has three routes.** `Ctrl+O`
  browses this computer, `Ctrl+R` offers the events you had open
  recently, and `Ctrl+Shift+O` lists the events you have backed up to
  the cloud. One key could not have chosen between them.
- **Starting an event has five, for the same reason.** `Ctrl+N` for an
  empty one, `Ctrl+Shift+N` to browse a registration list,
  `Ctrl+Shift+R` for an event ID and passcode, `Ctrl+Shift+W` for a
  published event's web address, and `Ctrl+Shift+H` for one your own NA
  Chess Hub account runs. That looks like a lot of keys for one menu, and
  it is — but which route you use is decided by how your club registers
  players and then never changes, so in practice you learn the one you
  use every week and ignore the rest.
- **`Ctrl+Shift+V` is the odd one out and worth knowing.** It re-opens an
  event of yours already saved on NA Chess Hub, pairings and results
  intact. Every other key above *starts* an event; this one returns you
  to work in progress, which is why it sits under **Open Event** rather
  than **New Event**.
- **`Ctrl+Shift+N` is for starting an event from a registration list**,
  not for opening one you already have — it browses NA Chess Hub and
  builds a new event from what it finds, which is why it sits beside
  `Ctrl+N` rather than beside the opening keys.
- **`Ctrl+P` follows the tab you are on.** On Pairings it prints the
  pairings, on Standings the standings, and so on — the same PDF as the
  **Print as PDF** button on that tab. On Overview it does nothing,
  because there is nothing there to print.
- **The `Shift` versions widen a key to the whole event.** `F5` pairs
  the section you have selected; `Shift+F5` pairs every section that is
  ready. `Ctrl+P` prints the tab in front of you; `Ctrl+Shift+P` prints
  every section's pairings. That is a real difference in scope, so check
  which one you are reaching for during a round.
- **Only the operations repeated during a round have keys.** Event
  Operations holds a good deal more — merging sections, renumbering
  boards, rating reports. Those stay one click away in the menu, because
  a shortcut list long enough to need scrolling is a list nobody learns.
  Every key that does exist is printed beside its item in the menu.
- **A menu only prints a key that does exactly what the item does.** A
  section's **Pairing Operations** menu shows `F5` beside *Pair Next
  Round* and `Ctrl+Shift+Delete` beside *Delete last round*, and nothing
  beside Check Section, Pairing Quality or Sync Roster — because `F8`,
  `F9` and `F6` act on the **whole event**, not on the section in front
  of you. Near enough to look like the same command, far enough to
  surprise you mid-round, so the menu says nothing rather than something
  almost true.
- **`Ctrl+Shift+Delete` is deliberately awkward**, and it still asks
  before deleting. It is the same confirmation the button shows; a
  keystroke is never a shorter route to throwing pairings away than the
  click it stands in for.
- **A shortcut that needs something does nothing until it has it.**
  `F5` with no section selected, or `Ctrl+S` with no event open, is
  silent rather than broken. The Shortcuts tab says what each one needs.

Keys cannot be reassigned in this release.

### Focus mode

A round of twenty boards does not fit on a laptop screen underneath a
toolbar, a divider, a tab strip and a round selector — so entering
results means scrolling between the scoresheet in your hand and the row
you are filling in.

**Focus mode shows the table and nothing else** — no toolbar, no
section list, no tabs, and none of the setup controls. Press **F11**, or
the **Focus (F11)** button on the header of the table you are looking
at. The same button then reads **Exit Focus Mode (F11)**. **Esc** leaves
too, as does the button in the strip along the top.

Every tab except Overview offers it: Roster, Pairings, Standings, Wall
Chart and Prizes. Where a tab shows **two** tables it has two Focus
buttons, one on each — the Roster tab always does, because of the **Byes
& Withdrawals** panel, and in a team event so do Pairings, Standings and
Wall Chart, which show the team table above the individual one. Each
button fills the window with its own table and hides the other, and
focusing the byes panel opens it if it was folded away.

**The focused table takes the whole window**, not just the space it
happened to occupy. That is the point of the mode: a bye list that stops
a third of the way down the screen has not given you any more room to
work in.

The strip along the top names what you are looking at, and on the
Pairings tab it names the **round** as well — "Focus mode — Open —
Round 3 Pairings". Focus mode takes the round selector away with
everything else, so that line is the only thing left telling you which
round you are typing results into.

**On the Pairings tab it also puts away the bye list and the colour-due
legend.** Both sit under the board grid and take as much room as they
need, so a late round with a lot of players paired out can leave you
four boards to look at above a screenful of byes — the opposite of what
you pressed F11 for. The panel header still says how many were paired
out, so you can see that byes exist; the roll call itself is a keystroke
away on leaving focus mode, and the Byes tab has it in full.

**The round's byes are a table of their own**, further down the Pairings
tab, with the same sorting, resizing and font control as any other. They
have their own **Focus (F11)** button too, so you can give the byes the
whole window when you are checking who is sitting out — the banner then
reads "Round 3 Byes" rather than "Round 3 Pairings", so there is no
doubt which table you are looking at. Requested byes for a future round
are tinted green, the same as on the wall chart.

It keeps the tab you are on;
from Overview — a form rather than a table — it takes you to Pairings.

**Pairing a round brings its pairings to the front.** If you press `F5`
from the Roster, or use the Pair button beside a section, the section
switches to Pairings once the round is made — the round you just asked
for is the thing to look at, and staying put with nothing visibly
different is hard to tell from nothing having happened. If you were in
focus mode you stay in it.

That strip along the top is the one piece of chrome it keeps. It names
the section you are in and says how to leave, because an application
that has silently shed its toolbar looks broken rather than focused.

Nothing else changes: the same grid, the same editing, the same
shortcuts. It is not remembered between sessions — it is a posture for
a few minutes at the scoring table, not a preference, and finding the
toolbar gone the next morning would be alarming rather than helpful.

---

## Creating and opening events

### Creating a new event

Use **➕ New Event**. You will be asked for the event name, dates
and location, and then for at least one section. An event with no
sections cannot be paired, so it is normal to create the first section
immediately.

### Opening an existing event

**📂 Open Event** offers the events you have worked on recently.
You can also browse the file system for an `.sjson` event file, or
browse events published to NA Chess Hub.

FreePair opens event files written by other programs and writes the same
format back, preserving anything it does not itself manage. See
[Compatibility with SwissSys](#compatibility-with-swisssys) for what does
and does not travel in each direction.

**An event reopens where you left it.** If you were on the U1200
section's Pairings tab when you closed, that is what you get back — no
navigating to the same place after every lunch break. Because it is
stored in the event file rather than on the computer, it also follows the
event to a second machine at the other end of the table.

It is written only when you actually moved. Open an event, read
something, close it again, and the file is left exactly as it was — its
contents and its timestamp both. Browsing is not editing.

If the section you were on has since been renamed, merged or deleted,
the event simply opens normally.

**FreePair reopens your last event on startup — but only if it was still
open when you quit.** If you close the event first and then close
FreePair, it starts up empty next time. Closing an event is how you say
you are finished with it, and an event that came back on its own would
make the close look as though it had not taken. The event stays in
**Open Recent…** either way, so it is still one click from returning.

### Quick Event from a roster file

**➕ New Event → Quick Event from a Roster File …** is the fast route
when you already have the entry list as a spreadsheet. It reads the file,
makes one section, puts everybody in it, and hands you an event that is
ready to pair. A club night can go from a CSV to round one in well under
a minute.

**It is also the way in when nothing else works.** Reading an event
straight off a web address only works for sites FreePair knows how to
read. If your registration site is not one of them — or it is, and the
site has changed — this is how you get the roster in anyway, without
typing it. See [When FreePair cannot read your
site](#when-freepair-cannot-read-your-site) below.

**This one needs you signed in to NA Chess Hub.** The menu item stays
greyed out until you are; its tooltip says so. See [Signing in to NA
Chess Hub](#signing-in-to-na-chess-hub).

**Two ways in**, on two tabs:

- **From a file** takes a `.csv`, a `.tsv`, or a `.txt` that is really a
  table. The separator is worked out from what is inside the file, not
  from its name, so a `.txt` full of commas is read correctly.
- **Paste the roster** gives you a box. Paste into it — or press **Paste
  from clipboard** — then press **Check this roster**. If an AI tool
  wrote the table its answer is already on the clipboard, and saving it
  to a file first is a detour through a text editor for no reason.

**The box can be edited before you check it**, and that is most of the
point of it: fix a name, delete a player who withdrew that morning, or
type the whole list yourself. Nothing is read until you press **Check
this roster**, so FreePair will not complain about a line you are still
halfway through typing. Edit after checking and the preview clears — it
belonged to the old text, and leaving it there would invite you to press
Create on something you are no longer looking at.

The event and section settings below the tabs are the same either way.

Either way, only these columns are read, and the header names are
matched loosely, so `Player`, `Name` and `Full Name` all work:

| Column | Needed? | Notes |
|---|---|---|
| **Player** | Yes | The only column that must be there. A row with a blank name is skipped. |
| **ID** | No | The US Chess ID. |
| **Rating** | No | Anyone without one comes in unrated, which pairs correctly. |
| **Byes** | No | Rounds the player has asked off. |

Anything else in the file is ignored, and FreePair says which columns it
ignored — so a registration export with thirty columns can be used as it
is, without editing.

**Copy generously; it sorts the rest out.** You do not have to select
exactly the table. FreePair finds the row that names the columns, throws
away anything above it, and drops the conversation underneath —
"Here is the roster you asked for", the ``` fences an AI tool wraps its
answer in, "Let me know if you would like me to adjust anything". It
tells you what it discarded, so you can check nothing of yours went with
it.

**There does not have to be a header row at all.** If the text is just
player rows, FreePair works out what the columns are from the values in
them, and says what it decided so you can check it:

```
"Tom Smith",7654321,1789,
"Jerry Jackson",6543217,2209,1
```

It can do this because the three numbers on a chess roster live in
ranges that do not overlap — a bye is a round number, a rating runs to
about 3000, and a US Chess ID is eight digits. **The order does not
matter**, so a site that puts the rating first is read just as well. A
column it cannot place confidently is left out rather than guessed at,
because an unread column costs you one edit and a wrongly-read one costs
you a pairing.

Two shorter forms work too:

- **Just names**, one per line. Everyone comes in unrated, which pairs
  correctly, and you can add ratings on the Players tab.
- **Name then rating**, as in `Tom Smith 1789` — the way most people
  type a list. The rating is only lifted off the end when *every* line
  has one; a half-filled column is more likely to be names that happen
  to contain a number.

The one thing to watch is a line that is a name and nothing else, with
no commas after it. FreePair keeps those, on the grounds that losing a
player is far worse than gaining a stray row — a missing player is not
obviously missing until they ask why they have no game, whereas a row
called "Thanks!" is plain to see in the preview.

**What it should look like.** The dialog shows this example under *"What
the file should look like"*, with a button to copy it — building a
spreadsheet from a worked example is quicker than reading a table of
rules:

```
Player,ID,Rating,Byes
"Tom Smith","7654321",1789,""
"Jerry Jackson","6543217",2209,"1"
"Jason Lee","4376521",1678,"2,3"
```

Tom has asked for no byes, Jerry wants round 1 off, and Jason wants
rounds 2 and 3. Quotes are only needed around a cell that contains a
comma, but they never hurt.

**The Byes column** takes round numbers and is not fussy about how they
are written: `1,3`, `1 3`, `1;3`, `R1 R3` and `round 1` all mean the same
thing. They become **half-point byes**, because a bye asked for in
advance on an entry form is a request, and that is what a request earns.
If a cell cannot be read at all, FreePair says so rather than quietly
dropping it — a bye that goes missing is a player who turns up to a game
they asked not to have.

**What it asks you.** Three things, all with defaults:

- **Event name** — filled in from the file's own name, so
  `Tuesday Swiss.csv` becomes *Tuesday Swiss*. Type over it and your
  version sticks, even if you then pick a different file.
- **Section** — name (**Open**), pairing (**Swiss**), rounds (**4**) and
  an optional time control. Round-robin, double round-robin and quad are
  also offered.
- Nothing else. Dates, location, prizes, ratings and the rest are all
  editable afterwards in the usual places.

**Check the names before you press Create.** The dialog lists the first
few players with their ratings and byes. This is worth two seconds: a
file with the wrong separator reads as one player with a very strange
name, and nothing else in the process would catch it.

**It does not ask where to save.** The event goes into your tournaments
folder under its own name — choosing a folder is the slowest part of
making an event and the part you care least about at five to seven. Use
**Save As** afterwards on the rare occasion it matters.

**Warnings you might see afterwards.** A bye for a round the section
does not have is dropped and named — usually it means the round count is
wrong, which is much better caught now than in round three. A name
appearing twice is kept, both times, and reported: two players really can
share a name, and only you know whether this is that or a double entry.

### When FreePair cannot read your site

If **Create New Web Event** fails — the site is not one FreePair knows,
or it is a PDF, or the roster only exists on paper — the dialog now
offers a way round it instead of stopping there. You do **not** have to
type the entry list in by hand.

1. **Screenshot or photograph the roster.** A phone picture of a printed
   sheet is fine. So is a screenshot of a web page that FreePair could
   not read.
2. **Press "Copy the instructions"** in the failure message. That puts a
   short, plain-English brief on your clipboard.
3. **Paste it into an AI tool along with the picture** — ChatGPT,
   Copilot, Claude and Gemini all do this, and all have a free tier that
   is more than enough. The panel has a link to each, so you can open one
   without going to find the address. It will reply with a table.
4. **Press "Open Quick Event"** in the same message. That closes the Web
   Event window and takes you straight there — you do not have to go
   back to the menu.
5. **Bring the roster in**, whichever way is easier:
   - The instructions ask the tool to produce a **downloadable
     `roster.csv`**. If it does, use the **From a file** tab.
   - Otherwise **copy its reply**, open the **Paste the roster** tab,
     paste, and press **Check this roster**. You do not need to select
     carefully — the fences, the "here you go" and the sign-off are all
     discarded.

The failure message in the Web Event window spells these four steps out,
so you do not have to remember them.

The same **Copy the instructions** button is at the top of the Quick
Event dialog, under *"No spreadsheet? Turn a photo of your roster into
one"* — so you can start there if you already know the site will not
work. The full text is shown on screen next to the button, and the box
grows with the window if you maximise it. It is worth reading once
before you send it anywhere, since you are about to hand a list of
players' names and ratings to somebody else's service.

**The instructions tell the tool not to guess.** Anything it cannot
actually read in the picture is left blank, because a plausible invented
rating is much worse than a missing one — it changes who plays whom, and
nothing downstream would catch it. That is also why the last step is
yours: **check the names and ratings against the original before you
pair.** The tool is doing the typing, not taking responsibility for the
event.

### Events from an online registry

If you are **signed in to NA Chess Hub** (see
[Signing in to NA Chess Hub](#signing-in-to-na-chess-hub)), the quickest
route is **➕ New Event → Create New Event Using Online Registry → My
Events on NA Chess Hub…**. That lists the events your account runs and
opens the one you pick straight away — **no passcode at all**.

Otherwise, creating an event from an online registry and opening a
cloud-saved one both ask for an **Event ID** and a **Passcode**. Once you
have entered the event ID, the **Passcode** caption becomes a link to that
event's own page, where the passcode is shown — click it if you do not
have the passcode to hand, and copy it from there rather than hunting
through email. Both fields also have a 📋 button to paste from the
clipboard.

Passcodes keep working whether or not you sign in, and you will still need
one for an event somebody else runs and has shared with you.

### Web events

**➕ New Event → Create New Web Event …** builds an event from one that is
already published online. Paste the event's address and FreePair reads the
entry list and any rounds that have been played. There is a 📋 button
beside the address box, as there is on the Event ID and Passcode fields:
the address always comes from somewhere else — a browser, an email, a
message — so pasting it is the only way it ever gets there.

**This one needs you signed in to NA Chess Hub.** The **Create** button
stays greyed out until you are, and the dialog offers the sign-in itself
so you do not have to go to Settings and come back. If you have no
account there is a **Create a free account** button beside it — an
account at nachesshub.com costs nothing and takes about a minute. See
[Signing in to NA Chess Hub](#signing-in-to-na-chess-hub) for what
signing in does and does not involve.

**If the address will not read**, the failure message offers a way round
it rather than leaving you stuck — see [When FreePair cannot read your
site](#when-freepair-cannot-read-your-site).

What you get is an ordinary event file — players with their IDs and
ratings, and a round history — so it opens, pairs and reports like any
other. **The event's own details come across too** where the source
publishes them: the dates, the city, state, ZIP and country, the
organizing affiliate, and the chief TD and assistant. They land in
**Event Configuration**, already filled in.

Where an event lists a different chief for each section, it is the
event's own pair that is taken — the ones its page prints at the top —
not whoever happens to run the first section. It is most useful for having a realistic finished event to look at,
which is exactly what the Norms tab wants.

**Every section is imported**, each under its own name, so a multi-section
event arrives whole and you do not have to run it once per group. If two
sections happen to share a name, the second is given a number, because
elsewhere in FreePair a section's name is how it is identified.

A large event is a lot to read, so the dialog stays open and tells you
which section it is on. **Cancel** stops a read in progress.

Three other things to expect. Ratings come in on the scale the source
publishes — a US Chess crosstable brings US Chess ratings, and FreePair
records them as such rather than filing them as FIDE ratings. Colours are
recorded only where the source records them; where a crosstable gives the
result and the opponent but not who had White, FreePair leaves the colour
blank rather than inventing one that would look identical to a real one.
And an address FreePair cannot read is refused before anything is fetched.

**Some addresses are entry lists rather than results.** A registration page
knows who is playing but not what they played, so what you get is a roster
with no rounds — you pair round one yourself. That is still most of the
setting-up done for you, and an entry list often carries more than names:
requested byes, membership expiry dates, and which section and schedule each
player entered.

**Where an event runs the same section on more than one schedule** — a
2-day and a 3-day entry into the same Under 1800 — those arrive as separate
sections, named for both. They are separate pairing groups until the
schedules merge, and putting them together would pair players against
opponents they are not yet due to meet.

Some entry lists sit behind an interface that only answers requests
coming from the event's own page. FreePair supplies that page — the one
you pasted — and still identifies itself as FreePair, so the site can see
what is calling. Use this only for sites you organise on or have
permission to read.

**An entry list is counted before it is accepted.** These pages state how
many entrants they hold, and FreePair compares that against what it actually
read. If the two disagree the import is refused rather than completed. A
roster that is quietly short is the worst outcome available: nothing looks
wrong, the event opens, and the missing players are discovered when they
turn up to play.

Where such a roster carries both a scholastic
(NWSRS) and a US Chess rating, FreePair works out per section which one the
section is actually run on, from the IDs the entrants have rather than from
which columns exist, and seeds on that. A section where nobody has a US
Chess ID is not a US Chess section, and seeding it on an empty column would
put the whole field at zero.

A rating of 0 on a roster means **unrated**, and FreePair keeps it that way
rather than treating it as a rating of zero — a player seeded at zero would
be paired bottom board against the field for want of a number nobody has
issued yet.

Titles and withdrawals are sometimes printed into the player's name. Both
are lifted out, so the name is a name, the title is a title, and a player
listed as withdrawn arrives withdrawn rather than in the pairing pool.

**On a scholastic entry list, the school becomes the team.** Where players
register through a school, that school is what team standings, team prizes
and the team wall chart are computed from, so it lands in the **Team**
column rather than being filed as a club. A scholastic event therefore
arrives ready to score by school without your retyping a row of it.

Some of those sites publish **two ratings**: the official one and a live
one that has moved since the last supplement. FreePair seeds and pairs on
the **official** rating, because that is the one the entry list everybody
has read is sorted by, and shows the live figure in the second rating
column so you can see both. A player who has games but has not yet
appeared in a supplement is therefore **unrated** here rather than rated
zero — which is the correct reading, and a rating you can fill in
yourself if the organizer means to pair on the live one.

Entrants who have withdrawn, cancelled or expired arrive **withdrawn**.
So does anyone still on a **waiting list**: they have no place yet, so
pairing them would be wrong, but leaving them out altogether would hide
the people you promote when somebody drops. Withdrawn, they are visible
and one click from playing.

**NA Chess Hub events can be read without the passcode.** Publishing to NA
Chess Hub needs the event's passcode, which belongs to the organizer.
Reading does not: paste the address of the event's page —
`.../Events/Details/…` — or of its **Roster** or **Pairing** page, whichever
you happen to have open, and FreePair reads the two public pages. So a
player, a parent, or a director helping out at somebody else's event can
build the file without asking anyone for anything.

Two pages are read, once each. The event page supplies the dates, the
venue, the organizing affiliate, and each section's own round count, time
control and rating system — so a six-round event arrives as a six-round
event rather than defaulting to one. The roster page supplies the entrants,
already grouped into the sections the organizer created. **A section nobody
has entered yet is still imported**, empty, because it was created on
purpose and finding it missing later is a nastier surprise than finding it
bare.

Three things on that roster matter more than they look:

- **Withdrawals.** A player whose Status reads *Withdrawn* arrives
  withdrawn. The other statuses are about money and say nothing about who
  is playing.
- **Requested byes.** The Byes column holds the rounds each player asked to
  sit out, and they come across as bye requests. That is the part you would
  otherwise be re-keying from a printout on the morning of round one.
  Where the column instead shows the word *Request*, nobody has asked for
  any.
- **What is not in a column.** The US Chess ID is in the link behind each
  name and the membership expiry is in its tooltip; a scholastic player's
  NWSRS ID, school code and grade are in the rating cell's tooltip. All of
  it is read, so the roster arrives ready to rate rather than needing every
  player looked up again.

Entries whose fee has not been paid are listed separately at the foot of
the roster, under a notice from the organizer saying they will not be
paired. FreePair leaves them out.

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
- **🪟 Open in Window** pops a section out into its own window, so you can
  watch two sections side by side.

Each row in the sections list collapses with the chevron on its right, so
a long list stays scannable. A collapsed row still carries the two
numbers you are usually scanning for, in brackets after the name —
`Open [123, 3/9]` is 123 players, 3 rounds played of 9.

Under the **Sections** heading is the event's own total — *4 sections ·
120 players* — so the size of the event is one glance rather than a
column of numbers added up by hand. Deleted sections are not in either
figure; they are struck through in the list because they are no longer
part of the event, and counting their players would say otherwise.

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

### Working on several players at once

**Ctrl-click or shift-click** rows in the roster to select more than one,
then use **Roster Update** — the menu shows how many you have selected,
so it is never a guess about what it will touch.

- **Edit selected players…** — changes the fields a group can genuinely
  share.
- **Delete selected players** — soft-deletes them all. They stay in the
  file and can be brought back until the section is paired.
- **Restore selected players** — brings back the ones that were deleted.
  Deleted players stay listed in the roster, so you can select them the
  same way.

**Only the fields you tick are changed.** The edit window has a tick-box
beside each field, and anything you do not tick is left exactly as it is.
That is what makes it safe to set the club for eleven players without
disturbing the three of them who are on a team.

**A ticked field with an empty box clears it.** That is deliberate — it
is how you empty a placeholder that came in with an import.

**Byes replace rather than add.** Everyone selected ends up with exactly
the rounds you list, which is what "these six are all away on Saturday"
means. If you want to add a round to byes people already have, edit them
individually.

**Names, ratings and IDs are not offered.** They identify a player.
Applying one value across a selection would not be an edit, it would be
losing eleven players' details at once.

**Nothing is half-done.** If any part of the change cannot be applied,
none of it is, and FreePair says so. You never end up with the first four
players changed and the rest not.

### Verifying IDs and ratings

**ID and Rating** verifies USCF and FIDE IDs against the online player
database and fills in the current ratings.

**On a FIDE-rated section it also fills in the FIDE half of the roster.**
Plenty of events arrive with national IDs and national ratings and nothing
else — an event imported from a national source usually does. For every
player still missing FIDE details, FreePair looks up their **FIDE ID,
rating, federation and title** and fills in whatever was blank. Without
them the Norms tab sees a field of unrated, untitled, federation-less
players and reports that nobody is near a norm, which looks like an answer
rather than missing data.

Three things it deliberately does not do. It never overwrites a value you
typed — if you corrected a federation by hand it stays corrected. It never
puts the FIDE rating in the pairing rating column, because a US Chess event
pairs on US Chess numbers and reseeding on a different scale would reorder
the whole field. And a player with no ID at all is skipped rather than
matched by name, since two players share a name often enough that guessing
would eventually attach the wrong title to somebody in a norm report.

The FIDE ID is written into the **ID2** column and the FIDE rating into
**Rating2**, which is where a dual-rated event keeps them and how they
travel back out to a SwissSys file. Both columns are then
labelled with the federation they hold — **ID2 [FIDE]** and **Rating2
[FIDE]** — so the FIDE export and the rating refresh will use them, and so
two ratings side by side cannot be mistaken for the same scale. Turn ID2 and
Rating2 on under **Optional Columns** if you do not already show them.

An ID2 already holding another federation's ID is never overwritten — the
FIDE ID is still recorded, just not in that column. And on a FIDE-only
event, where the first column already is the FIDE ID, nothing is copied to
ID2: it would show the same number twice.

A large section is one lookup per player, so it runs behind a progress
window you can cancel — one for the ID check, then one for the FIDE
lookup, each saying which it is. Cancelling keeps whatever had already
been found.

**Every name in the roster is a link.** Clicking one opens the player
database in your browser, searched for that name. It is a search rather
than a jump to one profile, because a name can match nobody, one person
or several — the identification is yours to make, not FreePair's.

**Only national IDs are checked.** US Chess, FIDE and CFC IDs are digits
only, so an ID carrying letters is not one of them and is never looked
up as one — it is left unchecked and labelled as such rather than
guessed at. An ID with the shape of an NWSRS one is recognised and the
column is labelled **NWSRS**; use **Roster Update** to check those
against the NWSRS database instead. If a section was previously
mislabelled this way, running the check again corrects it.

**Roster Update** re-pulls live ratings for the whole section. It first
checks that every ID column holding data has a known federation, because
otherwise it cannot tell whether the number it is about to overwrite is a
USCF rating or a FIDE one. That classification is saved in the event file,
so once a section has been through **ID and Rating** it stays classified —
closing and reopening the event does not send you back to square one.

Two warnings exist here on purpose:

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

**FIDE Country** appears once two things are true: the section is FIDE
rated, and at least one player has a federation recorded. **Verify IDs**
fills the federations in.

It follows the **section's** rating system rather than the event's, which
matters more often than it sounds — a norm round-robin pinned to
USCF/FIDE inside an event still set to US Chess is an ordinary way to run
one, and the column belongs on it. If the column is missing on a section
you expect it on, check the section's rating system on the **Overview**
tab before suspecting the lookup: everything else FIDE about the tab —
the ID2 and Rating2 headings, the Norms tab — reads the same setting.

### Copying a player's details

The roster's **Actions** column — the icons at the far left, next to the
✏ pencil — has a 📋 button. It opens a short list of everything FreePair
holds for that player: name, ID, rating, title, club, state, team, email,
phone and score. Press **Copy** beside the one you want and the window
closes with that value on the clipboard.

The pair number and the status are deliberately not on the list. Neither
travels: a pair number belongs to this section's seeding and means
nothing outside it, and *Active* is FreePair's word for the absence of
news.

The copy buttons used to sit inside the ID, Name, Rating, Team, Email and
Phone cells themselves. Six icons repeated down every row cost more width
than the values beside them, and you read past all of them on every line
to reach the data. One button per row does the same job and gives the
space back to the roster.

**Phone numbers are shown grouped** — *(206) 555-1234* rather than the run
of digits a registration form collected — so a column of them can be read
and compared. Only numbers FreePair can recognise with certainty are
regrouped; an international number is shown exactly as it was entered,
because guessing at the grouping of a number whose country is unknown
produces something that looks authoritative and is wrong. Nothing is
rewritten in the event file either way.

### The counting column

The roster's leftmost column, headed **#**, is a plain running count of
the rows in front of you: 1 at the top, whatever the last number is at
the bottom. It is **not** the pair number, which lives in its own column
a little further right.

The difference matters as soon as you sort. A pair number is a player's
identity and stays with them, so once you have sorted by state, rating or
name, the pair numbers are scattered and nothing tells you how far down
the list you are. The **#** column renumbers itself with the sort, so
sorting by state and reading the first and last **#** of a run tells you
how many players are from that state — no counting rows with a finger.

Because it only ever describes the current order, it cannot be sorted by
and it is not printed on any report.

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

**A file that names a rating system per section shows it.** Some event
files record a rating system on each section as well as on the
event.
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
every section at once, use **Pair all sections** on the event page —
see [Pairing every section at once](#pairing-every-section-at-once).
**Sync Roster with NACH** is on this menu as well as the Roster
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

### Pairing every section at once

**Pair all sections** on the event page pairs the next round of every
section that is ready, without you visiting each one. A section is ready
when at least two players are active, the previous round's results are
all in, and more rounds remain; the others are listed for context and
skipped.

The bottom half is a table of the whole event, one row per section:
players, rounds paired, **pairing rule**, starting board and the board
range that implies, pairing engine, **Avoid same team**, **Accelerate**,
and why a section is or is not ready.

- **Pair?** ticks the sections the run will pair. Every ready section
  starts ticked, so the default is still "pair everything"; untick the
  one that is waiting for a player who has not arrived, or that you are
  holding back a round. The **Pair?** heading itself is a button that
  ticks or clears them all. Sections that are not ready have no
  checkbox — there is nothing to pair.
- **Pairing rule** is how the section is paired — Swiss, Quad, Round
  Robin and so on — and you can change it here, for any section that
  has not paired round 1. It is worth reading first, because it decides
  what the rest of the row can do: a Swiss section runs the pairing
  engine and can be accelerated, while a quad or round robin computes a
  fixed schedule and cannot. After round 1 it is shown as plain text,
  because every operation that acts on the rule refuses a section that
  has already paired. **A rule you change here is saved to the
  section**, even if you then close the dialog without pairing anything
  — so setting Open to Quad and pairing it later from its own tab still
  pairs it as quads.
- **Start board** opens on the number each section *should* have. A
  section you have placed yourself keeps your number; one you have never
  placed gets its recommended board, so the sections stack end to end
  instead of all beginning at board 1 — the same rule the **Starting
  Boards** tab of Event settings describes. Any two sections whose
  ranges overlap are called out in orange. Overlapping ranges are not an
  error: two sections playing in different rooms may both start at board
  1. **Use recommended board numbers**, under the table, re-cascades
  them all.
- **Avoid same team** and **Accelerate** are per-section settings, shown
  together because they behave the same way — both are fixed once the
  section has paired round 1, and a section past that point shows what it
  is set to rather than a checkbox. Ticking either one reveals how many
  opening rounds it covers, so a tick never sits above a window you
  cannot see. Teammate avoidance defaults to the whole event;
  acceleration defaults to one round and is capped at half the section's
  scheduled rounds. Acceleration is per section rather than per event on
  purpose: a top section is normally accelerated precisely because the
  ones under it are not. Quads and round robins show a dash under
  Accelerate — their whole schedule is fixed in advance, so acceleration
  would do nothing.

> **In a quad, avoiding teammates means something different**, and the
> row says so: it reads **in different quads**, with no round count. A
> quad is a four-player round robin, so two teammates in the same quad
> are *certain* to play each other and no later pairing decision can
> prevent it — "avoid for rounds 1–3" would be the whole event restated.
> Instead, FreePair separates them when it cuts the field up, moving the
> fewest players it can so the seeding stays as close to the ratings as
> the separation allows. Players left over in the mini-Swiss are not
> moved: there, avoidance is an ordinary pairing constraint.
>
> **Two cases cannot be fully separated, and FreePair tells you which.**
> A section of exactly four players *is* one quad — everybody plays
> everybody whatever their team, so there is nowhere to move anyone. And
> a team with more players than there are groups cannot be spread over
> them; there, the strongest players are separated first and whatever
> remains is left at the bottom of the field, where it affects the
> fewest results. In both cases the split still goes ahead, and the
> summary afterwards says what could not be done rather than letting it
> pass as a success.
>
> **The mini-Swiss counts as somewhere to move a player.** A section of
> eleven, say, becomes one quad and a seven-player group — so two
> teammates seeded 3rd and 4th are separated by moving the lower-rated
> one down into that group, where avoiding them becomes an ordinary
> pairing constraint.

**Every split reports what it made.** Pairing quads from the section's
own button ends with a summary — how many quads, whether there is a
mini-Swiss, and what happened to the teammate separation: nothing to
do, done by moving *N* players out of rating order, or not fully
possible and why. In a batch run the same summary appears on the
section's row in the dashboard.

**The mini-Swiss is paired too — its round 1.** A split leaves the quads
fully paired (all three rounds each) and the leftover group ready but
empty, which is easy to walk away from. FreePair pairs its first round
as part of the same operation, so nothing is left half-done. Splitting
a section from its own button and from the batch run the identical
code, so they cannot drift apart.
- **Pairing engine** can be set here for any section that has not paired
  round 1 yet. After that it is locked and shown with a padlock.

Above the table are the questions FreePair would otherwise ask you once
per section:

- **Review each round in the pairing preview before it is committed.**
  **Off** by default. Unticked, nothing stops between sections: each
  round is committed exactly as the engine produced it, which is what a
  button called *Pair all sections* implies. Nothing is lost — you can
  unpair a round afterwards. Tick it if you would rather see each
  proposed round, and swap colours or boards, before it reaches the hall.
- **Use the same round 1 settings for every section that hasn't paired
  yet.** **On** by default. Answers the round 1 colour question once for
  the whole event instead of once per section. **Coin toss** — the
  default — tosses separately for each section, which is what the
  per-section prompt does; **Top seed plays White** or **Black** fixes
  board 1 the same way everywhere. Sections that have already paired
  round 1 are untouched. Untick it and each first-round section asks
  individually, exactly as it would on its own.
- **Re-seed each roster before pairing round 1.** **On** by default, and
  recommended for the same reason the per-section prompt recommends it:
  the pairing rules assume the engine's own seeding order, so this is the
  numbering the engine is about to be given. Each section is re-seeded by
  **its own** engine's rule — a FIDE section ranks title before name, a
  US Chess section goes straight to name — so an event mixing engines or
  rating scales still gets the right order in each section. Untick it
  only when the pair numbers were assigned elsewhere and must be
  reproduced exactly.

**Pair ready sections asks you to confirm first.** The prompt lists
every ticked section and what will happen to it — *pairs round 1*,
*pairs round 3* — and says whether the rounds will be reviewed in the
preview or committed straight through. **Cancel** goes back to the
table with nothing changed; **Pair now** starts the run.

**A section set to Quad is called out in that prompt**, because it is
not paired as it stands: it is split into new sections. Each quad is
four players and a three-round round robin, all paired at once; any
players left over become a mini-Swiss whose round 1 is paired too. The
original section is kept but soft-deleted, so you can undo the split by
undeleting it and removing the generated sections. When it finishes,
that row reports what it made — *Split into 2 quads and 1 mini-Swiss —
all paired*.

> Pairing a quad section on its own asks you a second time, on the
> section itself, whether you meant a Swiss round instead. The batch
> does not: you answered that question when you set the rule in this
> table and confirmed the summary, and asking again per section is the
> interruption the batch exists to remove.

**The dialog stays open while the run works**, and it is where the
progress is: a bar, *Section 3 of 5 — Open*, and a line saying what is
happening right now — checking player IDs, calculating pairings, saving
the round. Each row updates as its turn comes and finishes with the
round it paired, or with why it did not. Pairing a large section takes
several seconds, and this is what those seconds are.

**Stop after this section** halts the run cleanly: the section being
paired finishes, and the rest are left alone. Rounds already committed
stay committed. The dialog cannot be closed while the run is going —
stopping it is the way out — and once it has finished, **Close** returns
you to the event with a summary of what was paired.

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

### Pairing as a round-robin

**Pair as round-robin** converts a section in place — its players and
ratings are kept — and pairs every round at once, because in an
all-play-all every game is known before a move is made.

The dialog asks two things. The **pairing table** is Berger (the FIDE
Handbook tables, which USCF also uses, and the standard for title-norm
events) or FreePair's own balanced schedule, which pairs the same
opponents but evens out the colours.

The **seed numbers** decide the whole schedule, so this is the part worth
reading. You can keep the current order, re-seed by rating, draw at
random (**🎲 Reshuffle** draws again), or **Assign manually** — click a
cell in the **Seed** column and type the number you want that player to
have. Every number from 1 to N must end up used exactly once; FreePair
checks that when you press **Pair round-robin** and tells you which ones
are duplicated or missing rather than pairing something half-seeded.

The table under the choices previews the result in every mode, and the
dialog can be resized if the field is large.

**An odd field is warned about**, because a round-robin then runs one
extra round and each player sits out exactly once on a full-point bye.

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
set. Open it from **Pairing Operations → Display & Print Columns…** to
configure the on-screen grid and the printed sheet side by side, or from
**Page setup → Columns…** when the report is the pairing sheet, to
adjust the printed sheet alone.

**This one applies to every section**, unlike its neighbour on the same
menu — see below.

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

### Advanced pairing options

**Pairing Operations → Advanced Pairing Options…** holds the two settings
that change how a section is *paired*, rather than how it is displayed:
**Accelerate pairings** and **Avoid pairing teammates**, each with the
number of opening rounds it covers.

These are per-section, and they used to share a window with the column
layout above, which made it easy to miss that one applies to the section
in front of you and the other applies to every section. They are now two
menu entries.

**The on/off switches lock once round 1 is paired.** Both change what
round 1 does, so switching either one on afterwards would describe a
tournament that had not been run. The round windows stay adjustable
until the rounds they cover have been paired, so you can shorten or
extend acceleration mid-event.

**Turning either one on fills in its round window for you.** A ticked box
above a round count of 0 would be a setting that is switched on and does
nothing at all, so **Accelerate pairings** starts at 1 round and **Avoid
pairing teammates** starts at the whole event. Both are only defaults —
change the number to whatever the event needs. If a round count is already
set, it is left alone.

**The same two settings appear in three places, and they are the same
settings.** Changing one changes all of them:

- **Pairing Operations → Advanced Pairing Options…** — the window
  described above.
- **The section's Overview tab**, under *Advanced Pairing*. This is
  where to look when you want to know how a section is set up without
  starting to pair it.
- **The Edit Section dialog** (the pencil beside a section name), so the
  options are in front of you while you are setting the section up
  rather than something to remember afterwards.

**All three read the same way.** Same order — acceleration first — the
same two labels, and the same explanations underneath, including the
plus-two / minus-two note describing exactly when FreePair will accept a
teammate pairing rather than distort the standings. Wherever you happen
to open the setting, you get the same answer.

Once a section has started pairing, the Overview tab and the Edit
Section dialog still *show* both settings — greyed out, with a note
saying the section has started — rather than hiding them. "Is this
section accelerated?" is a fair question mid-event, and it deserves an
answer rather than an empty space.

**A section paired as quads reads differently, because it works
differently.** Acceleration is not shown at all: a quad's schedule is
fixed in advance, so there is nothing for it to accelerate. Teammate
avoidance *is* shown, but labelled **Keep teammates in different
quads** and without a round window — a quad is three rounds long and
everybody meets everybody, so a round count would restate the event
instead of settling anything. The explanation underneath is the quad
one rather than the plus-two / minus-two note, which describes a
decision only Swiss pairing makes.

**And a four-player quad shows neither.** Every quad produced by a
split is exactly four players, which *is* one quad: the four play a
round robin whatever their teams, so there is no grouping left to
decide and no acceleration to apply. Offering either would be a choice
that cannot change anything, and a director who ticked it would
reasonably believe they had prevented something.

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

**🖨 Print as PDF** writes the whole report — totals, every round, and
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

**Byes & Withdrawals** sits at the top of the **Roster** tab, folded away
until you open it. The heading carries the count, so you can see whether
there is anything to look at without opening it. It used to be a tab of
its own; it moved because it is a short list you consult while working on
the roster, and having it elsewhere meant leaving the roster to answer a
question about the roster.

- **Half-point byes** are requested in advance by the player. Record them
  and the player is withheld from that round's pairing pool.
- **Full-point byes** are assigned by FreePair when a section has an odd
  number of players. The rules decide who receives one; a player who has
  already had a full-point bye will not normally get another.
- **Zero-point byes** cover a player who is absent without a request.
- **Withdrawing** a player removes them from future rounds while keeping
  the games they have already played.

**The Kind column abbreviates**, matching the **HPB** and **0-PB**
columns on the roster itself: **HPB (½)** is a half-point bye, **0-PB
(0)** a zero-point one, and **FPB (1)** a full point. The value in
brackets is what the bye is worth on the scoresheet, so you can check a
bye before it becomes a score without having to remember which
abbreviation is which. Printed reports still spell the kind out in full,
since a sheet pinned to a wall is read by people who do not have the
rest of the screen for context.

**A zero-point bye is one round; withdrawing is the rest of the event.**
For the last round the two amount to the same thing, and FreePair pairs
them identically — the player stays in the record and is not paired. They
differ earlier on: give somebody a zero-point bye in round 2 and they are
paired again in round 3 as normal, whereas a withdrawal keeps them out
until you reinstate them.

FreePair also works out who has withdrawn from the shape of the
crosstable, for files that do not carry the marking — a player who
played and has been absent ever since. **Coming back ends it**: a player
who missed a round and then played again is not withdrawn, however the
missed round was recorded. Only an absence that runs to the last scored
round counts.

Byes are shown on the Roster, on the Pairings tab with a count, and on
the Standings, so a bye is hard to miss.

**Double-click any bye to edit that player's bye requests.** It works in
both lists — the Byes & Withdrawals panel on the Roster, and Byes this
round on the Pairings tab — and opens the player straight on their **Bye
Requests** tab. These lists are what you read just before changing
somebody's byes, so they now do something about it rather than sending
you back to the roster to find the player by hand.

It is a double-click, not a single one, because a single click selects
and selection follows the arrow keys — reading down the list would
otherwise open a dialog on every row.

**A requested bye survives unpairing the round.** Pair a round, look at
it, decide you want it again — the requests that were honoured the first
time are honoured the second time. This holds for a request that arrived
in the file you opened as well as one you entered here, and for
zero-point requests as well as half-point ones. A request only stops
being live once its round has actually been played.

It also holds across a save. Closing the event and reopening it later
does not lose the requests that have already been served, so unpairing a
played round still hands them back — the file keeps the record even
though the request is no longer live.

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

Round robin, double round robin and quad sections get a **🖨 Print
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
Chart tab in a window (**🪟 Open in Window**) and the crosstable appears
above the grid, with a **Show crosstable** tick box to hide it. It is on
by default for round robin and quad sections, because for those formats
it is usually the view you want — the round below is one slice of it.

### Prizes

The **Prizes** tab is where the prize fund is declared, awarded, and
argued about. It handles place prizes, class and under prizes, and
special prizes — biggest upset, top female, top junior, top senior, top
unrated, top player from a named club or state, and director's awards
such as best game.

Its contents are grouped into collapsible panels — **Prize winners**,
**Not awarded**, **Prize projection**, **Prize fund**, and in a team
section **Team medals** and **Board prizes**. A panel appears only when
it has something to say, so a section before its prizes are calculated
shows just the fund. Each panel header carries the same controls as
every other tab: a **Focus** button, a **Filter** box with **Clear**, and
the **A- / A+** font stepper.

The filters are per panel rather than shared, since the tables answer
different questions — filtering the winners for a player should not
quietly narrow the fund you are reading beside it. Each searches every
column of its own table, anywhere in the text and ignoring case, so
"Under 1600", "trophy" and a surname all work.

**Focus** fills the window with one panel, and each panel has its own
button, so you can put the winners on the screen for the room without
the fund and the projection underneath. **F11** from the keyboard cannot
say which panel you meant, so it takes the winners.

#### Declaring the fund

**✏ Edit prizes…** opens the prize editor. The fund is built out of
**groups**, and a group is one *ladder* of prizes: a set of places open to
exactly the same players, awarded in the order they are listed. "1st
Under 1600" and "2nd Under 1600" are two places on one ladder; "1st
Place", "2nd Place" and "3rd Place" are three places on another.

Press **➕ Add prizes** and pick what you want from the menu:

- **Place prizes** — 1st, 2nd, 3rd, open to everyone in the section.
- **Class prizes** — Class A through E, each arriving with the right
  US Chess rating band already set.
- **Under prizes** — Under 2200 down to Under 1000, plus **Under …** if
  you need a ceiling that is not on the list.
- **Special prizes** — Biggest Upset, Top Female, Top Junior, Top Senior,
  Top Unrated, Top Club player, Top State player.
- **Director's awards** — Best Game, Brilliancy, Sportsmanship, and
  Other.

Each one arrives complete except for the money, which is the only part
that varies by event. Adding **Class B** sets the 1600–1799 window for
you; you never type a rating band, and so you never mistype one.

Most groups arrive with **three places** ready for amounts, because that
is what an event usually announces. The ones that cannot be ranked —
Best Game, Brilliancy, Sportsmanship, Other, and Biggest Upset — arrive
with **one**, since "2nd Best Game" is not a prize.

#### Inside a group

Each group is a panel you can collapse. The header always shows what the
ladder is, who can win it and what it costs in total, so a fund with
every group collapsed still reads as a prize list.

Open a group and you can:

- Fill in the **amount** for each place. The ordinals are labels, not
  fields — 1st is whatever is at the top — so rank and order can never
  disagree.
- **➕ Add place** for a 4th place, a 2nd Under 1600, and so on. **✕**
  next to a place removes it. The last place cannot be removed; use
  **🗑 Remove group** to delete the ladder.
- Adjust **who can win it**, which is entered once for the whole ladder
  rather than repeated on every place:
  - **Name** — what the places are called. With more than one place they
    become "1st …", "2nd …"; a single place uses the name as it stands,
    so a lone Top Female prize is called exactly that.
  - **Rating ≥ / Rating ≤** — inclusive at both ends. "Under 1600" means
    a ceiling of 1599. Shown only where a rating window means something.
  - **Unrated eligible** — whether unrated players may win a
    *rating-restricted* prize. Off by default, because US Chess treats
    unrated players as eligible for place and unrated prizes only. A
    prize with no rating window admits everybody regardless.
  - **Age ≥ / Age ≤** — measured on the event's last day, not today, so
    a file reopened after somebody's birthday still shows the same
    winner. A player whose roster row has no date of birth is not
    eligible for an age-restricted prize; FreePair will not guess.
  - **Club / state** — which one, for those two prizes.
  - **Trophy or medal — not cash** — tick this for a prize that is an
    *object* rather than money: a trophy, a medal, a plaque, a title. It
    changes three things. There is no amount to fill in, so the amount
    boxes disappear and the group contributes nothing to the declared
    fund. A tie for it is settled by the section's **tiebreaks** instead
    of being shared, because an object cannot be cut in half (US Chess
    32C) — this is the one place in the whole prize system where
    tiebreaks decide anything. And winning it does **not** use up the
    player's one cash prize, so the same player can take the champion's
    trophy and the first-place money.

#### What you can only have one of

Some entries in the menu grey out once you have used them, with a note
saying why. The rules come from what the prizes mean:

- **One place ladder per section.** "1st place" is a fact about the
  section — a second place ladder would be a second first place. If you
  want more places, add them *inside* the group.
- **One ladder per class or under band.** Two Under 1600 groups would
  admit exactly the same players, so each would award its own prizes and
  the same money would go out twice.
- **Club, state and director's awards can repeat**, because something
  the menu cannot know tells them apart: which club, which state, which
  award. Add one Top Club player group per club.

A trophy and a cash prize for the same players are *not* duplicates and
both are allowed — a championship trophy beside a $500 first prize is an
ordinary prize list, and FreePair treats them as the separate things they
are.

These rules follow the eligibility, not the button you pressed, so they
keep working if you edit a rating window by hand. If you edit two groups
until they admit the same players, Save will refuse and name both.

#### How the prizes are shared out

Three checkboxes at the bottom of the editor control this, and the
shipped settings are the US Chess ones. Change them only if your
tournament announcement said something different — that is the condition
the rulebook itself puts on them.

- **A player wins only one cash prize — the largest they are eligible
  for.** With this on, a player who is top Under 1600 *and* wins the
  section outright takes first place, and the Under 1600 prize passes
  down to the next eligible player. That roll-down is the behaviour most
  directors are looking for.
- **Players tied on score add their prizes together and divide
  equally.** Tiebreaks are never used for cash. Two players tied for
  first with prizes of $500 and $300 take $400 each.
- **Within a shared pool, nobody takes more than the largest prize they
  personally qualify for.** This is what stops a class player drawing a
  share of a pool larger than the biggest prize they were ever eligible
  for; anything the cap frees up goes to the others in the tie.

When a group of tied players can reach more prizes than there are players
in the tie, FreePair chooses the combination that pays out the most,
which is what US Chess asks a director to do by hand.

**Biggest upset** is computed from the games: the largest rating gap in a
decisive win by the lower-rated player. Draws do not count, wins by
forfeit do not count, and a game involving an unrated player on either
side does not count — an unrated player has no rating to be upset by, and
counting a blank as zero would win every upset prize ever offered.

#### Checking the fund before the event

The **Prize fund** table on the Prizes tab lists everything you have
declared, and its **Eligible** column is worth a glance the moment you
finish setting the prizes up. It counts the players in the section who
could actually win each prize.

A prize reading **0 — nobody** is one no player in the section can win:
usually a rating window with a digit wrong, or an age prize in a section
where nobody has a date of birth on the roster. FreePair also names those
prizes in an orange line above the table, because a zero in a column only
helps somebody who is already looking at that column.

This is worth catching early. A prize nobody can win looks exactly like a
correct one — it has a name, an amount and a place in the list — and
nothing else will ever tell you otherwise. Without this check you find
out at the prize-giving, in front of whoever thought they had won it.

Director's awards are not counted, since nothing can work out who is
eligible for a best-game prize; a blank there means it is waiting for
you, not that it is wrong.

#### Awarding them

**🧮 Calculate prizes** works the fund out against the standings. It is
meant to be run once every round is complete; if the event is not over
you will be warned and can still go ahead, and the result is marked
provisional.

The **Prize winners** table lists one row per recipient, with the prize,
the amount, and a note saying why it is what it is — "Pooled and divided
equally", the upset that won an upset prize, or your own words on an
award you made by hand.

Below it, **Not awarded** lists every declared prize that nobody
received, and why: nobody in the section qualifies, there are fewer
eligible players than there are prizes in that group, no upset was
played, or it is a director's award waiting for you. A prize that quietly
vanished is money nobody notices until an entrant asks about it, so
FreePair shows them rather than hiding them.

#### Changing an award by hand

Select a row in either table and press **✍ Edit award…**. You can name
different recipients, change how the money is split, or press
**Withhold** to record that the prize is deliberately not being awarded.
Whatever you write in the note prints beside the award.

This is the escape hatch, and it is meant to be used. No rulebook decides
a best-game prize, a player who has gone home, or a residency condition
that was in the entry form. Withhold is a separate button from Cancel on
purpose: cancelling changes nothing, while withholding is a decision that
appears on the sheet the room reads.

**↩ Undo hand edits** drops every hand-made award in the section and
returns it to what the rules produce. **🗑 Clear prizes** discards the
calculation entirely but keeps the fund.

#### Prize projection

While rounds remain, the tab shows a **Prize projection** below the fund:
what the prize list looks like on the scores so far, and for each player
still in the money, three figures.

- **Projected** — what they would take if the event ended right now.
- **Best case** — if they win every remaining game and nobody else gains
  a point.
- **Worst case** — if they gain nothing and everybody else wins out.

The **Status** column turns those into a word. *Guaranteed* means the
worst case is still above zero, so they are in the money whatever
happens — which is the question players actually ask at the desk before
the last round. *In contention* means it depends on the games. Players
who cannot win anything under any result are left off the table
entirely, because in a large section they are most of it.

Best and worst case are **bounds, not forecasts**. They are not jointly
possible — everybody cannot win out, because they play each other — but
no real result can take a player above their best case or below their
worst. FreePair does not calculate odds, because it does not know any,
and a percentage it invented would be quoted back at it by somebody who
lost money.

Once the event is over the projection disappears. At that point the
winners table is the answer, and a "projection" sitting beside it would
only invite you to wonder whether the two might differ.

#### On the Live Score Board and in print

The Live Score Board has a prize screen, on by default and switchable per
section in **Live Score Board setup**. Until you press Calculate it shows the
prize **fund** — each prize, who can win it, and the amount — and after
you calculate it shows the winners. It will not name a winner before you
have, because a projector saying "1st Place — Ada Lovelace — $500" in
round four is read by a hall as a result, and players act on what it
says.

**🖨 Print** produces the whole thing as a PDF: winners, unawarded
prizes with their reasons, the fund with its eligibility column, and a
footnote stating the sharing rules that produced the numbers.

---

## Printing and reports

**🖨 Print** produces a PDF of whatever you are looking at — pairings,
standings, wall chart, roster or prizes. Reports are landscape by default
because tournament tables are wide, and each carries the event name,
section, time control, location, dates, a page number and a timestamp.

**Print Setup** controls page options, and the grid and print fonts can
be adjusted independently, which is useful when a wall chart is one
column too wide for the page.

### A quad prints on one sheet

A quad is four players and three rounds of two boards, so a sheet per
round is three pages carrying six lines between them. FreePair prints the
whole quad on **one** page instead: rounds 1 and 2 side by side, round 3
below round 1. Ten quads costs ten sheets rather than thirty.

This is the default, and it does not matter how far along the section is.
All three rounds of a quad are paired the moment you pair it, so the
sheet is complete from the start, and rounds that have already been
played stay on it — a sheet that dropped finished rounds would renumber
itself as the day went on.

To print one round per page instead, untick **Print the whole quad on one
page** in **Page setup**. The option appears there only when the print you
are about to make covers a quad; it does nothing anywhere else, so it is
not shown anywhere else. The setting is remembered, like the rest of page
setup.

With it off, a quad prints the round it is **actually on** — the first one
not yet finished — not round 3 just because round 3 is the last one on the
schedule. The Round dropdown at the top of the Pairings tab opens on that
same round, and whatever you pick there is what prints.

The quad sheet is always **landscape**, even if you set the pairing sheet
to portrait. Two tables side by side need the width; on portrait paper the
type would shrink until the sheet was unreadable.

The file is named for the section rather than for a round — for example
`Quad 1-pairings.pdf` — because every round is on it.

A quad you have paired only part-way, and any other section, prints the
single round you are looking at exactly as before.

Under **⚙ Settings → Display**, **Use ASCII-only output** replaces
the ½ glyph with plain text for printers and downstream systems that
cannot render it.

### Printing the whole event at once

The **🖨** icon at the top right of the window — or **Event Operations →
🖨 Print** — holds four items that print every section in one go, so a
ten-quad scholastic does not mean ten trips through the section tabs and
ten save dialogs:

- **All Pairings**
- **All Standings**
- **All Wall Charts**
- **All Crosstables**

Each opens a chooser listing every section. **Sections that have been
paired start ticked**; one that has not started yet is listed but left
unticked, because it would only add a page of empty rows. Untick anything
else you do not want on the wall — the side-game, the practice group. A
section that cannot go in this particular report at all is listed but
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
pairings print defaults to **the round each section is actually on** —
the first one that is not finished, or the last round once they all are.
Choosing a specific round number instead prints that same round
everywhere, and quietly skips any section that has not reached it yet.

That distinction matters for quads and round robins, which pair their
whole schedule in one go. Their *last* round is the end of a timetable,
not the newest thing that happened, so printing it would hand you the
round 3 sheet before anyone had played round 1.

**Quads ignore the round choice**, because their page carries all three
rounds whatever you pick — see *A quad prints on one sheet* above. Untick
the quad option in **Page setup** if you want the chosen round on its own
sheet instead.

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

### The Live Score Board

The **🖥** icon at the top right of the window — or **Event Operations →
Live Score Board (full screen)** — opens a full-screen display for a projector
or a spare monitor — the screen in the skittles room that stops players
crowding round the paper on the wall.

**Setting it up.** A short dialog opens first, and what you choose is
remembered for next time:

- **Sections** — which sections appear, and whether each shows pairings,
  standings, or both. Untick a side-game or a one-round exhibition to
  keep it off the screen entirely.

**A team event shows its team tables too.** Before the board pairings
for a team section, the rotation puts up the round's team match-ups —
match number, the two teams with their seeds, and the board score as it
stands. Before the player standings it puts up the **team standings** —
place, team, match points, board points, which is what the event is
actually decided on. That is the question a captain and a spectator are asking; the
boards that follow are how a player finds their own game. A running
match shows its score part-finished, because that is what a room
watches; one where nobody has finished a board shows a dash rather than
0–0, which would read as a set of draws.
- **Pairing columns** — the Live Score Board's own, separate from the grid and
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
start on. A Live Score Board is read from a distance and at an angle by
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
no Live Score Board opens. **Restore default columns** is the way to undo.

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

**The clock in the corner shows seconds.** It is a wall clock for the
room, but it is also the answer to the question a projector always
raises: is this screen live, or did it freeze twenty minutes ago? A
minute-only clock cannot tell you, because it looks identical either
way. A second ticking over says the board is current before anybody has
to check a board number against the sheet.

**Under the clock is when the event last changed** — *Last updated at
17:32:05*. That is not when the board last looked at the event, which it
does every couple of seconds and would always read as "now". It is the
last time a pairing or a result actually moved. The two lines sit
together because they answer different halves of the same question: the
clock says the screen is alive, and this says whether anything has
*happened*. 18:40 above *Last updated at 17:32* is an hour of scoresheets
that has not reached the desk — worth knowing, and invisible without it.

It covers the whole event rather than the screen in front of you, so a
result typed into a section two slides away moves it. That is deliberate:
the change is real and will be on screen shortly, and a stamp tied to the
current screen would reset every time the rotation moved on, which would
make the number mean nothing.

**Which round it shows.** For a Swiss section, the round you last paired
— those are the board numbers the room is looking for, and an earlier
round still missing a result from a game that ran long must not pull the
screen backwards.

**Nothing reaches the board until you accept it.** A round you are still
looking at in the pairing preview — swapping a colour, moving a board,
granting a late half-point bye — is not on the projector, is not on the
shared board, and is not on NA Chess Hub. It appears the moment you
accept the preview, and a preview you cancel never appears at all. The
hall should not be reading a pairing you have not finished deciding.

Round robins, double round robins and quads are different, because their
whole schedule is paired in one go. There the Live Score Board shows **the
first round that is not finished** — round 1 the moment the quad is
paired, then round 2 as soon as every round-1 result is in, and so on by
itself. Once the last round is complete it stays there, showing that
round with its results. Before, it showed the final round from the
moment the section was paired, which sent players to boards they were
not due at for another two hours.

**It uses columns you choose.** The Live Score Board has its own pairing and
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

**You can also close it from the desk.** Once a board is up, **Event
Operations → Close Live Score Board** appears, and the setup panel — the
🖥 button, or `Ctrl+B` — grows a **Close Live Score Board** button beside
its usual two. Either shuts the board down where you are standing.

That matters more than it sounds. The board opens full screen on the
display your working window is *not* on, which is the whole point of it;
but it meant the only way to stop it was to walk to that screen and press
Escape. Fine at a desk with two monitors, less so with a projector at the
far end of a hall. Nothing is lost by closing it — the board keeps no
state of its own and reopens in two clicks — so neither route stops to
ask.

Sharing to phones and televisions is a **separate** thing on its own
window, and closing the projector does not stop it. Turning the screen
off at the end of a round is not the same as asking the room to stop
following the event on their phones.

**Closing FreePair closes the board.** You do not have to remember to
shut the projector down first — quitting the application takes every one
of its windows with it. Previously the board was left running on a
display you could no longer see, still showing the last round, after you
had closed FreePair and packed up.

**Two screens or one.** If you have a second display, the Live Score Board
opens full screen on it and leaves your working window alone. If you
have only one, it opens as an ordinary window you can move and resize —
a full-screen display covering the grid you are trying to enter results
into would be no use to anybody. Press **F11** when you are ready to go
full screen, and again to come back.

Standings only appear once a round is complete — before that they would
be a list of zeroes in seeding order.

### Putting the Live Score Board on a TV or on phones

The **📺** icon — next to the Live Score Board icon at the top right, or
**Event Operations → Share Live Score Board** — turns the
board into a web page that anything on the same network can open. It is
read-only: people can look, and cannot change the event. The same window
can also share the board publicly on NA Chess Hub — see *Sharing
the Live Score Board on NA Chess Hub* below.

Press **Start sharing** and FreePair shows an address like
`http://192.168.1.50:8080/` with a QR code beside it. Sharing stays on
until you turn it off or close FreePair, and the page updates itself as
you pair and score — nobody needs to refresh anything.

**It is the same board as the projector.** The shared page and the
full-screen Live Score Board read one set of settings, so they always show the
same sections, the same columns and the same rotation. **⚙ Live Score Board
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

### Sharing the Live Score Board on NA Chess Hub

Below the address, the same window has **Share Live Score Board on NA Chess Hub**.
This puts the same board on a public web page — one anybody can open,
from anywhere, without being on the venue Wi-Fi at all.

It is available only when the event has an **NA Chess Hub event ID and
passcode** filled in on the Event tab. Those are what prove to the hub
that it is you sending the board. A local club night has neither, which
is not an error: the address above is the whole feature for those
events.

Press **Start sharing on NA Chess Hub**. FreePair sends the board straight away, then
keeps sending it as you pair and score. Once the first copy has landed
you get the public link and a QR code for it, and **Open in my browser**
to check it looks right.

**It is not either/or.** Sharing on the hub and sharing on the local
network are separate switches and you can run both. They solve different
problems — the local address is the only thing that works when the venue
has no internet, and the hub is the only thing that works when the venue
Wi-Fi refuses to let devices see each other, which most hotel and school
Wi-Fi does.

**Why the page says when it was last updated.** A board that has stopped
updating but still looks perfectly normal is the worst thing this
feature could do: it will seat twenty players at the wrong tables and
nobody will question it, because boards do not usually lie. So FreePair
keeps telling the hub it is still there, even when nothing has changed,
and the hub marks the page as no longer live within a few minutes of
FreePair going quiet — you closing the laptop, the internet dropping, or
the event simply being over. The page says so in words rather than
carrying on.

That is also why the window shows you a timestamp rather than a green
light. "Last sent at 14:52" is something you can check against your
watch.

**Going quiet does not take the board down.** This is worth knowing
before it happens to you. If FreePair stops sending — a closed lid over
a lunch break, a venue Wi-Fi drop between rounds — the hub adds a "not
updating, last updated …" notice after about five minutes, and **keeps
showing the pairings**. They stay readable for about six hours after the
last update. So a break does not clear the screen in the playing hall;
it only stamps what is on it as possibly out of date, which is exactly
what a player standing in front of it needs to know.

**Turning it off.** **Stop sharing on NA Chess Hub** takes the board down
there and then. Closing the window does not — sharing carries on,
deliberately, because it is something you switch on in the morning and
forget. Closing FreePair also leaves the board up: it stops being marked
live within minutes, and the hub removes it a few hours later. If you
want it gone immediately, press **Stop sharing** rather than just
quitting.

**If it will not send.** Three messages are worth knowing:

- *Rejected the event passcode* — this is the event's **upload
  passcode** from its page on the hub, not your account password. Fix it
  under Event Setup.
- *No event with this ID* — check the Event ID under Event Setup.
- *Too large for NA Chess Hub to accept* — showing fewer sections, or
  fewer rows per screen, in **Live Score Board settings** brings it down.
- *Not accepting the Live Score Board at this address* — nothing you have
  done, and nothing you can fix. NA Chess Hub is refusing the board, and
  the feature may not be live on their site yet. Sharing on the local
  network, above, is unaffected and is the thing to use meanwhile.
- *The live board was removed on NA Chess Hub* — somebody with access to
  the event pressed **Delete Live Board** on its page there. That might
  have been you, or another director. FreePair stops rather than putting
  it straight back; press **Start sharing on NA Chess Hub** again if it
  was not meant.

FreePair stops trying after any of those, because none of them fix
themselves and retrying a wrong passcode every few seconds all day is
not a polite thing to do to somebody else's server. A dropped network,
by contrast, it simply keeps trying through.

**What is on the page.** Exactly what the projector shows: names,
ratings, boards, results, standings. The same information that is pinned
to the wall. Nothing from the roster that is not — no email addresses,
no phone numbers — and never your passcode.

### QR codes for the live board

Once you are sharing the board — either way — FreePair puts a QR code
where people can get at it, so nobody has to read a web address aloud
across a room.

**In the app.** Small codes appear at the right-hand end of the tab row —
beside *Overview / Roster / Pairings / …* on a section, and beside
*Basic / Team Event / Results Publishing / …* on **Event Configuration**.
Click one for a large, properly scannable copy with the address written
underneath. They appear only while you are actually sharing, so the row
is unchanged if you never use the feature.

The Event Configuration copy is there because setting the event up is
when somebody is most likely to lean over and ask for the address, and
you should not have to click away from the page you are working on to
answer.

**Open in browser.** The enlarged code has a button that opens the same
address on this computer. Useful for checking the board looks right
before the round starts, or for putting it on a projector — not everyone
who wants the board has a phone in their hand.

**On every printed sheet.** Pairings, standings, wall charts, prize
lists — all of them carry the codes in the header, beside the event QR.
Tape a pairing sheet to the wall and a player can scan it and watch the
rest of the round from their phone.

**There are two codes because they fail in opposite situations**, and a
player has no way of knowing which situation they are in:

- **"on this network"** — served from your laptop. The only one that
  works when the venue has no internet at all. Useless to somebody on
  mobile data.
- **"anywhere"** — the NA Chess Hub page. The only one that works when
  the venue Wi-Fi refuses to let devices see each other, which most hotel
  and school networks do.

Share both when you can, and let people scan whichever works. If you are
only sharing one, only that code appears.

**The printed codes are a snapshot.** The laptop's address stops working
once you leave the venue or join a different network — by which time the
sheet is last round's pairings anyway.

### Tidying up the name order

After **Verify IDs**, FreePair may offer to rewrite names as **"Last,
First"**. It only offers this for players whose ID verified — a name on
its own cannot say which word is the surname, but the record behind a
confirmed ID can.

`Gledura Benjamin` becomes `Gledura, Benjamin`. That is the form US Chess
and FIDE publish, the form SwissSys stores, and the form reports and the
wall chart sort by, so a roster typed the other way round sorts oddly
next to players entered conventionally.

**Only the order changes.** If the database spells a name differently
from you — `Illya` where you wrote `Illia` — your spelling is kept.
Reordering is a small, reversible tidy-up; replacing what you typed with
a foreign transliteration is a much larger decision, and not one FreePair
makes for you.

The prompt says how many players would change and shows the first few, so
you can decline if the order is deliberate. Nothing happens unless you
say yes.

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

  **The name is filled in for you, numbered.** Renaming
  `MCC - 2026-09` offers `MCC - 2026-09 - 001`, then `- 002`, then
  `- 003`, so keeping a numbered series through the day is Save As and
  Enter — no typing unless you want to. Anything you add of your own is
  kept and numbered from there: `MCC - 2026-09 - Mark - 001`,
  `- 002`, and so on.

  The number comes from what is **already in the folder**, not from the
  file you have open, so reopening an earlier snapshot and saving from it
  still continues the series instead of offering a name that exists.
  FreePair only treats a **three-digit** ending as a number, so an event
  named for a year — `MCC - 2026` — becomes `MCC - 2026 - 001` rather
  than `MCC - 2027`.
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

## Compatibility with SwissSys

### Why the file is called `.sjson`

Your whole event lives in one file whose name ends `.sjson`, and the name
is literal. It is **JSON** — a plain-text data format used across the
software industry — describing a **Swiss**-system tournament. Swiss
JSON. The "S" is the pairing system the file describes, not the name of
any program.

That matters mainly because it tells you what you are holding. An
`.sjson` file is data, not a document belonging to one piece of software.
You can open one in a text editor and read it: the sections, the players,
the rounds and the results are all in there as readable text. On the day
something goes wrong at an event, being able to look inside the file and
see what it actually says is worth a great deal.

### Opening events made in SwissSys

**FreePair opens events created in SwissSys.** Browse to the file with
**📂 Open Event → Re-Open Locally Saved Event**, and it opens
like any other event: players, sections, pairings, results and
standings.

**Anything FreePair does not manage is kept, not discarded.** When you
save, FreePair rewrites only the parts of the file it understands and
leaves everything else exactly as it found it. Settings written by
another program survive a trip through FreePair even though FreePair
never shows them to you.

### Taking an event back the other way

**SwissSys opens events FreePair has saved.** Your players, sections,
pairings, results and standings are all recorded in the ordinary way, so
the event carries on there as you would expect and pairing is unaffected.

**What does not travel is the part FreePair added.** A few things have no
standard place in the format — your decision log, accelerated-pairing
settings, display preferences and similar — so FreePair records them
under entries of its own. Another program ignores entries it does not
recognise. Nothing is corrupted and nothing is lost from your file; those
features are simply not available while the event is open elsewhere, and
anything that depends on them is not there to see.

The practical version: the event moves in both directions, and what you
give up going out of FreePair is FreePair's own conveniences, not your
tournament.

### A note on names

SwissSys is a separate commercial product, and FreePair is not affiliated
with it, endorsed by it, or derived from it. FreePair reads and writes
the same file format so that directors are not locked into one program by
their own event data. Product names are mentioned here only to describe
that compatibility.

---

## NA Chess Hub

### Signing in to NA Chess Hub

**⚙ Settings → Online → NA Chess Hub sign-in.** Signing in once lets you
reach every event your NA Chess Hub account runs without typing a
passcode for each one.

Click **Sign in to NA Chess Hub** and your browser opens on NA Chess
Hub's own sign-in page. You sign in there, exactly as you would on their
website — **FreePair never sees your password**, and any two-factor step
stays between you and them. When it finishes, the browser says so and you
come back to FreePair, which shows which account is connected — by its NA
Chess Hub name, such as **PNW Chess Center (PNWCC - Organizer)**, rather
than the email address it was registered with. Worth a glance if you hold
both a personal login and a club one.

Once signed in, two menus list your own events, each with its own search
box that remembers what you last typed:

- **➕ New Event → Create New Event Using Online Registry → My Events on
  NA Chess Hub…** — start a *new* event from its NA Chess Hub entry list.
- **📂 Open Event → My Events Saved on NA Chess Hub…** — re-open an event
  you have already been running and saved to NA Chess Hub, with its
  pairings and results as you left them.

Both open the event you pick with no passcode, and both list events
delegated to you as well as ones you registered yourself.

The two are easy to mix up, so it is worth being clear about the
difference: the first builds a fresh tournament from the people who
signed up, and the second returns you to work already in progress. If
you are in the middle of an event, you want the second one.

Each list shows only your **current** events. Tick **Show all my events
on NA Chess Hub** to include finished ones too — useful for looking
something up after the fact, but left off by default so that a club with
years of history does not have to scroll past all of it to find
tonight's tournament.

**Uploading always uses the event passcode, however the event was
opened.** Publishing pairings and results, cloud backup and roster sync
are unchanged by signing in. This is deliberate: an upload works or
fails for the same reason every time, whether you are signed in or not.
Nothing is lost by it, because an event you downloaded by signing in
brings its passcode with it — FreePair already has what it needs.

**Signing in is optional and changes nothing else.** Per-event passcodes
work exactly as before, which is what you still want when somebody hands
you a single event to run for them.

**Signing out removes the sign-in from this computer.** It does not
cancel it at NA Chess Hub — there is no way to do that from FreePair yet
— but it expires on their side on its own within two weeks. On a shared
or borrowed machine, sign out when you are done.

**Signing out does not let you sign in as somebody else, and this
surprises people.** If your browser is signed in to NA Chess Hub, so is
FreePair: clicking Sign in hands you that account instantly, with no page
shown, and signing out of FreePair and back in gives you the same account
again. That is because NA Chess Hub remembers you in the *browser*, and
signing out of FreePair only removes the sign-in from this computer.

To change accounts, use **Use a different account…** next to the sign-in
buttons. It signs your browser out of NA Chess Hub and opens their
sign-in page so you can pick the account you want — one click, nothing to
do by hand. It is worth knowing this if you hold both a personal login
and a club one.

You may occasionally be asked to sign in again. The sign-in lasts about a
fortnight, so coming back to a tournament after a few weeks away is the
usual reason.

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
and the roster's HPB column shows that round whether the bye is still a
pending request or has already been played out.

**The HPB and 0-PB columns show every bye on record**, not only the ones
a player asked for in advance. A bye you grant when adding somebody
mid-event — a half point for the rounds they missed — appears there too,
alongside the requested ones. Before this, those byes showed on the Byes
& Withdrawals panel but left the roster cell blank, so the same player
appeared to have a bye in one place and none in another.

Turn the alerts on or off, and set how often they run, under
**⚙ Settings → Online → Alert when NA Chess Hub roster has updates
to sync**.

**Play a sound when the count changes** sits under that option and is
off until you turn it on. It uses your computer's own notification
sound, so it follows the system volume and Do Not Disturb — handy when
you are working on pairings and not watching the badge. It plays in
either direction, since an entry being withdrawn is worth hearing about
too, and it stays quiet when your own sync clears the count.

### Publishing pairings and results

Publishing puts pairings and results on the event's public page so
players can follow along. You can set new events to publish
automatically under **⚙ Settings → Online**.

**This is not the same as sharing the Live Score Board.** Publishing
uploads *files* — the pairing sheet, the standings, the event file — to
the event's page, where they sit until you publish again. Sharing the
Live Score Board sends the *live rotating display*, the one on the
projector, and keeps sending it so the page can say whether it is still
current. They are separate switches and neither replaces the other; see
[Sharing the Live Score Board on NA Chess
Hub](#sharing-the-live-score-board-on-na-chess-hub).

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

**For the USCF and NWSRS reports, everything that can be reported starts
ticked**, so if you have nothing to leave out, press Continue and you are
done. Untick anything that should not be submitted. Nothing is deleted and
your event file is not touched — the choice applies to this one export.

**The FIDE report and the TRF export are stricter**, because they take one
section rather than the whole event. They tick a section only when it is
FIDE-rated *and* every round has its results in. If none qualifies, nothing
is ticked and you choose for yourself — deliberately, because a default
that is wrong on a submission is the one a busy director accepts.

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

### Checking the affiliate and TD IDs

The affiliate ID and the two TD IDs are bare codes copied from somewhere
else, and a wrong one is invisible — the export succeeds and the rating
office rejects the submission days later, by which time you are running a
different event.

Click the **🔎** beside any of those three fields and FreePair names who
or what the ID belongs to, right there in the row: the club for an
affiliate ID, the director for a USCF ID. Reading a name you recognise
takes a second and settles it.

- **Nothing is checked for you automatically.** Each lookup is a click,
  because it goes out to the network and you may well be offline at the
  venue. The ID you typed is what gets exported either way.
- **Editing an ID clears the name.** That is deliberate. A name left
  sitting beside a number you have since changed is worse than no name,
  because it looks like the new number was checked.
- **"Not active" is not a typo.** If an affiliate comes back named but
  marked not active, the ID is right and the affiliation needs renewing
  before the report will be accepted.
- **"Couldn't reach…" is not a verdict.** It means the lookup failed,
  not that the ID is wrong. Try again when you have a connection.

**If you do not have a TD's ID to hand, press 🔎 with the box empty.**
It opens the player database in your browser so you can find them by
name and copy the ID back. You are already at that button when you
discover you do not know your assistant's number, and being told "Enter
a USCF ID first" is a dead end.

The affiliate 🔎 does not do this, because the player database holds
players — an affiliate code is not in there to be found. Affiliate codes
come from your club's US Chess account.

The **🔗** button beside each field still opens the full US Chess page
for that ID in your browser.

It writes **three files** next to your `.sjson`: `THEXPORT` (the event),
`TSEXPORT` (the sections) and `TDEXPORT` (the players and their games).
Each is a `.DBF` with your event name added on the end — for example
`TDEXPORT_Spring Open.DBF` — so several events can be exported into the
same folder without overwriting one another. Send all three; the rating
office reads them together, and the names before the underscore are what
it recognises them by, so do not rename them.

---

## Filing the FIDE rating report

**Event Operations → 📤 Export Report → FIDE Rating Report** writes a TRF
report for one section,
ready to send for FIDE rating. FreePair asks which section, fills in
everything the event already knows and asks you for the rest: host city,
federation, chief arbiter and any deputies. Those are remembered for next
time.

You do not need a section selected first. The chooser lists every section
with its rating type, player count and round status, so you can see which
one is actually FIDE-rated before you file it. Nothing is ticked unless a
section is both FIDE-rated and has every round fully scored — if none
qualifies you pick one yourself, which is deliberate: a default that is
wrong on a submission dialog is worse than no default.

**This is not the same as "Sections to TRF (engine input)"** lower down
the same submenu, which now asks which section in the same way. Both write a
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

## FIDE title norms

The **Norms** tab appears on FIDE-rated sections, and on any section you
have set to pair with the FIDE engine. Everywhere else it is not shown at
all — norms are a FIDE title matter, and a US-Chess-only event cannot
produce one, so there is nothing for the tab to say.

The tab answers three questions: does this event qualify for the large
Swiss exemption, who is in line for a norm, and what does the arbiter
still have to confirm before signing anything.

**The tab is three collapsible groups**: the event verdicts, the norm
candidates, and the arbiter's checklist. Each is read at a different
point in an event's life, so each folds away on its own. A fourth appears
in the one situation it is useful — see **What this game is worth**
below.

**The two event-level verdicts each take one line, with the working
behind a 🔍 Details… button.** The event requirements and the large Swiss
test — the one directors call the **Super Swiss**, which is why the
heading says both — are read closely once, when the event is set up or
when somebody disputes the answer, and glanced at every other time. Left
open on the tab they pushed the candidates table, which is what most
visits are actually for, below the fold. The line always states the
verdict; the button is there for the visit where you need to defend it.

**Everything here is read-only.** Nothing on this tab changes your event.

### What FreePair can and cannot decide

FreePair checks all of Article 1.4 of the FIDE Title Regulations — the
part that is arithmetic over your crosstable. Counted games, the opponent
rating floor, the average opponent rating, the performance rating, the
titled-opponent requirements, the federation rules: all of it, with the
article number against each check.

It cannot check most of Article 1.1, and neither could any program.
Whether the event was registered with FIDE thirty days ahead, whether the
chief arbiter held the right licence and was in the venue — none of that
is in an event file. Those are listed at the bottom of the tab as a
checklist for you to answer, and your answers print on every certificate.
**Anything you leave unanswered prints as outstanding, not as a pass**,
which is deliberate: a signed certificate should never claim something
was checked when nobody looked.

For the rules themselves, **📖 Norm rules** opens FreePair's full norms
reference — what each norm requires, worked through, per event type. It
works offline.

### Event requirements

The first line checks the two things that belong to the event rather than
to any one player, and says plainly which of them is wrong rather than
how many are. **🔍 Details…** opens both with the article number, what
was required, and what FreePair read.

**Rounds scheduled** against the nine games Article 1.4.1.1 asks for. A
shorter schedule is not fatal on its own — a player can still reach nine
games across two events — but it tells you at a glance that no norm will
be earned here alone.

**Time control** against Article 1.1.3, which wants each player to have
at least two hours for sixty moves. FreePair reads what you typed and
does the arithmetic, **and increments count**: `G/90;+30` is 90 + 60×0.5
= exactly 120 minutes and qualifies, while the same 90 minutes without
the increment does not. That thirty seconds is the whole difference
between a norm event and a nice tournament, and it is easy to miss.

Time controls are free text, so this one has three answers rather than
two. If FreePair cannot read what is written, or if the notation is
genuinely ambiguous about whether an increment runs from move one, the
result is **?** and the note under the 1.1.3 checklist item says what it
made of it. **It never ticks the box for you.** Calling an unreadable
control compliant would let an ineligible event look eligible; calling it
too fast would deny a norm somebody earned. The answer stays yours.

### Setting the host federation

FreePair fills this in from the event's own country, using **FIDE's**
three-letter code rather than the more familiar ISO one — they disagree
for a long list of chess countries, and it is FIDE's that players carry.
Germany is `GER` and not `DEU`, Switzerland `SUI`, the Netherlands `NED`.

Change it if the organising federation is not the one the event is being
held in. If FreePair leaves the box empty it could not place the country
with confidence and would rather you typed it than have it guess — the
United Kingdom is the common case, because FIDE has separate codes for
England, Scotland, Wales and Northern Ireland and none for the whole.

The large Swiss test counts rated players who are *not* from the host
federation, so nothing about it can be decided until this is set, and the
tab says as much until you do.

This is remembered for the session but is not saved into the event file.

**If every count reads zero,** the roster has no FIDE federations in it.
The whole rule is about which federation each player belongs to, so
without that FreePair can only report nobody. Run **Verify IDs** on the
Roster tab for a FIDE-rated section and it fills in each player's FIDE
id, rating, federation and title, and those are saved with the event.

Files saved by FreePair before this was fixed lost the federation and the
FIDE rating when they were closed — the ids and ratings in the ID2 and
Rating2 columns survived, but the federation was not written. Re-run
**Verify IDs** once on such an event and it will be right from then on.

### Is this a "Super Swiss"?

FreePair checks your section against **Article 1.4.3d**. It needs, in
every round, at least 20 FIDE-rated players who are not from the host
federation, from at least 3 federations, at least 10 of them holding GM,
IM, WGM or WIM. Players only count if they missed at most one round — a
pairing-allocated bye is not a missed round, since that was the
tournament's doing rather than the player's.

**The tab gives you the verdict and a line of counts** — how many of the
three thresholds were met, whether every round reached them, and how many
players do not count towards the total. That last number is the one that
decides whether the rest is worth reading, which is why it is on the tab
and not only inside.

**🔍 Details…** opens the working: the three thresholds, the field round
by round, every player who does not count, and the arbiter's notes. Each
threshold is listed with what it needs and what you have, so a section
that just misses tells you by how much. **Copy** puts the whole thing on
the clipboard for an email to a rating officer or a note in the file.

**The field, round by round.** Under the thresholds is a row per round
showing how many of the qualifying rated non-host players were in the
field that round, how many federations they came from and how many were
titled, and whether that round on its own cleared all three.

Two rules decide who is in the field for a round, and both come straight
from 1.4.3d. First, only players who count for the *event* appear at all
— someone who missed two or more rounds is excluded from every round,
**including the ones he played**, because the rule counts players rather
than seats. So one player with two requested byes takes a round of 20
down to 19 even in a round he sat down for.

Second, **a pairing-allocated bye still counts as being in the field**.
That is the rule's own parenthesis: a bye the pairing system handed out
because there was nobody to pair him with is the tournament's doing, not
the player's, and 1.4.3d declines to hold it against him. A requested
half-point bye or a TD-assigned zero-point bye is his own absence and
does reduce the round.

A round that falls below any of the three thresholds is shown **in red**
and **fails the whole section**. Article 1.4.3d asks for the field "in
every round", and FreePair reads that literally: one thin round is enough
to withhold the exemption, and the thresholds table carries a fourth row
saying which rounds were short.

This is the strict reading, and it is worth knowing that it bites. A
field of exactly twenty has no slack at all — a single player taking his
one permitted bye leaves nineteen at the board that round, and the
section fails on it. If you would rather that were advisory, say so; the
looser reading counts the qualifying players once across the event and
ignores which rounds they sat out.

Two things never put a round in the red. A **pairing-allocated bye** is
not an absence — that is the rule's own parenthesis. And a player who
missed more than one round is already out of the field entirely, so he
cannot make a round short by being absent again.

**Players who do not count.** Below the rounds is the list that makes the
totals auditable: every rated player from outside the host federation who
would have swelled the count and did not, with the reason and the number
of rounds they missed.

This is where "why is it 20 and not 24?" gets answered. A player who
missed more than one round is out of the field **entirely** — including
the rounds he did play — so a single name here can take a round of 20
down to 19 even in a round he sat down for. That is the rule's own
wording, and it surprises people, which is why the reason column says it
outright.

Two things are deliberately kept off this list. **Host-federation players
are not shown**: they are excluded by the rule's design rather than by
anything going wrong, and on a typical open they are most of the entry —
listing them would bury the handful of names worth reading. And a
**pairing-allocated bye is not a missed round**, so it never puts anybody
here.

If somebody appears with "No FIDE federation recorded" or "No FIDE rating
recorded", that is a gap in the roster rather than a verdict on the
player — he might be the twentieth. **Verify IDs** on the Roster tab
fills those in.

**If it qualifies, read what that actually buys you.** It waives one
requirement: that a norm-seeker face opponents from at least two
federations other than their own. It waives nothing else. The limits on
how many opponents may share one federation still apply, the
titled-opponent requirements still apply, and the average-rating and
performance thresholds still apply. This is the most commonly
misunderstood rule in the title regulations, and believing it relaxes
everything is how a director ends up promising a player a norm they
cannot earn. The verdict line says so on the tab, the dialog says it in
full, and it is on the printed summary.

### What this game is worth

Once the last round is **paired but not played**, a fourth panel appears
above the candidates: **Norm projection — round N**. It answers the
question a player actually asks at the desk on the last morning, and the
candidates table cannot, because that table reports games already played.

The opponent is now known — their rating, their title, their federation —
so nothing here is an estimate. Each of the **Win**, **Draw** and **Loss**
columns is the norm re-checked with that result written into the event, by
the same calculation that produces the certificate. **Needs** reduces the
three to the sentence you would say out loud: *Must win*, *Draw or
better*, *Already secured*, or *Not possible*.

**"Not possible" is the half worth having.** Some players cannot earn a
norm whatever happens on the board — the federation spread is not there,
or there were too few titled opponents, or the average opponent rating is
below the floor. Those are requirements a result cannot move, and the
**Why not** column names the article and the numbers. Telling a player
that before the round is kinder than telling them after it, and it stops
you promising something that was never available.

Rows are ordered with the players who have something to play for first,
then those already safe, then those who cannot get there. The panel header
carries the same **Focus**, **filter** and **font stepper** as every other
table on the section — the filter matches either side of the board, so
typing a name finds that player's row and the row of whoever is playing
them, and typing `must win` lists everyone who still has something to
play for.

**Focus** on either of the Norms tab's two grids fills the window with
that one grid. The event verdicts and the arbiter's checklist step aside
while it does — both are sized to their content, and between them they are
most of the tab's height.

The panel disappears as soon as the results are in: the norms are then
decided, and the candidates table says so.

### Who is in line for a norm

**Norm candidates** is a collapsible panel, like the tables on the
Pairings and Standings tabs. It opens expanded — it is what most visits
to the tab are for — and folds away if you are here to read the event
verdicts instead.

The candidates table lists each player against the norms **they are
actually chasing** — not all four. A grandmaster needs no norm at all and
is left out entirely; a male international master is only shown the GM
norm, because IM is already behind him.

The rule is two ladders, and a player is offered a title only if it
stands above what they already hold:

- **Open ladder** — GM above IM above FM. Only open titles count here, so
  a WGM is still shown the IM norm.
- **Women's ladder** — WGM above WIM above WFM. An open title counts at
  its own rank, so a female IM is shown WGM but not WIM: she is already
  past the WIM title.

So a female FM, a WFM and an untitled woman are all shown all four; an
untitled man and a male FM are shown GM and IM; a WIM is shown GM, IM and
WGM.

**Only women can earn WGM and WIM norms**, so those rows appear only for
players FreePair knows are female. A women's title is proof on its own —
nobody holds a WIM by accident — and otherwise it comes from the sex
recorded on the roster.

If that is blank, the tab says so above the table rather than quietly
omitting the rows, because a woman with no sex recorded looks exactly
like a man to the filter and the norm she may have earned would simply
never appear. **Verify IDs** on the Roster tab fills it in from the
player's FIDE record.

By default the table shows only players who either have a norm or can
still get one; tick **Show every player**, in the panel's own header, to
see the rest. It sits there rather than in the tab toolbar because it
changes what this one table lists and nothing else — up there, beside the
host federation, it read like a setting for the whole event.

Beside it, the same controls every other table on the section has: a
**Focus** button, a **filter** that matches on name, pair number,
federation, title, norm code or result, and the **font stepper**. Type
`Norm` in the filter to see only the players who have earned one.

**A row whose Result reads Norm is filled green.** It is the one outcome
on this tab worth spotting from across a room, and "Norm" sits in the
same column as "No" — same first letter, a glance apart.

For each row you get the games counted, the score, the average opponent
rating, the performance rating, and a verdict of **Norm**, **Possible**
or **No**, with what is still outstanding beside it.

**What "Possible" means.** It means the norm has not been earned yet but
can still be, arithmetically, in the rounds that remain — the score is
reachable, and the opponent mix can still be supplied by the pairings to
come. It is a live prospect, not a prediction: nothing about it says the
player will get there.

Once a section has no rounds left, nothing is Possible any more. Every
row is either **Norm** or **No**, because there is nothing left to
change. Some requirements can also be lost outright before the last
round — the 1.4.4 caps are maximums, so once a player has faced too many
opponents from one federation, further games cannot undo it and the
verdict turns to **No** immediately.

Select a row and the panel underneath explains where that player stands.
Mid-event that means what they still need: how many points from the
remaining rounds, how many more titled opponents, how many more
federations. If a norm has become impossible it says which requirement
can no longer be met, and it says so as early as the arithmetic allows —
telling a player on the last morning is kinder than telling them
afterwards.

**The full breakdown.** Double-click any row — or press **🔍 Full norm
breakdown…** — for the whole calculation in one window: the player's
score, Ra and Rp against what the title needs, every requirement of
Article 1.4 with its verdict, and every game the calculation counted,
with each opponent's title, federation, rating, colour and result.

This is the view to open when somebody asks *why*. The candidates table
has room for one phrase, which is enough to sort the list but not enough
to defend a decision. A norm is refused on a specific article with a
specific number, and both the director signing the certificate and the
player being turned away are entitled to see which one.

Three things in it are worth knowing about.

A requirement marked **exempt** in purple was **waived, not met**. That
happens when the event qualifies as a large Swiss under 1.4.3d, which
excuses the applicant from needing opponents of two other federations —
and excuses nothing else. It is shown as its own outcome rather than as a
tick because Article 1.4.3e requires at least one norm in a title
application to be earned *without* an exemption, so this is the first
thing a federation will query.

A game may be annotated **raised to floor (1.4.6.3)**. Exactly one
opponent — the lowest-rated — may be lifted to the title's rating floor
for the calculation, so the number shown is not that opponent's real
rating.

Byes and forfeits are absent from the games list on purpose. Only games
actually played count towards a norm, and a point awarded because an
opponent did not arrive says nothing about anybody's strength.

**Copy** puts the whole breakdown on the clipboard as text, for an email
to a rating officer or a note in the event file.

**Print norm report…** writes the signable norm certificate for that
player as a PDF — the same document as the **📄 Norm certificate…**
button on the tab toolbar, so there is only ever one norm report and it
cannot drift. It contains the verdict, the event and applicant details,
every counted game with the opponent's FIDE ID and the rating actually
used, the Ra / dp / Rp working, the requirements table, your Article 1.1
answers and signature blocks for the chief arbiter and the federation's
rating officer.

It prints for a player who **missed** a norm too, with a red banner and
the failing requirement named. That is deliberate: the commonest use of a
norm report is explaining a refusal, and a player who came close is
entitled to the same worked calculation as one who got there.

The PDF lands in the event folder and opens in your usual viewer, which
is where you print it on paper from.

**Treat the score figures as a guide, not a promise.** The remaining
opponents are not known yet, so the projection assumes the average
opponent rating stays where it is. In a Swiss a norm-chaser is usually
near the top and will meet stronger players than average, so what they
actually need often eases as the event goes on.

One difference worth knowing: in a **Swiss**, a norm once secured cannot
be lost — a player may ignore every game after a title result. In a
**round robin** it can, because a norm there must be based on all the
scheduled rounds. The panel tells you which case you are in.

### Printing

**🖨 Print** writes what the tab is showing, in the order it shows it: the
1.4.3d verdict with each threshold, then — while the last round is paired
but unplayed — the norm projection with the win / draw / loss columns and
the reason beside anyone who cannot get there, then a row per candidate
with the calculation and the outstanding requirements. The projection is
left out once the results are in, because there is then nothing to
project.

**📄 Norm certificate…** produces the signable document for the selected
player and norm. It carries the event and player details, every counted
game with both the published rating and the rating actually used in the
average, the Ra / dp / Rp calculation, every requirement with its article
and its numbers, your Article 1.1 answers, and signature blocks for you
and for your federation's rating officer.

The certificate shows its working on purpose. Where one opponent's rating
was raised to the norm's floor, that player is marked and named in a
footnote — only one opponent may ever be raised, and it must be the
lowest-rated, so it matters that you can see which one it was. Unrated
opponents are shaded and footnoted with the rating they were credited
with.

**It is not FIDE's IT1 form.** It carries every field the regulations
require, but the official form's layout is not reproduced, and it says so
at the foot of the page. Where your federation wants the IT1, use this as
the worked calculation to copy from and file alongside it.

Ctrl+P on the Norms tab prints the summary.

---

## Filing the NWSRS rating report

**🎒 NWSRS Report** on **Event Operations → 📤 Export Report** writes the file NWSRS
needs to rate your event. It appears only when something in the event is
NWSRS-rated, so on a purely US Chess or FIDE event you will not see it.

**NWSRS rates from the event file itself.** There is no separate report
format to fill in, so the export asks you two things only: which sections
to include, and where to put the file. FreePair saves your event first,
then writes the report wherever you choose.

**The report is a clean copy, not simply your event file renamed.**
FreePair's file carries entries that are FreePair's business rather than
the rating system's, and the report is rebuilt without them so that
nothing but the event itself reaches NWSRS.

What that leaves out is exactly that: your decision log, your column
layouts, your USCF affiliate details, the display settings, your tiebreak
order, your half-point-bye allowance. The decision log in particular is
your own reasoning about calls you made during the event, which is
nobody's business outside the tournament.

What it keeps is the event, the players and every game: each player's
ID, rating, club, grade and contact details, and their result in every
round exactly as recorded — same opponent, same colour, same board,
same result. Nothing about the games is recalculated on the way out.

**Your NWSRS IDs are put where NWSRS reads them.** When a section is
paired on NWSRS ratings, FreePair keeps the NWSRS ID and rating in the
main **ID** and **Rating** columns, because those are the numbers it
pairs from. SwissSys arranges them the other way round — the US
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

Settings apply to the application, not to one event. Open them with
**⚙ Settings** at the top right of the toolbar. They open in a
window of their own; there is no Save button, because every change is
written as you make it. **Close** is the only way out, and Escape does
the same thing.

They are split across tabs:

- **Files** — where events are saved, and whether you are prompted for
  a folder each time.
- **Pairing** — pairing-engine behaviour, re-showing the pairing-engine
  notice, and the USCF and FIDE engine binaries. The binaries are
  bundled with the installer and normally need no attention.
- **Display** — theme, the size of app text and grid text, and
  ASCII-only output for printing.
- **Online** — publishing defaults, the NA Chess Hub address, and the
  service used to verify IDs and ratings.
- **Updates** — update checks, the release channel, your current
  version, and — when a check finds one — a button to install it without
  leaving the page. Also where you go back to an earlier release, and
  where any crash reports are listed.
- **Shortcuts** — a reference card for the keyboard shortcuts. It is the
  only tab that changes nothing; it is here so the list is available
  without leaving the application, which matters in a hall with no
  internet. See [Keyboard shortcuts](#keyboard-shortcuts).

---

## Troubleshooting

**FreePair closed unexpectedly.** It writes a crash report each time that
happens. The quickest way to them is **⚙ Settings → Updates**, which says
how many there are and when the most recent was, with a button to open
the folder. They live in **`%AppData%\FreePair\CrashReports`**.

The report holds the error, the FreePair version, your operating system,
and the file name of whichever event was open. It holds **no player
names, ratings, contact details or results**, and FreePair never sends it
anywhere. Whether it leaves your machine is entirely your decision;
attaching the newest one to a bug report is the single most useful thing
you can do. The twenty most recent are kept and older ones are discarded.

**The mouse pointer turned into an hourglass.** FreePair is working on
something that takes a moment — opening an event, or running the pairing
engine. The pointer goes back to normal on its own when the work
finishes. Opening a large event is the usual cause; nothing is wrong and
there is nothing to click.

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

### Reporting a bug or asking for a feature

FreePair is shaped by what directors report, and both are welcome at
**FreePairChess@Gmail.com**.

**Help → About FreePair** has the address as a link, a **Copy address**
button for machines with no mail program set up, and — more importantly
— the version you are running, with its own **📋 Copy** button so you can
paste it rather than transcribe a date-stamp. Include that version in
anything you send. Nearly every report that cannot be acted on is one
where the version is missing, because the first question is always
whether the behaviour still happens in the current build.

Also worth including: what you expected to happen and what happened
instead, the newest crash report if FreePair closed on you, and — for
anything about a pairing — the event file itself. A pairing question
without the file is usually unanswerable; with the file it is usually
answerable in minutes.

---

## About this guide

This guide describes FreePair **v0.95.20260901**. It is updated whenever a
change affects what you see or do.

The copy that ships with the app is the one that matches your installed
version. An online copy is published with each release; if you are
reading that one, check the version above matches the FreePair you are
running.
