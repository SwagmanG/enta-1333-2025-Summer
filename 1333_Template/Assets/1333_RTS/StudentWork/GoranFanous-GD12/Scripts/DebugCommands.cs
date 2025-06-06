using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine;

public class DebugCommands : MonoBehaviour
{
    private void HelloWorld()
    {
        Debug.Log("Hello World");
    }

    private void OnEnable()
    {
        DebugLogConsole.AddCommand("HelloWorld", "Prints a message to the console", HelloWorld);
    }

}
 
