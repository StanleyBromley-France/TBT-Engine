using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Abilities.Targeting;

/// Identifies the kinds of targets an ability can select, such as its user,
/// allies, enemies, or an area on the map.
/// </summary>
public enum TargetType
{
    Self,
    Ally,
    Enemy,
    Area
}
