<img width="256" height="256" alt="icon" src="https://github.com/user-attachments/assets/bca47f01-a5dc-4f9d-a297-855e1a130c72" />

# Weapon Tiers
A mod for SULFUR that adds 4 new weapon tiers that can randomly roll on weapon drops, granting them multipliers for various weapon stats!

<img width="394" height="546" alt="image" src="https://github.com/user-attachments/assets/42c62cd4-3c9a-42b3-8c8f-4004dc4c33c8" />

Tiered weapons have colored tier names, tooltip bonuses, and a animated shimmer effect so you can differentiate them easily from normal weapons. Standard weapons keep their vanilla appearance!

Buying weapons from shops does not grant tiers, only naturally dropped weapon pickups!

Melee weapons have not been tested at the time of writing, although they should function just fine!

## Tiers

| Tier | Damage | Fire rate | Reload speed | Max durability | Default chance |
|---|---:|---:|---:|---:|---:|
| Standard | x1 | x1 | x1 | x1 | 70% |
| Blessed | x1.25 | x1.05 | x1.05 | x1.10 | 20% |
| Hallowed | x1.50 | x1.10 | x1.10 | x1.20 | 7% |
| Sacred | x1.75 | x1.15 | x1.15 | x1.35 | 2.5% |
| Divine | x2 | x1.20 | x1.20 | x1.50 | 0.5% |

## Requirements/installation

Requires BepInEx 5

Install through your mod manager, or copy files to your plugins folder directly (`SULFUR/BepInEx/plugins/SulfurWeaponTiers/`)

## Config

The game must be launched first to generate the config. Use your mod manager's configuration editor or just open `BepInEx/config/pseudotim.sulfur.weapontiers.cfg` with any text editor!

```ini
[Drops]
Weights = 70,20,7,2.5,0.5

[Visuals]
GlintEnabled = true
GlintStrength = 0.39
GlintSpeed = 0.3
GlintIntensityScale = 0.5
```

- **Weights:** Relative chances for Standard, Blessed, Hallowed, Sacred, and Divine, in that order!
- **GlintEnabled:** Turns the colored shimmer on or off
- **GlintStrength:** Effect brightness
- **GlintSpeed:** Shimmer movement speed
- **GlintIntensityScale:** Overall glint intensity for pickups, held weapons, and inventory icons
