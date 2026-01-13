using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Domain.Abilities;

/// <summary>
/// Categorizes abilities by their general purpose or behavior
/// </summary>
public enum AbilityCategory
{
    MeleeAttack,
    RangedAttack,
    OffensiveSpell,
    DefensiveSpell,
    Utility
}

