using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerCheck : MonoBehaviour
{
    public delegate void EnteredTrig(Collider2D collider);
    public event EnteredTrig EnteredEvent;
    public delegate void ExitedTrig(Collider2D collider);
    public event ExitedTrig ExitedEvent;

    void OnTriggerEnter2D (Collider2D coll)
    {  
        EnteredEvent(coll);
    }
    void OnTriggerExit2D (Collider2D coll)
    {
        ExitedEvent(coll);
    }
}
