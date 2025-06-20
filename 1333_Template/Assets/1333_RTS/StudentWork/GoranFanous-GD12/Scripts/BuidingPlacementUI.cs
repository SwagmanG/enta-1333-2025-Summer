using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuidingPlacementUI : MonoBehaviour
{



    [SerializeField] private RectTransform LayoutGroupParent;

    [SerializeField] private SelectBuildingButton ButtonPrefab;
        
    [SerializeField] private BuildingTypesSo BuildingData;

    // Start is called before the first frame update
    void Start()
    {
        foreach (BuildingData t in BuildingData.Buildings)
        {
            SelectBuildingButton button = Instantiate(ButtonPrefab, LayoutGroupParent);
            button.setup(t);
        }
    }

    
}
