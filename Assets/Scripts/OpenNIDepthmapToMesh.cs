
using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using OpenNI;

public class OpenNIDepthmapToMesh : MonoBehaviour
{
    public Vector3 gridScale = Vector3.one;
    public bool GenerateNormals = false;
    public bool GenerateUVs = true;
    public bool RealWorldPoints = true; // perform perspective transform on depth-map

    public Vector2 DesiredResolution = new Vector2(160, 120); // should be a divisor of 640x480
                                                              // and 320x240 is too high (too many vertices)
    int factorX;
    int factorY;

    short[] rawDepthMap;
    float[] depthHistogramMap;
    int XRes;
    int YRes;
    Mesh mesh;
    MeshFilter meshFilter;

    Vector2[] uvs;
    Vector3[] verts;
    int[] tris;
    Point3D[] pts;
    // Use this for initialization
    void Start()
    {
        // init stuff
        MapOutputMode mom = OpenNIContext.Instance.Depth.MapOutputMode;
        YRes = mom.YRes;
        XRes = mom.XRes;
        factorX = (int)(XRes / DesiredResolution.x);
        factorY = (int)(YRes / DesiredResolution.y);
        // depthmap data
        rawDepthMap = new short[(int)(mom.XRes * mom.YRes)];

        // the actual mesh we'll use
        
        mesh = new Mesh();

        meshFilter = (MeshFilter)GetComponent(typeof(MeshFilter));
        meshFilter.mesh = mesh;

        int YScaled = YRes / factorY;
        int XScaled = XRes / factorX;

        verts = new Vector3[XScaled * YScaled];
        uvs = new Vector2[verts.Length];
        tris = new int[(XScaled - 1) * (YScaled - 1) * 2 * 3];
        pts = new Point3D[XScaled * YScaled];
        CalculateTriangleIndices(YScaled, XScaled);
        CalculateUVs(YScaled, XScaled);
    }

    void UpdateDepthmapMesh()
    {
        if (meshFilter == null)
            return;
        Profiler.BeginSample("UpdateDepthmapMesh");
        mesh.Clear();
        
        // flip the depthmap as we create the texture
        int YScaled = YRes / factorY;
        int XScaled = XRes / factorX;
        // first stab, generate all vertices (next time, only vertices for 'valid' depths)
        // first stab, decimate rather than average depth pixels
        UpdateVertices(YScaled, XScaled);
        if (GenerateUVs) {
            UpdateUVs(YScaled, XScaled);
        }
        UpdateTriangleIndices();
        // normals - if we generate we need to update them according to the new mesh
        if (GenerateNormals) {
            mesh.RecalculateNormals();
        }

        Profiler.EndSample();
    }

    private void UpdateUVs(int YScaled, int XScaled)
    {
        Profiler.BeginSample("UpdateUVs");
        mesh.uv = uvs;
        Profiler.EndSample();
    }

    private void CalculateUVs(int YScaled, int XScaled)
    {
        for (int y = 0; y < YScaled; y++) {
            for (int x = 0; x < XScaled; x++) {
                //uvs[y * XScaled + x] = new Vector2((float)x / (float)XScaled,
                //                       (float)y / (float)YScaled);
                uvs[y * XScaled + x].x = (float)x / (float)XScaled;
                uvs[y * XScaled + x].y = ((float)(YScaled - 1 - y) / (float)YScaled);
            }
        }
    }
    
    private void UpdateVertices(int YScaled, int XScaled)
    {
        int depthIndex = 0;
        Profiler.BeginSample("UpdateVertices");

        Profiler.BeginSample("FillPoint3Ds");
        DepthGenerator dg = OpenNIContext.Instance.Depth;
        short maxDepth = (short)OpenNIContext.Instance.Depth.DeviceMaxDepth;
        Vector3 vec = new Vector3();
        Point3D pt = new Point3D();
        for (int y = 0; y < YScaled; y++) {
            for (int x = 0; x < XScaled; x++, depthIndex += factorX) {
                short pixel = rawDepthMap[depthIndex];
                if (pixel == 0) pixel = maxDepth; // if there's no depth,  default to max depth

                // RW coordinates
                pt.X = x * factorX;
                pt.Y = y * factorY;
                pt.Z = pixel;
                pts[x + y * XScaled] = pt; // in structs, assignment is a copy, so modifying the same variable
                                           // every iteration is okay
            }
            // Skip lines
            depthIndex += (factorY - 1) * XRes;
        }
        Profiler.EndSample();
        Profiler.BeginSample("ProjectiveToRW");
        if (RealWorldPoints) {
            pts = dg.ConvertProjectiveToRealWorld(pts);
        }
        else {
            for (int i = 0; i < pts.Length; i++) {
                pts[i].X -= XRes / 2;
                pts[i].Y = (YRes / 2) - pts[i].Y; // flip Y axis in projective
            }
        }
        Profiler.EndSample();
        Profiler.BeginSample("PointsToVertices");
        for (int y = 0; y < YScaled; y++) {
            for (int x = 0; x < XScaled; x++) {
                pt = pts[x + y * XScaled];
                vec.x = pt.X * gridScale.x;
                vec.y = pt.Y * gridScale.y;
                vec.z = -pt.Z * gridScale.z;
                verts[y * XScaled + x] = vec;
            }
        }
        Profiler.EndSample();
        Profiler.BeginSample("AssignVerticesToMesh");
        mesh.vertices = verts;
        Profiler.EndSample();

        Profiler.EndSample();
    }

    private void UpdateTriangleIndices()
    {
        Profiler.BeginSample("UpdateTriangleIndices");

        mesh.triangles = tris;
        Profiler.EndSample();
    }

    private void CalculateTriangleIndices(int YScaled, int XScaled)
    {
        int triIndex = 0;
        int posIndex = 0;
        for (int y = 0; y < (YScaled - 1); y++) {
            for (int x = 0; x < (XScaled - 1); x++, posIndex++) {
                // Counter-clockwise triangles

                tris[triIndex++] = posIndex + 1; // bottom right
                tris[triIndex++] = posIndex; // bottom left
                tris[triIndex++] = posIndex + XScaled; // top left

                tris[triIndex++] = posIndex + 1; // bottom right
                tris[triIndex++] = posIndex + XScaled; // top left
                tris[triIndex++] = posIndex + XScaled + 1; // top right
            }
            posIndex++; // finish row
        }
    }

    int lastFrameId;
    void FixedUpdate()
    {
        // update only if we have a new depth frame
        if (lastFrameId != OpenNIContext.Instance.Depth.FrameID) {
            lastFrameId = OpenNIContext.Instance.Depth.FrameID;
            Marshal.Copy(OpenNIContext.Instance.Depth.DepthMapPtr,
                         rawDepthMap, 0, rawDepthMap.Length);
            UpdateDepthmapMesh();
        }
    }
}
