using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectBuildingButton : MonoBehaviour
{
    [SerializeField] private Image _buttonImage;
    [SerializeField] private TMP_Text _buttontext;
    [SerializeField] private Button _button;

    private BuildingData _buildingDataForButton;

    public void setup(BuildingData buildingData)
    {
        _buildingDataForButton = buildingData;
        _buttontext.text = _buildingDataForButton.BuildingName;
        //optional
        _buttonImage.sprite = _buildingDataForButton.Icon;
    }
}
