# Door Slammer

A 7 Days To Die mod. Closing a door on a zombie takes health off **both the zombie and the door**
— 1 HP off the zombie, 10 HP off the door by default.

It puts a small price on a very safe tactic: trap a zombie behind a door, open it, land a hit,
slam it shut, repeat. Now the slam itself does a little work, and costs you a little door.

## Installing

Copy `dist/DoorSlammer/` (checked into this repo, so no build needed) into the game's `Mods/`:

```
Mods/DoorSlammer/
├── ModInfo.xml
└── DoorSlammer.dll
```

Load order does not matter.

## Console commands

`ds` toggles the mod and prints the menu — `doorslammer` is an alias. Every line names the command
that changes it, so the menu is also the reference:

```
Door Slammer is now ON
  ds sound              : [ off | >impact< | break ]
  ds rage               : [ >off< | on ]
  ds dmg {z} {d}        : 1 to zombie / 10 to door
  ds floor {hp}         : will not cause damage below 10 health
  ds tuning {cd} {dist} : 1 sec cooldown / 0.35 range
```

`ds tuning` takes the per-door cooldown in seconds and the reach past the frame in metres. A setter
called with no arguments prints its usage and current value; changes last until the game restarts.

Two more: `ds info` prints the same block with the patch state and counters added, and `ds reset`
zeroes those counters.

## What a slam does

- Catches **at most one zombie** — the nearest one standing in the doorway.
- Does **nothing else**: no knockdown, stun, knockback or hit reaction. The damage is credited to
  nobody, so it never sets the zombie on you and grants no XP.
- **Never lands a killing blow.** A target at or below the floor is left alone, so slamming
  whittles a zombie down and then stops, and wears a door down but never breaks it open.
- Costs nothing when no zombie is caught, so ordinary door use around your base is free.

## Sound

A slam plays a sound only when HP actually came off, so one spared by the floor or by the
trader-area check is as silent as it is harmless.

Nothing is bundled: both modes name a sound the game already plays on that exact door, keyed on the
door's own `SurfaceCategory`. So every door type sounds like itself, a modded door the mod has
never seen still gets the right sound, and one whose material has none gets silence.

| Mode | Sound |
|---|---|
| **impact** (default) | `organichit<material>` — what a zombie's fist sounds like on that door |
| **break** | the door's own `DestroyFX` sound, else `<material>destroy` — what it sounds like when it breaks |

**Neither is audible to zombies.** The sound goes out with the entity id left at `-1`, which is
what `Manager.Play` checks before signalling the AI — so no AI noise and no screamer heat, which
the break sounds carry in quantity. Vanilla's own door sounds do signal the AI; a slam deliberately
does not.

## Defaults

All tunable in `Settings.cs`, and all but the sound settable in-game:

| Setting | Default |
|---|---|
| slam sound | impact |
| damage to the zombie | 1 HP |
| damage to the door | 10 HP |
| never-kill floor | 10 HP remaining |
| cooldown per door | 1 s |
| reach past the door frame | 0.35 m |

The cooldown stops a held or macro'd activate key from grinding out damage frame by frame.

## Undead Legacy

**Not required** — the mod works fine on a plain install, and is built to sit alongside UL without
modifying anything of UL's. Tested against **UL 2.7.19**; all of UL's ordinary doors are covered.

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

That restages `dist/DoorSlammer/`. Rebuild before committing so `dist/` matches `src/`.

## License

MIT — see `LICENSE`.
