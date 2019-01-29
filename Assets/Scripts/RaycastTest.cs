using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastTest : MonoBehaviour
{
    /// <summary>
    /// 射线反射次数
    /// </summary>
    public int MAX_STEP = 1;

    /// <summary>
    /// 场景Mesh
    /// </summary>
    Mesh mesh;

    /// <summary>
    /// 相机角
    /// </summary>
    Vector3[] cameraCorn;
    /// <summary>
    /// 相机坐标
    /// </summary>
    Vector3 cameraPosition;

    public int screenWidth = 160;
    public int screenHeight = 90;


    //测试用颜色
    Color[] _c = new Color[] { Color.black, Color.red, Color.yellow, Color.green, Color.cyan };
    // Use this for initialization
    void Start()
    {
        SetUpScene();
        GetCameraData();

        StartCoroutine("Tracing");
    }

    IEnumerator Tracing()
    {
        int i = 0;
        Texture2D t2d = new Texture2D(screenWidth, screenHeight, TextureFormat.ARGB32, false);

        for (int x = 0; x < screenWidth; x++)
        {
            for (int y = 0; y < screenHeight; y++)
            {
                Ray _r = GetCurrentPixelRay(x, y);
                Color _c = RayTracing(_r);
                t2d.SetPixel(x, y, _c);
                i++;
                Debug.Log(i);
                yield return null;
            }
        }
        t2d.Apply();
        System.IO.File.WriteAllBytes("Assets/Save.png", t2d.EncodeToPNG());
        Debug.Log("Finish");
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

    /// <summary>
    /// 输入当前屏幕坐标，并获得射线
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    Ray GetCurrentPixelRay(float x, float y)
    {
        float h = x / (float)screenWidth;
        float v = y / (float)screenHeight;
        Vector3 _hd = Vector3.Lerp(cameraCorn[0], cameraCorn[3], h);
        Vector3 _ht = Vector3.Lerp(cameraCorn[1], cameraCorn[2], h);
        return new Ray(cameraPosition, Vector3.Lerp(_hd, _ht, v).normalized);
    }

    /// <summary>
    /// 计算光线追踪
    /// </summary>
    /// <param name="ray"></param>
    /// <returns></returns>
    Color RayTracing(Ray ray)
    {
        Ray _ray = new Ray(ray.origin, ray.direction);
        bool isCast = false;
        bool isCastPrv = true;
        bool isCastAnything = false;

        Color colorStart = Color.white;

        for (int step = 0; step < MAX_STEP; step++)
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
                    _ray,
                    mesh.vertices[cPoint_0],
                    mesh.vertices[cPoint_1],
                    mesh.vertices[cPoint_2],
                    ref t,
                    ref u,
                    ref v))
                {
                    //当目标位于正方向
                    if (t > 0)
                    {
                        if (t < _cDistance)
                        {
                            normal = (Vector3.Lerp(mesh.normals[cPoint_0], mesh.normals[cPoint_1], u) + Vector3.Lerp(mesh.normals[cPoint_0], mesh.normals[cPoint_2], v)) / 2;
                            castPoint = _ray.GetPoint(t) + normal * 1e-5f;              //不添加上偏移量会导致错误的cast     
                            isCast = true;
                            isCastAnything = true;
                            _cDistance = t;
                        }
                    }

                }
            }
            //如果射中东西，则更新ray的位置与方向，并衰减光照
            if (isCast)
            {
                //_ray.origin = castPoint;
                //_ray.direction = Vector3.Reflect(_ray.direction, normal).normalized;
                //colorStart *= 0.7f;
                colorStart.r = (normal.x + 1) / 2.0f;
                colorStart.g = (normal.y + 1) / 2.0f;
                colorStart.b = (normal.z + 1) / 2.0f;
            }

            isCastPrv = isCast;
        }

        if (isCastAnything)
        {
            colorStart.a = 1.0f;
            return colorStart;
        }
        else
        {
            return Color.black;
        }
    }
}
