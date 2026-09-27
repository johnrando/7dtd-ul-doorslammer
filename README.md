# DoorSlammer

A 7 Days To Die mod. Closing a door on a zombie takes health off **both the zombie and the door**
— 5% of the zombie's max health and 5% of the door's by default, or a flat HP amount if you prefer.

It puts a small price on a very safe tactic: trap a zombie behind a door, open it, land a hit,
slam it shut, repeat. Now the slam itself does a little work, and costs you a little door.

## Installing

Download the zip from [Releases](https://github.com/johnrando/ul-doorslammer/releases) and extract
it into the game's `Mods/`. The mod folder is the root of the archive, so it lands as:

```
Mods/DoorSlammer/
├── ModInfo.xml
└── DoorSlammer.dll
```

Load order does not matter, and nothing needs building.

## Console commands

`ds` prints the menu and changes nothing — `doorslammer` is an alias. Every line names the command
that changes it and says what it is for, so the menu is also the reference:

```
DoorSlammer is ON
  ds on|off             : [ >on< | off ]             - damage a zombie caught in a slammed door
  ds mode               : [ >percent< | flat ]       - read 'ds dmg' as a % of max health, or as flat HP
  ds dmg {z} {d}        : 5% to zombie / 5% to door (flat: 10 / 10)
  ds sound              : [ off | impact | >break< ] - play a material-relevant sound on slam
  ds rage               : [ >off< | on ]             - suppress UL's chance to rage from slam damage
  ds flavor fw          : [ >on< | off ]             - FletchWounds: a slam drives your arrows deeper
  ds flavor wl          : [ >on< | off ]             - WhackLash: a slam can floor a zombie you have been working on
  ds flavor sb          : [ >on< | off ]             - Stumblr: a slam can trip the zombie
  ds floor {hp}         : enemies below 20 health will not be affected by slam damage
  ds tuning {cd} {dist} : 1 sec cooldown / 0.35 range
```

`ds on` and `ds off` are the master switch — with it off the door-close hook returns immediately, so
a slam does nothing and every other setting is inert. They say which state you want rather than
toggling, so the command reads the same whichever state you were in and repeating it is harmless.

`ds mode` picks how `ds dmg` is read. **Percent**, the default: each number is a percentage of that
target's max health, so a slam takes the same fraction off a 50 HP stall door as off a 21,000 HP
vault hatch, rounded to the nearest whole HP and never rounded down to nothing. **Flat**: each
number is whole HP, the same on every door. `ds mode` alone flips between them; `ds mode flat`,
`ds mode percent` or `ds mode pct` says which. `ds dmg {z} {d}` then sets the pair the current mode
reads, zombie first — percentages run from 0 to 100 and take decimals, like `2.5`. Both pairs are
remembered, so switching back finds the other pair as you left it.

`ds tuning` takes the per-door cooldown in seconds and the reach past the frame in metres. A setter
called with no arguments prints its usage and current value. **Changes are saved** — see
[Settings file](#settings-file).

`ds flavor` lists the extra behaviour supported mods offer, one switch per mod, and changes
nothing. `ds flavor fw`, `ds flavor wl` or `ds flavor sb` toggles one of them, by the other mod's
command name; `ds flavor on` and `ds flavor off` set them all. Right now that is **FletchWounds**,
**WhackLash** and **Stumblr**. FletchWounds: catch a zombie that still has one of your arrows in it
and the door drives that arrow deeper. WhackLash: a zombie you have been hitting, with its focus
meter up, can be knocked down by the door, with a chance per meter point set by `wl door`. Stumblr:
a zombie caught in the door can go down in one of the game's own stumble animations, with a chance
set by `sb door`. Each effect is the other mod's doing rather than the door's, so what it costs is
documented there.

Every pair of mods is switched **on both sides**, and toggling it in either one sets both: `ds
flavor fw` and `fw flavor ds` are the same switch. Other pairs are left alone, so FletchWounds'
interaction with CrawlerGuts, say, can stay on while its interaction with the door is off. A mod
this build does not know about is let through until you switch it off, and gets a line of its own
once it has been seen.

Two more: `ds info` prints the same block with the patch state and counters added, and `ds reset`
zeroes those counters.

## What a slam does

- Catches **at most one zombie** — the nearest one standing in the doorway.
- Does **nothing else**: no knockdown, stun, knockback or hit reaction. The damage is credited to
  nobody, so it never sets the zombie on you and grants no XP.
- **Cannot land a killing blow.** Each hit is capped to whatever health the target has above the
  never-kill floor, so a target at or below the floor is left alone and one above it lands exactly
  on the floor rather than through it. Slamming whittles a zombie down and then stops, and wears a
  door down without breaking it open — at any `ds dmg`, in either mode, including one far larger
  than the floor. Only `ds floor 0` removes that.
- Costs nothing when no zombie is caught, so ordinary door use around your base is free.

## Sound

A slam plays a sound only when HP actually came off, so one spared by the floor or by the
trader-area check is as silent as it is harmless.

Nothing is bundled: both modes name a sound the game already plays on that exact door, keyed on the
door's own `SurfaceCategory`. So every door type sounds like itself, a modded door the mod has
never seen still gets the right sound, and one whose material has none gets silence.

| Mode | Sound |
|---|---|
| **impact** | `organichit<material>` — what a zombie's fist sounds like on that door |
| **break** (default) | the door's own `DestroyFX` sound, else `<material>destroy` — what it sounds like when it breaks |

**Neither is audible to zombies.** The sound goes out with the entity id left at `-1`, which is
what `Manager.Play` checks before signalling the AI — so no AI noise and no screamer heat, which
the break sounds carry in quantity. Vanilla's own door sounds do signal the AI; a slam deliberately
does not.

## Defaults

All settable in-game, and all written back to the settings file as soon as you set them. These are
what a first run starts from:

| Setting | Default |
|---|---|
| slam sound | break |
| damage mode | percent |
| damage to the zombie | 5% of max health (10 HP in flat mode) |
| damage to the door | 5% of max health (10 HP in flat mode) |
| never-kill floor | 20 HP remaining |
| cooldown per door | 1 s |
| reach past the door frame | 0.35 m |
| enhanced mod interaction | on |

The cooldown stops a held or macro'd activate key from grinding out damage frame by frame.

## Settings file

Every setting survives a restart. A change made with `ds` is written straight out to:

```
%APPDATA%/7DaysToDie/DoorSlammer/settings.txt
```

— the game's own user data folder, next to `Saves`, rather than `Mods/DoorSlammer/`, so updating
the mod does not take your settings with it. `ds info` prints the full path and whether the last
read or write worked.

It is plain `key = value` text, one line per setting, each naming the command that sets it:

```
enabled             = on       # ds on|off
damage.mode         = percent  # ds mode - percent or flat
damage.zombie       = 10       # ds dmg {z} {d} in flat mode
damage.door         = 10       # ds dmg {z} {d} in flat mode
damage.zombie.pct   = 5        # ds dmg {z} {d} in percent mode
damage.door.pct     = 5        # ds dmg {z} {d} in percent mode
sound               = break    # ds sound - off, impact or break
rage                = off      # ds rage
flavor.fletchwounds = on       # ds flavor fw
flavor.whacklash    = on       # ds flavor wl
flavor.stumblr      = on       # ds flavor sb
floor               = 20       # ds floor {hp}
cooldown            = 1        # ds tuning {cd} {dist}
range               = 0.35     # ds tuning {cd} {dist}
```

Edit it by hand with the game closed — it is rewritten whenever a `ds` command changes something.
A line that will not parse is logged and ignored rather than fatal, and deleting the file brings
back the defaults above (which live in `Settings.cs`). A file from an older build has no
`damage.mode` line, so it comes up in percent mode; its `damage.zombie` and `damage.door` values
are kept as the flat pair, and `ds mode flat` brings them back into play.

## Undead Legacy

**Not required** — the mod works fine on a plain install, and is built to sit alongside UL without
modifying anything of UL's. Tested against **UL 2.7.36**; all of UL's ordinary doors are covered.

**`ds rage`.** UL rolls a chance to enrage a zombie on every bit of damage it takes, however small
— only around 0.63% per slam at UL's default, but across the many slams it takes to whittle one
down that adds up to better than a coin flip, and a super rage brings an alert scream with it.
`ds rage` suppresses that roll for slam damage only, leaving ordinary combat alone. Off by default.
Without UL, it suppresses vanilla's own rage roll instead.

**Wide doors count in full** — the doorway checked is the door's whole footprint in its placed
rotation, so a zombie caught in either leaf of a double closet or commercial door, or anywhere
under a cellar door, is fair game. Earlier builds only checked the one column above the door's
parent block.

**Powered doors are not covered** — powered vault doors, garage doors and the like.

**UL door HP varies enormously** — a bathroom stall is 50, a standard wooden door 250, a vault
hatch 21,000 — so a fixed HP per slam means very different things depending on the door. That is
why percent mode is the default: 5% is 3 HP off a stall and 1,050 off a hatch. `ds mode flat` gets
the fixed-HP behaviour back.

## Limitations

- Doors opened by an **electrical trigger** are not covered, only doors opened by hand.
- **Dedicated-server clients get nothing.** Door activation is client-side, so slams only register
  in single-player or on a host.

## Building

Requires the .NET SDK; there are no NuGet dependencies. The mod builds in place inside the game
install, against the game's own assemblies.

```
dotnet build src/DoorSlammer/DoorSlammer.csproj -c Release
```

That restages `dist/DoorSlammer/`, ready to copy into `Mods/`. To also build the release archive:

```
dotnet build src/DoorSlammer/DoorSlammer.csproj -c Release -t:Package
```

That writes `release/DoorSlammer-v<version>-<date>.zip`, taking the version from `ModInfo.xml`.
Neither `dist/` nor `release/` is tracked — the zip is published as a GitHub Release instead.

## License

MIT — see `LICENSE`.
