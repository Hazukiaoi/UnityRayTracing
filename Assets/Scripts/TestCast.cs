using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCast : MonoBehaviour {

    Ray rays;
    Ray raysRef;

    public Transform normal;
    public Transform Ins;
    // Use this for initialization
    void Start () {
        rays = new Ray();
        raysRef = new Ray();
    }
	
	// Update is called once per frame
	void Update () {
        rays.origin = Ins.position;
        rays.direction = Ins.forward;


        raysRef.origin = normal.position;
        raysRef.direction = Vector3.Reflect(rays.direction, normal.up);

        Debug.DrawRay(rays.origin, rays.direction * 10, Color.red);
        Debug.DrawRay(raysRef.origin, raysRef.direction * 10, Color.blue);
    }
}
