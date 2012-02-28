
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class DepthmapToParticles : MonoBehaviour
{
    public Vector3 gridScale = Vector3.one;
    //public bool GenerateNormals = false;
    //public bool GenerateUVs = true;
    public bool RealWorldPoints = true; // perform perspective transform on depth-map

    public Vector2 DesiredResolution = new Vector2(320, 240); // should be a divisor of 640x480
                                                             // and 320x240 is too high (too many vertices)

    public bool onlyUsers = true;

    public Vector3 velocity = new Vector3(0f,1f,0f);
    public GameObject particlePrefab;
    private ParticleEmitter[] particleEmitters;
    static int MAX_PARTICLES_PER_PE = 16250;
    int factorX;
    int factorY;
    private int YScaled;
    private int XScaled;
    short[] rawDepthMap;
    float[] depthHistogramMap;
    

    int XRes;
    int YRes;
    int emitterCount;
    public Color color;
    public float size = .1f;
    public float energy = 1f;
    //Mesh mesh;
    //MeshFilter meshFilter;

    //Vector2[] uvs;
    //Vector3[] verts;
    //int[] tris;
    
    public int cycles = 10;
    // Use this for initialization
    void Start()
    {
        // init stuff
        
        YRes = ZigInput.Depth.yres;
        XRes = ZigInput.Depth.xres;
        factorX = (int)(XRes / DesiredResolution.x);
        factorY = (int)(YRes / DesiredResolution.y);
        // depthmap data
        rawDepthMap = new short[(int)(XRes * YRes)];

        YScaled = YRes / factorY;
        XScaled = XRes / factorX;

     
        emitterCount = 1 + ((XScaled * YScaled) / MAX_PARTICLES_PER_PE);
        Debug.Log(emitterCount);
        
        particleEmitters = new ParticleEmitter[emitterCount*cycles];
        for (int i = 0; i < emitterCount * cycles; i++)
        {
            particleEmitters[i] = ((GameObject)Instantiate(particlePrefab, Vector3.zero, Quaternion.identity)).GetComponent<ParticleEmitter>();
            //particleEmitters[i].particles = new Particle[MAX_PARTICLES_PER_PE];
        }
        ZigInput.Instance.AddListener(gameObject);        
    }
    
    private int cycle = 0;
    void LateUpdate()
    {
        int x = 0;
     int y = 0;
        short[] rawDepthhMap = ZigInput.Depth.data;
        short[] rawLabelMap = ZigInput.LabelMap.data;
        for (int i = cycle*emitterCount; i < (cycle+1)*emitterCount; i++)
        {
            particleEmitters[i].ClearParticles();
            for (int particleIndex = 0; particleIndex < MAX_PARTICLES_PER_PE; particleIndex++)
            {
                if (y >= YScaled)
                {
                    //Debug.Log("points drawn:" + j);
                    break;                   
                }
                Vector3 scale = transform.localScale;
                Vector3 vec = new Vector3(x * scale.x, y * scale.y, rawDepthhMap[x * factorX + XRes * factorY * y] * scale.z);
                if (onlyUsers)
                {
                    if (rawLabelMap[x * factorX + XRes * factorY * y] != 0)
                    {
                        particleEmitters[i].Emit(transform.rotation * vec + transform.position, velocity, size, energy, color);
                    }
                }
                else
                {
                    particleEmitters[i].Emit(transform.rotation * vec + transform.position, velocity, size, energy, color);
                }
                x = (x + 1) % XScaled;
                y = (x == 0) ? y+1 : y;
            }

            if (y >= YScaled)
            {
                break;
            }
            
        }
        cycle = (cycle + 1) % cycles;


        

    }


}
