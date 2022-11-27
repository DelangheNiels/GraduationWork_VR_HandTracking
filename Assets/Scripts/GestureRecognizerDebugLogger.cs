using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureRecognizerDebugLogger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LogSpellDebugMessage(string message)
    {
        Debug.Log("Spell Debug Log message: " + message);
    }
}
