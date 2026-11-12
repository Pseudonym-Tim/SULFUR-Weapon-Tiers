#ifndef WEAPON_TIERS_ENCHANTMENT_GLINT
#define WEAPON_TIERS_ENCHANTMENT_GLINT

float EnchantmentGlint(float2 uv, float time, float width)
{
    // Keep faint tint between passes, with just one brighter moving band...
    float center = lerp(-1.5, 1.5, frac(time));
    float distance = abs(uv.y - uv.x * 0.5 - center);
    float band = 1.0 - smoothstep(width * 0.15625, width, distance);
    return 0.375 + band * 0.625;
}

#endif