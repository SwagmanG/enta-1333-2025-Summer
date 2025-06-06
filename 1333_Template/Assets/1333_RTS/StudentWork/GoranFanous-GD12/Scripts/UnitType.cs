using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;

namespace RTS_1333
{
    /// <summary>
    /// A ScriptableObject that defines the stats and characteristics of a unit type.
    /// This is used to configure various unit prefabs like infantry, archers, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitType", menuName = "Game/Unit Type")]
    public class UnitType : ScriptableObject
    {
        /// <summary>
        /// The width of the unit in grid cells. This determines how many horizontal cells the unit occupies.
        /// </summary>
        [SerializeField] private int _width = 1;

        /// <summary>
        /// The height of the unit in grid cells. This determines how many vertical cells the unit occupies.
        /// </summary>
        [SerializeField] private int _height = 1;

        /// <summary>
        /// The maximum health points of the unit.
        /// </summary>
        [SerializeField] private int _maxHp = 1;

        /// <summary>
        /// The movement speed of the unit. Affects how fast the unit moves across the grid.
        /// </summary>
        [SerializeField] private float _moveSpeed = 1;

        /// <summary>
        /// The amount of damage the unit deals to enemies.
        /// </summary>
        [SerializeField] private int _damage = 1;

        /// <summary>
        /// The amount of defense the unit has to reduce incoming damage.
        /// </summary>
        [SerializeField] private int _defence = 1;

        /// <summary>
        /// The type of attack this unit uses (e.g., Melee, Ranged, etc.).
        /// </summary>
        [SerializeField] private AttackType _attackType = AttackType.Melee;

        /// <summary>
        /// The range at which this unit can attack. For melee units, this is usually 1.
        /// </summary>
        [SerializeField] private int _range = 1;

        /// <summary>
        /// Public read-only property to access the unit's width.
        /// </summary>
        public int Width => _width;

        /// <summary>
        /// Public read-only property to access the unit's height.
        /// </summary>
        public int Height => _height;
    }
}

