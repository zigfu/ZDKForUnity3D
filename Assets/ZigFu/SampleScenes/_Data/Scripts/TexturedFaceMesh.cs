using UnityEngine;
using System.Collections.Generic;

using Zigfu.FaceTracking;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]

public class TexturedFaceMesh : MonoBehaviour
{
    Mesh _mesh;
    FaceTriangle[] _triangleIndices;
    Texture2D _texture;
    Color32[] _outputPixels;


    #region Init and Destroy

    void Start()
    {
        ZigInput.Instance.AddListener(gameObject);
    }

    void InitTexture(ZigImage image)
    {
        int w = (int)image.xres;
        int h = (int)image.yres;

        _texture = new Texture2D(w, h);
        renderer.material.mainTexture = _texture;

        _outputPixels = new Color32[w * h];
    }

    void InitMesh(ZigFaceTrackFrame faceTrackingFrame)
    {
        _mesh = GetComponent<MeshFilter>().mesh;
        _mesh.Clear();

        int vertexCount = faceTrackingFrame.Get3DShape().Count;

        // Vertices and Texture Coordiantes
        _mesh.vertices = new Vector3[vertexCount];
        _mesh.uv = new Vector2[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            _mesh.vertices[i] = new Vector3();
            _mesh.uv[i] = new Vector2();
        }


        // Triangles
        _triangleIndices = faceTrackingFrame.GetTriangles();
        var indices = new List<int>(_triangleIndices.Length * 3);
        foreach (FaceTriangle triangle in _triangleIndices)
        {
            indices.Add(triangle.Third);
            indices.Add(triangle.Second);
            indices.Add(triangle.First);
        }
        _mesh.triangles = indices.ToArray();

        // Normals
        _mesh.RecalculateNormals();
    }

    #endregion


    #region Update

    void Zig_Update(ZigInput input)
    {
        UpdateTexture(ZigInput.Image);
        UpdateMesh(ZigFaceTracker.Instance.FaceTrackFrame);
    }

    void UpdateTexture(ZigImage image)
    {
        if (!_texture) { InitTexture(image); }

        BGRA_2_RGBA(image.data, _outputPixels);

        _texture.SetPixels32(_outputPixels);
        _texture.Apply();
    }

    void BGRA_2_RGBA(Color32[] inColors, Color32[] outColors)
    {
        Color32 c;
        for (int i = 0; i < outColors.Length; i++)
        {
            c = inColors[i];
            outColors[i] = new Color32(c.b, c.g, c.r, 1);
        }
    }

    void UpdateMesh(ZigFaceTrackFrame faceTrackingFrame)
    {
        if (!_mesh) { InitMesh(faceTrackingFrame); }

        EnumIndexableCollection<FeaturePoint, Vector3DF> shapePoints = faceTrackingFrame.Get3DShape();
        EnumIndexableCollection<FeaturePoint, PointF> projectedShapePoints = faceTrackingFrame.GetProjected3DShape();

        Vector3[] vertices = _mesh.vertices;
        Vector2[] uv = _mesh.uv;

        ZigFaceTransform faceTrans = ZigFaceTracker.Instance.FaceTransform;
        Vector3 facePosition = faceTrans.position;
        Quaternion faceRotationInv = Quaternion.Inverse(Quaternion.Euler(faceTrans.eulerAngles));

        // Update the 3D model's vertices and texture coordinates
        for (int i = 0; i < shapePoints.Count; i++)
        {
            // --- Vertices ---
            Vector3DF pt = shapePoints[i];
            Vector3 vert = new Vector3(pt.X, pt.Y, -pt.Z);

            // The retrieved shapePoints already have a transform applied to them, 
            //  so we undo it here, thus centering the mesh at gameObject's origin.
            vert -= facePosition;
            vert = faceRotationInv * vert;

            vertices[i] = vert;


            // --- Texture Coordinates ---
            PointF projected = projectedShapePoints[i];

            uv[i] =
                new Vector2(
                    projected.X / _texture.width,
                    projected.Y / _texture.height);
        }

        _mesh.vertices = vertices;
        _mesh.uv = uv;

        _mesh.RecalculateNormals();
    }

    #endregion

}
