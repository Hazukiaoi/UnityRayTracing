using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCS : MonoBehaviour {

    public Material mat;
    public ComputeShader shader;

    public Texture2D t2d;

	// Use this for initialization
	void Start () {
        RenderTexture rt = new RenderTexture(512, 512, 24);
        rt.enableRandomWrite = true;
        rt.Create();

        mat.SetTexture("_MainTex", rt);

        int kid = shader.FindKernel("CSMain");

        shader.SetTexture(kid, "Result", rt);
        shader.SetTexture(kid, "BaseTexture", t2d);
        shader.SetFloat("width", 512.0f);
        shader.SetFloat("height", 512.0f);

        shader.Dispatch(kid, rt.width / 8, rt.height / 8, 1);

	}
	
}
