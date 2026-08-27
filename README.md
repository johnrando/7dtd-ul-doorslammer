# Door Slammer

A 7 Days To Die mod. Closing a door on a zombie takes health off **both the zombie and the door**
— 1 HP off the zombie, 10 HP off the door by default.

It puts a small price on a very safe tactic: trap a zombie behind a door, open it, land a hit,
slam it shut, repeat. Now the slam itself does a little work, and costs you a little door.

## What a slam does

- Catches **at most one zombie** — the nearest one standing in the doorway.
- Does **nothing else**: no knockdown, no stun, no knockback, no hit reaction. The damage is
  credited to nobody, so it never sets the zombie on you and grants no XP.
- **Never lands a killing blow.** Anything at or below the never-kill floor is left alone, so
  slamming whittles a zombie down and then stops, and wears a door down but never breaks it open.
- Costs nothing when no zombie is caught, so ordinary door use around your base is free.

## Console commands

| Command | Effect |
|---|---|
| `ds` or `doorslammer` | toggle the mod, and print status + counters |
| `ds rage` | toggle rage suppression, **off** by default |
| `ds reset` | zero the counters |

Everything is a toggle, so status prints on each one — `ds` twice shows you where things stand
and leaves the mod as it was.

## Installing

Copy `dist/DoorSlammer/` (checked into this repo, so no build needed) into the game's `Mods/`:

```
Mods/DoorSlammer/
├── ModInfo.xml
└── DoorSlammer.dll
```

Load order does not matter.

## Settings

Defaults, all tunable in `Settings.cs`:

| Setting | Default |
|---|---|
| damage to the zombie | 1 HP |
| damage to the door | 10 HP |
| never-kill floor | 10 HP remaining |
| cooldown per door | 0.5 s |
| reach past the door frame | 0.35 m |

The cooldown stops a held or macro'd activate key from grinding out damage frame by frame.

## Undead Legacy compatibility

**Undead Legacy is not required** — the mod works fine on a plain install. It is built to sit
alongside UL without modifying anything of UL's, and is tested against **UL 2.7.17**. All of UL's
ordinary doors are covered.

**`ds rage`.** UL rolls a chance to enrage a zombie on every bit of damage it takes, however
small. One slam is only around 0.63% at UL's default setting, but across the many slams it takes
to whittle a zombie down that adds up to better than a coin flip — and a super rage brings an
alert scream with it. `ds rage` suppresses that roll for slam damage only, leaving ordinary combat
alone. Off by default. Without UL installed, the same toggle suppresses vanilla's own rage roll.

**UL powered doors are not covered** — powered vault doors, garage doors and the like. Nobody is
hand-slamming those anyway.

**UL door HP varies enormously** — a bathroom stall is 50, a standard wooden door 250, a vault
hatch 21,000 — so a fixed 10 HP per slam means very different things depending on the door.

## Limitations

- **Doors opened by an electrical trigger are not covered**, only doors opened by hand.
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
