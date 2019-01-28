using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayUnit
{

    /// <summary>
    /// 射线与三角形求交
    /// </summary>
    /// <param name="ray">射线</param>
    /// <param name="v0">顶点1</param>
    /// <param name="v1">顶点2</param>
    /// <param name="v2">顶点3</param>
    /// <param name="t">射线从起点到交点的权重</param>
    /// <param name="u">U</param>
    /// <param name="v">V</param>
    /// <returns></returns>
    static public bool RayCast(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, ref float t, ref float u, ref float v)
    {
        Vector3 E1 = v1 - v0;
        Vector3 E2 = v2 - v0;
        Vector3 P = Vector3.Cross(ray.direction, E2);

        float det = Vector3.Dot(E1, P);

        Vector3 T;
        if (det > 0)
        {
            T = ray.origin - v0;
        }
        else
        {
            T = v0 - ray.origin;
            det = -det;
        }

        if (det < 0.0001f)
            return false;
        u = Vector3.Dot(T, P);
        if (u < 0.0f || u > det)
        {
            return false;
        }

        Vector3 Q = Vector3.Cross(T, E1);
        v = Vector3.Dot(ray.direction, Q);
        if (v < 0.0f || u + v > det)
            return false;

        t = Vector3.Dot(E2, Q);
        float fInvDet = 1.0f / det;
        t *= fInvDet;
        u *= fInvDet;
        v *= fInvDet;

        return true;
    }
}
