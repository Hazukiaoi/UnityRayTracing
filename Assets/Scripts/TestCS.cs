using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCS : MonoBehaviour
{

    struct VertAndNormal
    {
        public Vector3 vertices;
        public Vector3 normlas;
    }

    //public Material mat;
    public ComputeShader shader;

    public Texture2D t2d;

    public RenderTexture renderTexture;
    /// <summary>
    /// 射线反射次数
    /// </summary>
    public int MAX_STEP = 1;
    public int MAX_SAMPLE = 20;

    /// <summary>
    /// 场景Mesh
    /// </summary>
    Mesh mesh;

    /// <summary>
    /// 相机角
    /// </summary>
    Vector4[] cameraCorn;
    /// <summary>
    /// 相机坐标
    /// </summary>
    Vector3 cameraPosition;

    public int screenWidth = 512;
    public int screenHeight = 512;


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

        for (int i = 0; i < _mfs.Length; i++)
        {
            _mfs[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 初始化相机
    /// </summary>
    void GetCameraData()
    {
        Camera cam = Camera.main;
        Vector3[] _cameraCorn = new Vector3[4];
        cameraCorn = new Vector4[4];
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, _cameraCorn);
        cameraPosition = cam.transform.position;
        for (int i = 0; i < 4; i++)
        {
            _cameraCorn[i] = cam.transform.localToWorldMatrix * _cameraCorn[i];
            cameraCorn[i] = new Vector4(_cameraCorn[i].x, _cameraCorn[i].y, _cameraCorn[i].z);
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
        return new Ray(cameraPosition, Vector3.Lerp(_hd, _ht, v));
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
                _ray.origin = castPoint;
                _ray.direction = Vector3.Reflect(_ray.direction, normal).normalized;
                colorStart *= 0.5f;
                //colorStart.r = (normal.x + 1) / 2.0f;
                //colorStart.g = (normal.y + 1) / 2.0f;
                //colorStart.b = (normal.z + 1) / 2.0f;
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


    //测试数据
    Vector3[] perDirData;
    ComputeBuffer perDir;

    // Use this for initialization
    private void Start()
    {
        SetUpScene();
        GetCameraData();

        StartCoroutine("OutputPerDirect");

    }

    IEnumerator OutputPerDirect()
    {

        renderTexture = new RenderTexture(screenWidth, screenWidth, 24);
        renderTexture.enableRandomWrite = true;//允许随机写入
        renderTexture.Create();

        int kid = shader.FindKernel("CSMain");

        //传递基础数据
        shader.SetTexture(kid, "Result", renderTexture);
        shader.SetFloat("width", screenWidth);
        shader.SetFloat("height", screenHeight);

        //设置相机数据
        shader.SetVectorArray("camCorn", cameraCorn);
        shader.SetVector("camPos", cameraPosition);

        //设置光追数据
        shader.SetInt("max_step", MAX_STEP);
        shader.SetInt("max_sample", MAX_SAMPLE);

        yield return null;

        //传Mesh数据
        shader.SetInt("vertexCount", mesh.vertexCount);
        shader.SetInt("trianglesCount", mesh.triangles.Length);
        ComputeBuffer tris = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        tris.SetData(mesh.triangles);

        ComputeBuffer vAndN = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 6);
        List<VertAndNormal> _vAndNData = new List<VertAndNormal>();
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            _vAndNData.Add(new VertAndNormal()
            {
                vertices = mesh.vertices[i],
                normlas = mesh.normals[i]
            });
        }
        vAndN.SetData(_vAndNData);

        shader.SetBuffer(kid, "triangles", tris);
        shader.SetBuffer(kid, "vertAndNormal", vAndN);
        yield return null;

        //执行
        shader.Dispatch(kid, renderTexture.width / 8, renderTexture.height / 8, 1);
    }


    Rect rect = new Rect(0, 0, 256, 256);
    private void OnGUI()
    {
        if(renderTexture)
            GUI.Box(rect, renderTexture);
    }
}