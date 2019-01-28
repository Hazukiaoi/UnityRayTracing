using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastTest : MonoBehaviour
{
    const int MAX_STEP = 10;

    Mesh mesh;

    List<Ray> rays;
    // Use this for initialization
    void Start()
    {

        mesh = new Mesh();
        //初始化场景
        MeshFilter[] _mfs = FindObjectsOfType<MeshFilter>();
        CombineInstance[] _cin = new CombineInstance[_mfs.Length];
        for(int i = 0; i < _mfs.Length; i++)
        {
            _cin[i].mesh = _mfs[i].mesh;
            _cin[i].transform = _mfs[i].transform.localToWorldMatrix;
        }
        mesh.CombineMeshes(_cin);

        rays = new List<Ray>();

        Debug.Log(mesh.triangles.Length);
    }	
	
    
	void Update ()
    {

        Ray startRay = new Ray(transform.position, transform.forward);
        rays.Add(startRay);
        bool isCast = false;
        bool isCastPrv = true;

        for(int step = 0; step < MAX_STEP; step++)
        {
            //如果上一帧没射中东西，则表明已经结束追踪
            if (!isCastPrv)
                return;

            float _cDistance = float.MaxValue;
            Vector3 castPoint = Vector3.zero;
            Vector3 normal = Vector3.zero;
            isCast = false;

            for (int i = 0; i < mesh.triangles.Length - 3; i = i + 3)
            {
                float t = float.MaxValue;
                float u = float.MaxValue;
                float v = float.MaxValue;

                int cPoint_0 = mesh.triangles[i];
                int cPoint_1 = mesh.triangles[i + 1];
                int cPoint_2 = mesh.triangles[i + 2];

                if (RayUnit.RayCast(
                    rays[step],
                    mesh.vertices[cPoint_0],
                    mesh.vertices[cPoint_1],
                    mesh.vertices[cPoint_2],
                    ref t,
                    ref u,
                    ref v))
                {
                    if (t < _cDistance)
                    {
                        isCast = true;
                        _cDistance = t;
                        castPoint = rays[step].GetPoint(t);
                        normal = (Vector3.Lerp(mesh.normals[cPoint_0], mesh.normals[cPoint_1], u) + Vector3.Lerp(mesh.normals[cPoint_0], mesh.normals[cPoint_2], v)) / 2;
                    }
                }
            }

            //如果射中东西，则把反射后的路径加入路径列表
            if (isCast)
            {
                if(step +1 < MAX_STEP)
                {
                    rays.Add(new Ray(castPoint, Vector3.Reflect(rays[step].direction, normal)));
                }
            }

            isCastPrv = isCast;
        }

        for(int i = 0;i < rays.Count - 1; i++)
        {
            Debug.DrawLine(rays[i].origin, rays[i + 1].origin, Color.red);
        }
	}
}
