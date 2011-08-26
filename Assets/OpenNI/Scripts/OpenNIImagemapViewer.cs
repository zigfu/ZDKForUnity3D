using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using OpenNI;
using Tao.OpenGl;

public class OpenNIImagemapViewer : MonoBehaviour 
{
    public Renderer target;
	private ImageGenerator Image;
	private Texture2D imageMapTexture;
	private int imageLastFrameId;
    byte[] rawImageMap;
    Color32[] imageMapPixels;
    Vector2 inputSize;
    Vector2 outputSize;
    bool openGl;

	// Use this for initialization
	void Start () 
	{
        if (null == target) {
            target = renderer;
        }

		Image = OpenNIContext.OpenNode(NodeType.Image) as ImageGenerator;
        inputSize = new Vector2(Image.MapOutputMode.XRes, Image.MapOutputMode.YRes);
        outputSize = (null != target) ?
            new Vector2(Mathf.NextPowerOfTwo((int)inputSize.x), Mathf.NextPowerOfTwo((int)inputSize.y)) :
            new Vector2(inputSize.x, inputSize.y);

		imageMapTexture = new Texture2D((int)outputSize.x, (int)outputSize.y, TextureFormat.RGB24, false);
        rawImageMap = new byte[Image.GetMetaData().BytesPerPixel * Image.MapOutputMode.XRes * Image.MapOutputMode.YRes];
        imageMapPixels = new Color32[(int)(outputSize.x * outputSize.y)];

        if (null != target) {
		    float uScale = inputSize.x / outputSize.x;
            float vScale = inputSize.y / outputSize.y;
            target.material.SetTextureScale("_MainTex", new Vector2(uScale, -vScale));
            target.material.SetTextureOffset("_MainTex", new Vector2(0.0f, vScale - 1.0f));
            target.material.mainTexture = imageMapTexture;
        }

        openGl = SystemInfo.graphicsDeviceVersion.Contains("OpenGL");
        if (openGl) {
            print("Running in OpenGL mode");
        }
	}
	
	void FixedUpdate () {
		// see if we have a new RGB frame
		if (Image.FrameID != imageLastFrameId) {
			imageLastFrameId = Image.FrameID;
			WriteImageToTexture(imageMapTexture);
		}
	}

    void WriteImageToTexture(Texture2D tex)
    {
        // NOTE: This method only works when unity is rendering with OpenGL ("unity.exe -force-opengl"). This is *much* faster
        // then Texture2D::SetPixels32 which we would have to use otherwise
        // NOTE: The native texture id needs a +1 if we are rendering to GUI
        if (openGl) {
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, (null == target) ? tex.GetNativeTextureID() + 1 : tex.GetNativeTextureID());
            Gl.glTexSubImage2D(Gl.GL_TEXTURE_2D, 0, 0, 0, (int)inputSize.x, (int)inputSize.y, Gl.GL_RGB, Gl.GL_UNSIGNED_BYTE, Image.ImageMapPtr);
            return;
        }

        // The slow method: copy image map data to a manager buffer, and then create a Color32
        // for each pixel
        Marshal.Copy(Image.ImageMapPtr, rawImageMap, 0, rawImageMap.Length);
        int src = 0;
        int dst = 0;
        for (int row = 0; row < (int)inputSize.y; row++) {
            for (int col = 0; col < (int)inputSize.x; col++) {
                imageMapPixels[dst] = new Color32(rawImageMap[src], rawImageMap[src + 1], rawImageMap[src + 2], 255);
                src += 3;
                dst++;
            }
            dst += (int)(outputSize.x - inputSize.x);
        }

        tex.SetPixels32(imageMapPixels);
        tex.Apply();
    }

   void OnGUI()
   {
       if (null == target) {
           GUI.Box(new Rect(Screen.width - 128, Screen.height - 96, 128, 96), imageMapTexture);
       }
   }
}
