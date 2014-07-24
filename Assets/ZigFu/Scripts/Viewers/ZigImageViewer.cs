using UnityEngine;
using System.Collections;


public class ZigImageViewer : MonoBehaviour
{
    const string ClassName = "TexturedFaceMesh";


    public Renderer target;
    public ZigResolution TextureSize = ZigResolution.QQVGA_160x120;
    public bool mirrored = true;
    public bool verbose = true;


    Texture2D _texture;
    ResolutionData _textureSize;
    Color32[] _outputPixels;


    void Start()
    {
        LogStatus("Start");

        if (target == null) { target = renderer; }

        InitTexture(TextureSize);

        ZigInput.Instance.AddListener(gameObject);
    }

    void InitTexture(ZigResolution size)
    {
        LogStatus("InitTexture");

        _textureSize = ResolutionData.FromZigResolution(size);

        int w = _textureSize.Width;
        int h = _textureSize.Height;

        _texture = new Texture2D(w, h);
        _texture.wrapMode = TextureWrapMode.Clamp;
        renderer.material.mainTexture = _texture;

        _outputPixels = new Color32[w * h];
    }


    void Zig_Update(ZigInput input)
    {
        UpdateTexture(ZigInput.Image);
    }

    void UpdateTexture(ZigImage image)
    {
        Color32[] rawImageMap = image.data;

        int w = (int)_textureSize.Width;
        int h = (int)_textureSize.Height;
        int srcIndex = 0;
        int factorX = image.xres / w;
        int factorY = ((image.yres / h) - 1) * image.xres;

        // invert Y axis while doing the update
        for (int y = h - 1; y >= 0; --y, srcIndex += factorY)
        {
            int outputIndex = y * w;
            for (int x = 0; x < w; ++x, srcIndex += factorX, ++outputIndex)
            {
                if (!mirrored) { outputIndex = y * w + w - x - 1; }
                _outputPixels[outputIndex] = rawImageMap[srcIndex];
            }
        }

        _texture.SetPixels32(_outputPixels);
        _texture.Apply();
    }


    void LogStatus(string msg)
    {
        if (!verbose) { return; }
        Debug.Log(ClassName + ":: " + msg);
    }
}

