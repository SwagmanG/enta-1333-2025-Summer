using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTS_1333 
{
    public abstract class UnitBase : MonoBehaviour
    {
        // Start is called before the first frame update
        [SerializeField] protected UnitType _unitType;
        public virtual int Width => _unitType != null ? _unitType.Width : 1;
        public virtual int Height => _unitType != null ? _unitType.Height : 1;

        public abstract void MoveTo(GridNode targetNode);

    }

}
