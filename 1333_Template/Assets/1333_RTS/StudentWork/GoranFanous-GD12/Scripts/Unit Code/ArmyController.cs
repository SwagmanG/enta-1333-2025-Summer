using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Defines the different army factions in the game
public enum ArmyType
{
    Player, // The player's army units
    Enemy   // Enemy army units
}

public class ArmyController : MonoBehaviour
{
    // List storing all units that belong to the player army
    public List<UnitController> PlayerArmy = new List<UnitController>();

    // List storing all units that belong to the enemy army
    public List<UnitController> EnemyArmy = new List<UnitController>();

    /// <summary>
    /// Adds the given unit to the appropriate army list based on its army type.
    /// </summary>
    /// <param name="unit">The unit to register.</param>
    /// <param name="armyType">The army type of the unit (Player or Enemy).</param>
    public void RegisterUnit(UnitController unit, ArmyType armyType)
    {
        if (armyType == ArmyType.Player)
        {
            PlayerArmy.Add(unit);
        }
        else if (armyType == ArmyType.Enemy)
        {
            EnemyArmy.Add(unit);
        }
    }

    /// <summary>
    /// Removes the given unit from both player and enemy army lists.
    /// Only one of these lists should contain the unit.
    /// </summary>
    /// <param name="unit">The unit to remove.</param>
    public void RemoveUnit(UnitController unit)
    {
        PlayerArmy.Remove(unit);
        EnemyArmy.Remove(unit);
    }

    /// <summary>
    /// Clears all units from both the player and enemy army lists.
    /// </summary>
    public void ClearArmies()
    {
        PlayerArmy.Clear();
        EnemyArmy.Clear();
    }
}
