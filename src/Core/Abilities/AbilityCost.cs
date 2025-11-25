using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Abilities;

/// <summary>
/// Represents the resource cost required to activate an ability
/// </summary>
public sealed class AbilityCost
{
    public int Mana { get; }

    public AbilityCost(int mana)
    {
        Mana = mana;
    }
}

