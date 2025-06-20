using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Defines types of armies in the game
public enum ArmyType
{
    Player,  // Represents the player's units
    Enemy    // Represents enemy units
}

public class ArmyController : MonoBehaviour
{
    // List of units belonging to the player army
    public List<UnitController> PlayerArmy = new List<UnitController>();

    // List of units belonging to the enemy army
    public List<UnitController> EnemyArmy = new List<UnitController>();

    // Registers a unit to the appropriate army list based on its type
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

    // Removes a unit from both army lists (only one list will contain the unit)
    public void RemoveUnit(UnitController unit)
    {
        PlayerArmy.Remove(unit);
        EnemyArmy.Remove(unit);
    }

    // Clears both army lists, removing all registered units
    public void ClearArmies()
    {
        PlayerArmy.Clear();
        EnemyArmy.Clear();
    }
}