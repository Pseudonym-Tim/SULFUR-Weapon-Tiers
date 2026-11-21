
<img width="256" height="256" alt="icon" src="https://github.com/user-attachments/assets/bca47f01-a5dc-4f9d-a297-855e1a130c72" />

# Weapon Tiers
A mod for SULFUR that adds 4 new weapon tiers that can randomly roll on weapons, granting them multipliers for various weapon stats!

<img width="394" height="546" alt="image" src="https://github.com/user-attachments/assets/42c62cd4-3c9a-42b3-8c8f-4004dc4c33c8" />

## Tiers

| Tier | Damage (Firearms and melee) | Fire rate (Firearms) | Reload speed (Firearms) | Max durability (Firearms) | Default chance |
|---|---:|---:|---:|---:|---:|
| Standard (Normal drops) | x1 | x1 | x1 | x1 | 70% |
| Blessed | x1.25 | x1.05 | x1.05 | x1.10 | 20% |
| Hallowed | x1.50 | x1.10 | x1.10 | x1.20 | 7% |
| Sacred | x1.75 | x1.15 | x1.15 | x1.35 | 2.5% |
| Holy | x2 | x1.20 | x1.20 | x1.50 | 0.5% |

The tier system does NOT apply to throwables! 

The tier system does apply to melee weapons, but they only receive the damage bonus! 

Tiered weapons have colored tier names, tooltip bonuses, and an animated shimmer effect so you can differentiate them easily from normal weapons. Standard weapons keep their vanilla appearance!

Weapons from treasure chests, naturally dropped weapon pickups, and weapons in shops can all roll tiers!

Tiered weapon prices increase by 10% per tier

## Sulfurium Tincture

<img width="347" height="207" alt="image" src="https://github.com/user-attachments/assets/979db482-76a2-4a82-892a-5957443647e4" />

The Sulfurium Tincture is a new consumable item added by the mod that upgrades a weapon by one tier.
It can be purchased from Scholar and Fex Shuen for 7,500 SULF, and applied by dragging it onto a weapon.

## Requirements/installation

Requires BepInEx 5

Install through r2modman or another compatible mod manager, or copy files to your plugins folder directly (`SULFUR/BepInEx/plugins/SulfurWeaponTiers/`)

## Config

The game must be launched first to generate the config. Use your mod manager's configuration editor or just open `BepInEx/config/pseudotim.sulfur.weapontiers.cfg` with any text editor!

```ini
[Drops]
Weights = 70,20,7,2.5,0.5

[UpgradeItem]
Price = 7500

[TiersBlessed]
Damage = 1.25
Price = 1.10
FireRate = 1.05
ReloadSpeed = 1.05
MaxDurability = 1.10

[TiersHallowed]
Damage = 1.50
Price = 1.20
FireRate = 1.10
ReloadSpeed = 1.10
MaxDurability = 1.20

[TiersSacred]
Damage = 1.75
Price = 1.30
FireRate = 1.15
ReloadSpeed = 1.15
MaxDurability = 1.35

[TiersHoly]
Damage = 2
Price = 1.40
FireRate = 1.20
ReloadSpeed = 1.20
MaxDurability = 1.50

[Visuals]
GlintEnabled = true
GlintStrength = 0.39
GlintSpeed = 0.3
GlintIntensityScale = 0.5
```

- **Weights:** Relative chances for Standard, Blessed, Hallowed, Sacred, and Divine, in that order!
- **TiersBlessed/TiersHallowed/TiersSacred/TiersHoly:** (Set each tier's multipliers, such as damage, durability, and price of the weapon)
- **UpgradeItem Price:** Base coin price of the Sulfurium Tincture
- **GlintEnabled:** Turns the colored shimmer on or off
- **GlintStrength:** Effect brightness
- **GlintSpeed:** Shimmer movement speed
- **GlintIntensityScale:** Overall glint intensity for pickups, held weapons, and inventory icons
