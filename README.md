# DoorSlammer

A 7 Days To Die mod. Closing a door on a zombie takes health off **both the zombie and the door**
— 10 HP off the zombie, 10 HP off the door by default.

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
  ds dmg {z} {d}        : 10 to zombie / 10 to door
  ds sound              : [ off | impact | >break< ] - play a material-relevant sound on slam
  ds rage               : [ >off< | on ]             - suppress UL's chance to rage from slam damage
  ds flavor             : [ >on< | off ]             - enhanced mod interaction with FletchWounds
  ds floor {hp}         : enemies below 20 health will not be affected by slam damage
  ds tuning {cd} {dist} : 1 sec cooldown / 0.35 range
```

`ds on` and `ds off` are the master switch — with it off the door-close hook returns immediately, so
a slam does nothing and every other setting is inert. They say which state you want rather than
toggling, so the command reads the same whichever state you were in and repeating it is harmless.

`ds tuning` takes the per-door cooldown in seconds and the reach past the frame in metres. A setter
called with no arguments prints its usage and current value. **Changes are saved** — see
[Settings file](#settings-file).

`ds flavor` switches on the extra behaviour a supported mod offers, and names whichever it found.
Right now that is **FletchWounds** — catch a zombie that still has one of your arrows in it and
the door drives that arrow deeper. The arrow is FletchWounds' doing rather than the door's, so
what it costs is documented there. Both mods carry the switch and **toggling either one moves
both**, so you only ever have to set it in one place.

Two more: `ds info` prints the same block with the patch state and counters added, and `ds reset`
zeroes those counters.

## What a slam does

- Catches **at most one zombie** — the nearest one standing in the doorway.
- Does **nothing else**: no knockdown, stun, knockback or hit reaction. The damage is credited to
  nobody, so it never sets the zombie on you and grants no XP.
- **Cannot land a killing blow.** Each hit is capped to whatever health the target has above the
  never-kill floor, so a target at or below the floor is left alone and one above it lands exactly
  on the floor rather than through it. Slamming whittles a zombie down and then stops, and wears a
  door down without breaking it open — at any `ds dmg`, including one far larger than the floor.
  Only `ds floor 0` removes that.
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
| damage to the zombie | 10 HP |
| damage to the door | 10 HP |
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
enabled       = on       # ds on|off
damage.zombie = 10       # ds dmg {z} {d}
damage.door   = 10       # ds dmg {z} {d}
sound         = break    # ds sound - off, impact or break
rage          = off      # ds rage
flavor        = on       # ds flavor
floor         = 20       # ds floor {hp}
cooldown      = 1        # ds tuning {cd} {dist}
range         = 0.35     # ds tuning {cd} {dist}
```

Edit it by hand with the game closed — it is rewritten whenever a `ds` command changes something.
A line that will not parse is logged and ignored rather than fatal, and deleting the file brings
back the defaults above (which live in `Settings.cs`).

## Undead Legacy

**Not required** — the mod works fine on a plain install, and is built to sit alongside UL without
modifying anything of UL's. Tested against **UL 2.7.24**; all of UL's ordinary doors are covered.

**`ds rage`.** UL rolls a chance to enrage a zombie on every bit of damage it takes, however small
— only around 0.63% per slam at UL's default, but across the many slams it takes to whittle one
down that adds up to better than a coin flip, and a super rage brings an alert scream with it.
`ds rage` suppresses that roll for slam damage only, leaving ordinary combat alone. Off by default.
Without UL, it suppresses vanilla's own rage roll instead.

**Powered doors are not covered** — powered vault doors, garage doors and the like.

**UL door HP varies enormously** — a bathroom stall is 50, a standard wooden door 250, a vault
hatch 21,000 — so a fixed 10 HP per slam means very different things depending on the door.

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

That writes `release/DoorSlammer-<version>-<date>.zip`, taking the version from `ModInfo.xml`.
Neither `dist/` nor `release/` is tracked — the zip is published as a GitHub Release instead.

## License

MIT — see `LICENSE`.
