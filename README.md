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
- **Makes a sound**, picked automatically from the door's own material, and one the AI cannot
  hear. See below.

## Console commands

| Command | Effect |
|---|---|
| `ds` or `doorslammer` | toggle the mod, and print the settings |
| `ds sound` | cycle the slam sound: impact → break → off. **Impact** by default |
| `ds rage` | toggle rage suppression, **off** by default |
| `ds dmg {z} {d}` | set the damage to the zombie and to the door |
| `ds floor {hp}` | set the never-kill floor |
| `ds tuning {cd} {dist}` | set the per-door cooldown in seconds, and the reach past the frame in metres |
| `ds info` | print the patch state and the counters |
| `ds reset` | zero the counters |

Every command reports the state it left behind, and the settings block doubles as the menu: each
line names the command that changes it, and the two toggles list their choices with the live one
marked. So `ds` twice shows you where things stand and leaves the mod as it was.

```
Door Slammer is now ON
  ds sound              : [ off | >impact< | break ]
  ds rage               : [ >off< | on ]
  ds dmg {z} {d}        : 1 to zombie / 10 to door
  ds floor {hp}         : will not cause damage below 10 health
  ds tuning {cd} {dist} : 1 sec cooldown / 0.35 range
```

A setter takes effect immediately and lasts until the game is restarted; the defaults live in
`Settings.cs`. Called with no arguments it prints its usage and the current value.

## Installing

Copy `dist/DoorSlammer/` (checked into this repo, so no build needed) into the game's `Mods/`:

```
Mods/DoorSlammer/
├── ModInfo.xml
└── DoorSlammer.dll
```

Load order does not matter.

## Settings

Defaults. All of them are tunable in `Settings.cs`, and all but the sound are also settable
in-game from the commands above:

| Setting | Default |
|---|---|
| slam sound | impact |
| damage to the zombie | 1 HP |
| damage to the door | 10 HP |
| never-kill floor | 10 HP remaining |
| cooldown per door | 1 s |
| reach past the door frame | 0.35 m |

The cooldown stops a held or macro'd activate key from grinding out damage frame by frame.

## The slam sound

A damaging slam plays a sound on the door — only when HP actually came off, so a slam spared by
the never-kill floor or by the trader-area check is as silent as it is harmless.

Nothing is bundled. Both modes name a sound the game already ships and already plays on that exact
door, keyed on the door's own `SurfaceCategory` — `wood`, `metal`, `stone`, `glass`, `cloth`,
`earth`, `organic`, `plant`. So every door type sounds like itself, a modded door the mod has never
seen still gets the right sound, and one whose material has none gets silence rather than a wrong
one.

| Mode | Sound | Where it comes from |
|---|---|---|
| **impact** | `organichit<material>` — `organichitwood`, `organichitmetal`, … | `ItemActionAttack.Hit` composes a block-hit sound as `{attackerMadeOf}hit{surface}`, and zombie hands are `Material Morganic`. This is literally the sound of a zombie punching that door. |
| **break** | the door's own `DestroyFX` sound if it names one, else `<material>destroy` | What `Block.SpawnDestroyFX` plays when that door is destroyed. Also its downgrade sound: `SpawnDowngradeFX` falls through to the same call, and no vanilla or UL door sets `DowngradeFX`. |

Impact is the default because it is authored for repetition — three clips, pitch variation — and
reads as "a body hit the door". Break is the bigger, splintering one-shot, which on every slam can
read as "the door just broke", something the never-kill floor guarantees has not happened.

**Neither is audible to zombies.** The sound goes out through `Audio.Manager.BroadcastPlay(pos,
name)`, which leaves the entity id at `-1`; `Manager.Play` only signals the AI when it is handed an
`EntityPlayer`. So there is no AI noise and no screamer heat — which the break sounds carry in
quantity (`metaldestroy` is `noise="20" heat_map_strength="1.42"`). Vanilla's own door open/close
sounds do signal the AI; a slam deliberately does not, in keeping with the rest of what a slam does
not do.

`ds info` reports the last sound name a slam resolved to. That is worth having because
an unknown name fails silently by design — `Manager.Play` returns at its lookup, with no log line —
which is exactly what makes guessing a name off a material safe.

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
