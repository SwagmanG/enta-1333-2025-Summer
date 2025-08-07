using UnityEngine;

namespace RTS_1333
{
    [CreateAssetMenu(fileName = "UnitType", menuName = "Game/Unit Type")]
    public class UnitType : ScriptableObject
    {
        //Settings data
        [SerializeField] private int width = 1;
        [SerializeField] private int height = 1;
        [SerializeField] private int maxHp = 1;
        [SerializeField] private float moveSpeed = 1;
        [SerializeField] private int damage = 1;
        [SerializeField] private int defence = 1;
        [SerializeField] private AttackType attackType = AttackType.TowerBreaker;
        [SerializeField] private int range = 1;
        [SerializeField] private float attackSpeed = 1f;

        //Exposing Variables.
       
        public int MaxHp => maxHp;
        public float MoveSpeed => moveSpeed;
        public int Damage => damage;
        public int Range => range;
        public float AttackSpeed => attackSpeed;
        public AttackType AttackType => attackType;
    }
}