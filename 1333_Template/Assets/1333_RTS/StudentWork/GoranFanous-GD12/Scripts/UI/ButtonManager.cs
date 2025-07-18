using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    public void OnButtonClicked()
    {
        AudioManager.Instance?.PlaySFX("Enter/Exit Buildmode");
    }

   
}
