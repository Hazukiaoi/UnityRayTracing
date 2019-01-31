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
    public ComputeShader RaytracingShader;
    public ComputeShader texCombineShader;

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
    /// 单一块的长宽高
    /// </summary>
    public int tileSize = 64;

    /// <summary>
    /// 长和宽各需要多少个块
    /// </summary>
    public Vector2Int tileCount;

    void RenderTileSetUp()
    {
        tileCount = new Vector2Int();
        tileCount.x = screenWidth % tileSize > 0 ? screenWidth / tileSize + 1 : screenWidth / tileSize;
        tileCount.y = screenHeight % tileSize > 0 ? screenHeight / tileSize + 1 : screenHeight / tileSize;
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
   

    //测试数据
    Vector3[] perDirData;
    ComputeBuffer perDir;

    // Use this for initialization
    private void Start()
    {
        SetUpScene();
        GetCameraData();
        RenderTileSetUp();

        StartCoroutine("OutputPerDirect");

    }

    IEnumerator OutputPerDirect()
    {

        renderTexture = new RenderTexture(screenWidth, screenHeight, 24);
        renderTexture.enableRandomWrite = true;//允许随机写入
        renderTexture.Create();

        int kid = RaytracingShader.FindKernel("CSMain");
        int cid = texCombineShader.FindKernel("CSMain");

        //传递基础数据
        //shader.SetTexture(kid, "Result", renderTexture);
        RaytracingShader.SetFloat("width", screenWidth);
        RaytracingShader.SetFloat("height", screenHeight);

        //设置相机数据
        RaytracingShader.SetVectorArray("camCorn", cameraCorn);
        RaytracingShader.SetVector("camPos", cameraPosition);

        //设置光追数据
        RaytracingShader.SetInt("max_step", MAX_STEP);
        RaytracingShader.SetInt("max_sample", MAX_SAMPLE);


        //传Mesh数据
        RaytracingShader.SetInt("vertexCount", mesh.vertexCount);
        RaytracingShader.SetInt("trianglesCount", mesh.triangles.Length);
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

        RaytracingShader.SetBuffer(kid, "triangles", tris);
        RaytracingShader.SetBuffer(kid, "vertAndNormal", vAndN);

        //自动分割Tile
        //执行
        for (int x = 0; x < tileCount.x; x++)
        {
            for (int y = 0; y < tileCount.y; y++)
            {
                Vector4 tileInfo = new Vector4(tileSize, tileSize, x, y);

                RenderTexture rt = RenderTexture.GetTemporary(tileSize, tileSize, 24);
                rt.enableRandomWrite = true;
                rt.Create();
                RaytracingShader.SetTexture(kid, "Result", rt);
                RaytracingShader.SetVector("tile", tileInfo);
                RaytracingShader.Dispatch(kid, tileSize / 8, tileSize / 8, 1);


                texCombineShader.SetTexture(cid, "Result", renderTexture);
                texCombineShader.SetTexture(cid, "Tile", rt);
                texCombineShader.SetInt("offsetU", tileSize * x);
                texCombineShader.SetInt("offsetV", tileSize * y);

                texCombineShader.Dispatch(cid, tileSize / 8, tileSize / 8, 1);
                rt.Release();
                yield return null;
            }
        }

        //保存文件
        RenderTexture _crt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D save = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        save.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        save.Apply();
        System.IO.File.WriteAllBytes("Assets/Save.png", save.EncodeToPNG());
    }


    private void OnGUI()
    {
        if(renderTexture)
            GUI.Box(new Rect(0,0,Screen.width, Screen.height), renderTexture);
    }
}