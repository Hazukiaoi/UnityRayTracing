using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCast : MonoBehaviour {

    const int MAX_STEP = 10;

    Mesh mesh;

    List<Ray> rays;

    // Use this for initialization
    void Start () {
        //初始化场景
        MeshFilter[] _mfs = FindObjectsOfType<MeshFilter>();
        CombineInstance[] _cin = new CombineInstance[_mfs.Length];
        for (int i = 0; i < _mfs.Length; i++)
        {
            _cin[i].mesh = _mfs[i].mesh;
            _cin[i].transform = _mfs[i].transform.localToWorldMatrix;
        }
        mesh.CombineMeshes(_cin);

        rays = new List<Ray>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
