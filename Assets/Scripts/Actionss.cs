using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Actionss : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        nyaa()();
    }

    Action nyaa()
    {
        Debug.Log("Nyaa");
        return nyaa2();
    }

    Action nyaa2()
    {
        Debug.Log("Nyaa2");
        return nyaa3;
    }

    void nyaa3()
    {
        Debug.Log("Nyaa3");
    }
}
