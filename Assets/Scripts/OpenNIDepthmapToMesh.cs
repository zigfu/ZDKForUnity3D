
using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using OpenNI;

public class OpenNIDepthmapToMesh : MonoBehaviour
{
   public int factor = 2;
       public Vector3 gridScale = Vector3.one;

       short[] rawDepthMap;
       float[] depthHistogramMap;
       int XRes;
       int YRes;
       Mesh mesh;
       MeshFilter meshFilter;

       // Use this for initialization
       void Start ()
       {
               // init stuff
               MapOutputMode mom = OpenNIContext.Instance.Depth.MapOutputMode;
               YRes = mom.YRes;
               XRes = mom.XRes;

               // depthmap data
               rawDepthMap = new short[(int)(mom.XRes * mom.YRes)];

               // histogram stuff
               int maxDepth = (int)OpenNIContext.Instance.Depth.DeviceMaxDepth;
               depthHistogramMap = new float[maxDepth];
				mesh = new Mesh();
		
               meshFilter = (MeshFilter)GetComponent(typeof(MeshFilter));
				meshFilter.mesh = mesh;
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
               depthHistogramMap[i] = (1.0f - (depthHistogramMap[i] /
numOfPoints)) * 255;
               }
       }
               depthHistogramMap[0] = 0;
       }

       void UpdateDepthmapMesh()
   {
               if (meshFilter == null)
                       return;
               
               mesh.Clear();
               //meshFilter.mesh.Clear(); //the meshFilter mesh is assigned to the "mesh" variable
               // flip the depthmap as we create the texture
               int YScaled = YRes/factor;
               int XScaled = XRes/factor;
               int depthIndex = 0;
               // first stab, generate all vertices (next time, only vertices for 'valid' depths)
               // first stab, decimate rather than average depth pixels
               Vector3[] verts = new Vector3[XScaled * YScaled];
               for (int y = YScaled-1; y >= 0; y--)
               {
                       for (int x = 0; x < XScaled; x++)
                       {
                               short pixel = rawDepthMap[depthIndex];
                               verts[y*XScaled + x] = new Vector3(x * gridScale.x, y *
gridScale.y, (float)depthHistogramMap[pixel] * gridScale.z);
                               depthIndex += factor;
                       }
                       // Skip lines
                       depthIndex += (factor-1)*XRes;
               }
               mesh.vertices = verts;

               Vector2[] uvs = new Vector2[mesh.vertices.Length];
               for (int y = YScaled-1; y >= 0; y--)
               {
                       for (int x = 0; x < XScaled; x++)
                       {
                               uvs[y*XScaled + x] = new Vector2((float)x / (float)XScaled,
(float)y / (float)YScaled);
                       }
               }
               mesh.uv = uvs;

               depthIndex = 0;
               int[] tris = new int[(XScaled-1) * (YScaled-1) * 2 * 3];
               int triIndex = 0;
               for (int y = 0; y < (YScaled-1); y++)
               {
                       for (int x = 0; x < (XScaled-1); x++)
                       {
                               short pixel = rawDepthMap[depthIndex];
                               if (true || pixel > 0)
                               {
                                       tris[triIndex++] = x + y*XScaled;
                                       tris[triIndex++] = x + 1 + y*XScaled;
                                       tris[triIndex++] = x + (y+1)*XScaled;

                                       tris[triIndex++] = x + y*XScaled+1;
                                       tris[triIndex++] = x+(y+1)*XScaled+1;
                                       tris[triIndex++] = x+(y+1)*XScaled;
                               }
                               depthIndex += factor;
                       }
                       // Skip lines
                       depthIndex += (factor-1)*XRes;
               }
               mesh.triangles = tris;
              // meshFilter.mesh = mesh; //no need for so many mesh assignments, this causes the mesh count to increase each time
  }

   int lastFrameId;
   void FixedUpdate()
   {
       // update only if we have a new depth frame
       if (lastFrameId != OpenNIContext.Instance.Depth.FrameID) {
           lastFrameId = OpenNIContext.Instance.Depth.FrameID;
           Marshal.Copy(OpenNIContext.Instance.Depth.DepthMapPtr,
rawDepthMap, 0, rawDepthMap.Length);
           UpdateHistogram();
           UpdateDepthmapMesh();
       }
   }
}
