# TODO

- Convert Weapon collectable in weapon level increment + rarity lights
- React on **host** disconnect
- Tnt
    - The explosion is handled by WorldManager::EditVoxel(*, 0).
    - Recursively, when another TNT is met, another explosion is triggered.
- Multiple lobby spawns lots of entries in menu. Code not always rendered
    - Code is rendered only for the host of that lobby

# IN PROGRESS
- `Add Firebase Storage integration.`

# DONE
- ~~The player respawns after a kill~~
- ~~Skin and helmet texture not loading for both owner and non-owner. PlayerMarker are of wrong color.~~
- ~~Helmet not spawning. After death, clients cannot throw grenades nor bazooka. Audio clip across the map~~
- ~~Take kills & deaths into consideration in dashboard~~
- ~~Add in-game esc menu~~
- ~~Team selector~~
- ~~Show menu bg while loading + loading sprite~~
- ~~Integrate Lobby service~~
- ~~Integrate Relay service~~
- ~~Lobby menu~~
- ~~Click to respawn, load spawn camera.~~
- ~~Helmet removal on first head Hit + sound~~
- ~~Collectables weapons/ammo~~
- ~~Introduce a secondary weapon~~
- ~~Add weapon variation based on texture~~
- ~~Shift to run with stamina bar~~
- ~~Ctrl to crunch and avoid falling~~
- ~~Bazooka weapon~~
    - ~~Automatically switch to the previous weapon when ammo runs over~~
- ~~Friendly fire must be disabled~~
- ~~Scopes~~
- ~~Mipmap with positions~~
- ~~Melee should deal enemy damage~~
- ~~Player death (~~ragdoll~~, respawn)~~
- ~~Weapon Reload~~
- ~~Damage Text spawn~~
- ~~Switch the weapon while firing/aiming not change the weapon~~
- ~~Move player state from Inventory manager to PLayer~~
- ~~Grenades + explosions~~
- ~~Self ragdoll~~
