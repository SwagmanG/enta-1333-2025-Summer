using UnityEngine;

namespace RTS_1333
{
    [CreateAssetMenu(fileName = "UnitType", menuName = "Game/Unit Type")]
    public class UnitType : ScriptableObject
    {
        [SerializeField] private int _width = 1;
        [SerializeField] private int _height = 1;
        [SerializeField] private int _maxHp = 1;
        [SerializeField] private float _moveSpeed = 1;
        [SerializeField] private int _damage = 1;
        [SerializeField] private int _defence = 1;
        [SerializeField] private AttackType _attackType = AttackType.TowerBreaker;
        [SerializeField] private int _range = 1;
        [SerializeField] private float _attackSpeed = 1f;

        public int Width => _width;
        public int Height => _height;
        public int MaxHp => _maxHp;
        public float MoveSpeed => _moveSpeed;
        public int Damage => _damage;
        public int Defence => _defence;
        public int Range => _range;
        public float AttackSpeed => _attackSpeed;
        public AttackType AttackType => _attackType;
    }
}