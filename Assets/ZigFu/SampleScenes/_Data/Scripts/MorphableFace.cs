using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class MorphableFace : MonoBehaviour
{
 	public GameObject defaultMeshGameObject;
		
	public Mesh neutralExpressionMesh;

    // The meshes corresponding to each MorphableFeature
	public Mesh browLowerMesh;
	public Mesh browRaiserMesh;
	public Mesh jawLowerMesh;
	public Mesh lipCornerDepressorMesh;
	public Mesh lipRaiserMesh;
	public Mesh lipStretcherMesh;

    public bool loadFromFile = false;
    public string filePath = "Assets/MyBlendShapeCollection.txt";


    BlendShapeCollection _blendShapeCollection;
    Mesh _workingMesh;
    Dictionary<MorphableFeature, bool> _morphEnabledStates;

    bool _meshNeedsUpdate = false;      // True when one or more morphCoefficents have just been changed


    #region MorphableFeature and MorphCoefficients

    // --- MorphableFeature ---

    public enum MorphableFeature
    {
        LipRaiser,
        JawLower,
        LipStretcher,
        BrowLower,
        LipCornerDepressor,
        BrowRaiser
    }

    public int NumMorphableFeatures { 
        get { return Enum.GetValues(typeof(MorphableFeature)).Length; } 
    }


    // --- MorphCoefficients ---

    public const float MinMorphCoeff = 0, MaxMorphCoeff = 1;

    Dictionary<MorphableFeature, float> _morphCoefficients;

    public float GetMorphCoefficient(MorphableFeature mf)
        { return _morphCoefficients[mf]; }
    public void SetMorphCoefficient(MorphableFeature mf, float value)
    {
        float oldCoeff = _morphCoefficients[mf];
        float newCoeff = Mathf.Clamp(value, MinMorphCoeff, MaxMorphCoeff); 
        if(newCoeff != oldCoeff)
        {
            _morphCoefficients[mf] = newCoeff;
            _meshNeedsUpdate = true;
        }
    }

    public bool GetMorphEnabled(MorphableFeature mf)
        { return _morphEnabledStates[mf]; }
    public void SetMorphEnabled(MorphableFeature mf, bool doEnable)
    {
        _morphEnabledStates[mf] = doEnable;
        _meshNeedsUpdate = true;
    }

    #endregion


    #region Init and Destroy

    void Awake()
    {
        InitMorphedFeatures();
		
        TryCreateBlendShapes(loadFromFile);
		
        // Populate the working mesh
        MeshFilter filter = defaultMeshGameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
        filter.sharedMesh = neutralExpressionMesh;
        _workingMesh = filter.mesh;
    }

    void InitMorphedFeatures()
    {
        _morphCoefficients = new Dictionary<MorphableFeature, float>();
        foreach (MorphableFeature mf in Enum.GetValues(typeof(MorphableFeature)))
        {
            _morphCoefficients.Add(mf, 0);
        }

         _morphEnabledStates = new Dictionary<MorphableFeature, bool>();
        foreach (MorphableFeature mf in Enum.GetValues(typeof(MorphableFeature)))
        {
            _morphEnabledStates.Add(mf, true);
        }
    }

    bool TryCreateBlendShapes(bool loadFromFile = true)
    {
        try
        {
            if (loadFromFile)
            {
                _blendShapeCollection = BlendShapeCollection.LoadFromFile(filePath);
            }
            else
            {
                // Create an array of the feature Meshes, ensuring the index of each Mesh within the array equals
                //  the int value of the corresponding MorphableFeature.
                Mesh[] morphedFeatureMeshes = new Mesh[] {
                    lipRaiserMesh, jawLowerMesh, lipStretcherMesh, browLowerMesh, lipCornerDepressorMesh, browRaiserMesh
                };

                _blendShapeCollection = BlendShapeCollection.BuildBlendShapes(neutralExpressionMesh, morphedFeatureMeshes);
                BlendShapeCollection.SaveToFile(_blendShapeCollection, filePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
        finally
        {
            // We have created the blendshapes so the feature meshes are no longer needed
            browLowerMesh = null;
            browRaiserMesh = null;
            jawLowerMesh = null;
            lipCornerDepressorMesh = null;
            lipRaiserMesh = null;
            lipStretcherMesh = null;
        }

        return true;
    }
	
    public void Reset()
    {
        foreach (MorphableFeature mf in Enum.GetValues(typeof(MorphableFeature)))
        {
            SetMorphCoefficient(mf, MinMorphCoeff);
        }
    }

    #endregion


    void Update()
    {
        if (_meshNeedsUpdate) { UpdateMesh(); }
	}

    // Blends the NeutralExpressionMesh together with each MorphedFeatureMesh,
    //  using their corresponding MorphCoefficients to weight their influence
    void UpdateMesh()
    {
        // Set up working data to store mesh offset information.
        Vector3[] morphedVertices = neutralExpressionMesh.vertices;
        Vector3[] morphedNormals  = neutralExpressionMesh.normals;

        for (int j = 0; j < NumMorphableFeatures; j++)
        {
            MorphableFeature mf = (MorphableFeature)j;
            if (_morphEnabledStates[mf] == false) { continue; }

            float weight = _morphCoefficients[mf];
            if (Mathf.Approximately(weight, 0)) { continue; }

            // Adjust each vertex according to the offset value and weight
            foreach (var v in _blendShapeCollection[j].vertices)
            {
                int idx = v.originalIndex;
                morphedVertices[idx] += v.deltaPosition * weight;
                morphedNormals[idx]  += v.deltaNormal   * weight;
            }
        }

        // Update the actual mesh with new vertex and normal information, then recalculate the mesh bounds.      
        _workingMesh.vertices = morphedVertices;
        _workingMesh.normals  = morphedNormals;
        _workingMesh.RecalculateBounds();

        _meshNeedsUpdate = false;
    }

}
