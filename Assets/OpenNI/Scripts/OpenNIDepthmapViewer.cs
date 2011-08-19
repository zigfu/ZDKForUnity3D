using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using OpenNI;

public class OpenNIDepthmapViewer : MonoBehaviour 
{
    public int factor = 2;
    public Renderer target;
    public bool DrawHand = true;
    public Color32 handColor = new Color32(255, 255, 255, 255);
    public int handPixSize = 4;

	private Texture2D depthMapTexture;
	short[] rawDepthMap;
	float[] depthHistogramMap;
	Color32[] depthMapPixels;
	int XRes;
	int YRes;

	// Use this for initialization
	void Start () 
	{
		// init texture
		MapOutputMode mom = OpenNIContext.Instance.Depth.MapOutputMode;
		YRes = mom.YRes;
		XRes = mom.XRes;
		depthMapTexture = new Texture2D(mom.XRes/factor, mom.YRes/factor);
		
		// depthmap data
		rawDepthMap = new short[(int)(mom.XRes * mom.YRes)];
		depthMapPixels = new Color32[(mom.XRes/factor) * (mom.YRes/factor)];
		
		// histogram stuff
		int maxDepth = (int)OpenNIContext.Instance.Depth.DeviceMaxDepth;
		depthHistogramMap = new float[maxDepth];
		
		if (null == target){
			target = renderer;
		}
		
		if (null == target && null != guiTexture){
			target = guiTexture.renderer;
		}
		
		if (target){
			target.material.mainTexture = depthMapTexture;
		}
	}
	
	void UpdateHistogram()
	{
		int i, numOfPoints = 0;
		
		Array.Clear(depthHistogramMap, 0, depthHistogramMap.Length);

        for (i = 0; i < rawDepthMap.Length; i++) {
            // only calculate for valid depth
            if (rawDepthMap[i] != 0) {
                depthHistogramMap[rawDepthMap[i]]++;
                numOfPoints++;
            }
        }
		
        if (numOfPoints > 0) {
            for (i = 1; i < depthHistogramMap.Length; i++) {   
		        depthHistogramMap[i] += depthHistogramMap[i-1];
	        }
            for (i = 0; i < depthHistogramMap.Length; i++) {
                depthHistogramMap[i] = (1.0f - (depthHistogramMap[i] / numOfPoints)) * 255;
	        }
        }
		depthHistogramMap[0] = 0;
	}
	
	void UpdateDepthmapTexture()
    {
		// flip the depthmap as we create the texture
		int YScaled = YRes/factor;
		int XScaled = XRes/factor;
		int i = XScaled*YScaled-XScaled;
		int depthIndex = 0;
		for (int y = 0; y < YScaled; ++y, i-=XScaled)
		{
			for (int x = 0; x < XScaled; ++x, depthIndex += factor)
			{
				short pixel = rawDepthMap[depthIndex];
				if (pixel == 0)
				{
					depthMapPixels[i+x] = Color.clear;
				}
				else
				{
					Color32 c = new Color32(SessionManager.InSession ? (byte) 0 : (byte)depthHistogramMap[pixel], (byte)depthHistogramMap[pixel], 0, 255);
					depthMapPixels[i+x] = c;
				}
			}
            // Skip lines
			depthIndex += (factor-1)*XRes; 
		}

		if (DrawHand && SessionManager.InSession)
		{
            Point3D handProjective = OpenNIContext.Instance.Depth.ConvertRealWorldToProjective(SessionManager.LastRawPoint);
		    int k;
		    if ((handProjective.X/factor + handPixSize < XScaled) &&
			    (handProjective.Y/factor + handPixSize < YScaled) &&
			    (handProjective.X/factor - handPixSize > 0) &&
			    (handProjective.Y/factor - handPixSize > 0)) {
			
		        for (i = (int)handProjective.X-handPixSize; i <= (int)handProjective.X+handPixSize; i++) {
			        for (k = (int)handProjective.Y-handPixSize; k <= (int)handProjective.Y + handPixSize; k++) {
				        depthMapPixels[(i/factor)+((YScaled - k/factor)*XScaled)] = handColor;
			        }
		        }
    		}
		}

		depthMapTexture.SetPixels32(depthMapPixels);
        depthMapTexture.Apply();
   }

    int lastFrameId;
    void FixedUpdate()
    {
        // update only if we have a new depth frame
        if (lastFrameId != OpenNIContext.Instance.Depth.FrameID) {
            lastFrameId = OpenNIContext.Instance.Depth.FrameID;
            Marshal.Copy(OpenNIContext.Instance.Depth.DepthMapPtr, rawDepthMap, 0, rawDepthMap.Length);
            UpdateHistogram();
            UpdateDepthmapTexture();
        }
    }

    void OnGUI()
    {
        if (null == target) {
            GUI.Box(new Rect(Screen.width - 128 - 10, Screen.height - 96 - 10, 128, 96), depthMapTexture);
        }
    }
}
