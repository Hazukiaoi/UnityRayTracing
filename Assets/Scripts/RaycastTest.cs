using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastTest : MonoBehaviour
{
    const int MAX_STEP = 4;

    Mesh mesh;

    List<Ray> rays;

    Vector3[] cameraCorn;
    Vector3 cameraPosition;

    [Range(0,1)]
    public float h = 0;

    [Range(0, 1)]
    public float v = 0;
    // Use this for initialization
    void Start()
    {
        SetUpScene();
        GetCameraData();
    
        rays = new List<Ray>();

    }

    /// <summary>
    /// 初始化场景
    /// </summary>
    void SetUpScene()
    {
        mesh = new Mesh();
        //初始化场景
        MeshFilter[] _mfs = FindObjectsOfType<MeshFilter>();
        CombineInstance[] _cin = new CombineInstance[_mfs.Length];
        for (int i = 0; i < _mfs.Length; i++)
        {
            _cin[i].mesh = _mfs[i].mesh;
            _cin[i].transform = _mfs[i].transform.localToWorldMatrix;
        }
        mesh.CombineMeshes(_cin);
    }

    /// <summary>
    /// 初始化相机
    /// </summary>
    void GetCameraData()
    {
        Camera cam = Camera.main;
        cameraCorn = new Vector3[4];
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, cameraCorn);    
        cameraPosition = cam.transform.position;
        for (int i = 0; i < 4; i++)
        {
            cameraCorn[i] = cam.transform.localToWorldMatrix * cameraCorn[i];
        }
    }

    Color[] _c = new Color[] { Color.black, Color.red, Color.yellow, Color.green, Color.cyan };

    void Update ()
    {

        for (int i = 0;i < 4; i++)
        {
            Debug.DrawRay(cameraPosition, cameraCorn[i], Color.blue);

        }

        Vector3 _hd = Vector3.Lerp(cameraCorn[0], cameraCorn[3], h);
        Vector3 _ht = Vector3.Lerp(cameraCorn[1], cameraCorn[2], h);
        Vector3 _c = Vector3.Lerp(_hd, _ht, v);

        Debug.DrawRay(cameraPosition, _c.normalized, Color.red);

        return;

        rays.Clear();
        Ray startRay = new Ray(transform.position, transform.forward);
        rays.Add(startRay);
        bool isCast = false;
        bool isCastPrv = true;
        
        for(int step = 0; step < MAX_STEP; step++)
        {
            //如果上一帧没射中东西，则表明已经结束追踪
            if (!isCastPrv)
                break;

            float _cDistance = float.MaxValue;
            Vector3 castPoint = Vector3.zero;
            Vector3 normal = Vector3.zero;
            isCast = false;

            //找到射线正方向上最近的一个点
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
                    //当目标位于正方向
                    if(t > 0)
                    {
                        if (t < _cDistance)
                        {
                            normal = (Vector3.Lerp(mesh.normals[cPoint_0], mesh.normals[cPoint_1], u) + Vector3.Lerp(mesh.normals[cPoint_0], mesh.normals[cPoint_2], v)) / 2;
                            castPoint = rays[step].GetPoint(t) + normal * 1e-5f;              //不添加上偏移量会导致错误的cast     
                            isCast = true;
                            _cDistance = t;
                        }
                    }

                }
            }

            //如果射中东西，则把反射后的路径加入路径列表
            if (isCast)
            {
                if(step +1 < MAX_STEP)
                {
                    rays.Add(new Ray(castPoint, Vector3.Reflect(rays[step].direction, normal).normalized));
                    //Debug.DrawRay(castPoint, normal, Color.blue);
                }
            }

            isCastPrv = isCast;
        }

        //for(int i = 0;i < rays.Count ; i++)
        //{
        //    Debug.DrawRay(rays[i].origin, rays[i].direction * 5, _c[i]);
        //}
	}
}
