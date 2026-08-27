# Door Slammer

A 7 Days To Die mod. Closing a door on a zombie takes a little health off **both the zombie and
the door** — 1 HP off the zombie, 10 HP off the door by default.

It exists to put a small price on a very safe tactic: trap a zombie behind a door, open it, land a
hit, slam it shut, repeat. Now the slam itself does a bit of work, and costs you a bit of door.

**Undead Legacy is not required.** The mod patches only vanilla methods, so it works on a
plain install; it is simply also built to stay out of Undead Legacy's way (tested against 2.7.17).
No Undead Legacy file is modified: the hooks are on vanilla methods that UL inherits.

## What a slam does, and what it deliberately doesn't

A slam catches **at most one zombie** — the nearest one standing in the doorway — and does nothing
else. No knockdown, no stun, no knockback, no hit-reaction animation. The damage is credited to
nobody, so it never sets the zombie on you and grants no XP.

A slam **never lands a killing blow**. Anything at or below the never-kill floor (10 HP remaining
by default) is left alone. So slamming whittles a zombie down and then stops — finishing it takes
a real hit — and it can wear a door down but never break it open.

If no zombie is caught, nothing is damaged. Ordinary door use around your base costs you nothing.

## Console command

`ds` toggles the mod. Every toggle prints the full status block, which is where the counters
live — so `ds` twice shows you status and leaves the mod as it was:

| Command | Effect |
|---|---|
| `ds` or `doorslammer` | toggle the mod, and print status + counters |
| `ds rage` | toggle rage suppression, **off** by default |
| `ds reset` | zero the counters |

The line to watch is **`door closes checked`**. The startup log only proves the hook was
installed; that number proves door closes are reaching it. If it climbs but `caught a zombie`
stays at zero, the zombie isn't being seen as inside the doorway.

### `ds rage`

Undead Legacy rolls a rage chance on *every* damage response, scaled by how hard the hit was. A
1 HP slam is only about 0.63% at UL's default setting — but spread over the many slams it takes to
whittle a zombie down, that adds up to better than a coin flip, and super rage brings an alert
scream with it.

`ds rage` suppresses that roll **for slam damage only**; ordinary combat is untouched. It is
**off by default**, because staying out of UL's way is the safer default. Without UL, the same
toggle suppresses vanilla's own rage roll.

## Settings

Defaults, all live-tunable in `Settings.cs`:

| Setting | Default |
|---|---|
| damage to the zombie | 1 HP |
| damage to the door | 10 HP |
| never-kill floor | 10 HP remaining |
| cooldown per door | 0.5 s |
| reach past the door frame | 0.35 m |

The cooldown is what stops a held or macro'd activate key from grinding damage out frame by frame.

## Known limitations

- **Undead Legacy powered doors are not covered.** UL's `BlockULM_PoweredDoor` extends
  `BlockULM_Powered` rather than `BlockDoor` and uses a different open/closed encoding. No player
  is hand-slamming a powered vault door anyway.
- **Electrically triggered doors are not covered.** Those toggle through `OnTriggered`, which
  bypasses the method this mod hooks.
- **Dedicated-server clients get nothing.** The door-activation path is client-side, so the hook
  only runs in single-player or on a host. `ServerAuthoritative.cs` documents the server path in
  full but is not wired up.
- Undead Legacy door HP varies enormously — a bathroom stall is 50, a standard wooden door 250, a
  vault hatch 21,000 — so a flat 1 HP means very different things depending on the door.

## Building

Requires the .NET SDK. The mod is built in place inside the game install and compiles against the
game's own assemblies; there are no NuGet dependencies.

```
dotnet build src/DoorSlammer/DoorSlammer.csproj -c Release
```

That stages a ready-to-copy mod folder at `dist/DoorSlammer/`. Install it by copying that folder
into the game's `Mods/` directory. `dist/` is committed so the mod can be installed without a
toolchain; rebuild before committing so it matches `src/`.

## License

MIT — see `LICENSE`.
