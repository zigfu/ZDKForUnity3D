using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;


public class BlendShapeCollection
{
    public const string DefaultFilePath = "Assets/MyBlendShapeCollection.txt";


    #region BlendShape and BlendShapeVertex Classes

    public class BlendShapeVertex
    {
        public int originalIndex;           // The index of this vertex within its source Mesh's vertices array
        public Vector3 deltaPosition;       // Change in position when vertex is completely blended
        public Vector3 deltaNormal;         // Change in normal   when vertex is completely blended
    }

    public class BlendShape
    {
        public BlendShapeVertex[] vertices;

        public BlendShape() : this(0) { }
        public BlendShape(int size)
        {
            vertices = new BlendShapeVertex[size];
            for (int i = 0; i < size; i++)
            {
                vertices[i] = new BlendShapeVertex();
            }
        }
    }

    #endregion


    public BlendShape[] BlendShapes { get; private set; }

    public BlendShape this[int i]
    {
        get { return BlendShapes[i]; }
        private set { BlendShapes[i] = value; }
    }


    #region Init and Destroy

    public BlendShapeCollection() : this(0) { }
    public BlendShapeCollection(int size)
    {
        BlendShapes = new BlendShape[size];
        for (int i = 0; i < size; i++)
        {
            BlendShapes[i] = new BlendShape();
        }
    }

    #endregion


    #region Build Blend Shapes

    public static BlendShapeCollection BuildBlendShapes(Mesh sourceMesh, Mesh[] attributeMeshes)
    {
        ValidateAttributeAndSourceMeshCompatibility(sourceMesh, attributeMeshes);
		
        int numAttributes = attributeMeshes.Length;
        BlendShapeCollection bsc = new BlendShapeCollection(numAttributes);

        int numSrcVerts = sourceMesh.vertexCount;

        // For each attribute: Figure out which vertices are affected, then store their info in a BlendShape object.
        for (int i = 0; i < numAttributes; i++)
        {
            Mesh atrbMesh = attributeMeshes[i];
            if (!atrbMesh) { continue; }

            List<BlendShapeVertex> vertsList = new List<BlendShapeVertex>();

            for (int j = 0; j < numSrcVerts; j++)
            {
                Vector3 atrbVert = atrbMesh.vertices[j];
                Vector3 workVert = sourceMesh.vertices[j];

                // If vertex is the same in both meshes, then there's no need to blend it
                if (workVert == atrbVert) { continue; }
                
                // Create a BlendShapeVertex and populate its data.
                BlendShapeVertex v = new BlendShapeVertex();
                v.originalIndex = j;
                v.deltaPosition = atrbVert - workVert;
                v.deltaNormal = atrbMesh.normals[j] - sourceMesh.normals[j];

                vertsList.Add(v);
            }

            bsc[i].vertices = vertsList.ToArray();
        }

        return bsc;
    }

    static BlendShapeCollection BuildDummyBlendShapeCollection_FOR_TESTING_ONLY(int numBlendShapes, int numVerts)
    {
        BlendShapeCollection bsc = new BlendShapeCollection(numBlendShapes);

        for (int i = 0; i < numBlendShapes; i++)
        {
            bsc[i] = new BlendShape(numVerts);

            for (int j = 0; j < numVerts; j++)
            {
                BlendShapeVertex v = new BlendShapeVertex();
                v.originalIndex = j;
                v.deltaPosition = new Vector3(j, j * 2, j * 3);
                v.deltaNormal = 10 * v.deltaPosition;

                bsc[i].vertices[j] = v;
            }
        }

        return bsc;
    }

    #endregion


    #region Save/Load

    public static void SaveToFile(BlendShapeCollection blendShapeCollection) { SaveToFile(blendShapeCollection, DefaultFilePath); }
    public static void SaveToFile(BlendShapeCollection blendShapeCollection, string filename)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(BlendShapeCollection));
        TextWriter writer = new StreamWriter(filename);

        serializer.Serialize(writer, blendShapeCollection);

        writer.Close();
    }

    public static BlendShapeCollection LoadFromFile() { return LoadFromFile(DefaultFilePath); }
    public static BlendShapeCollection LoadFromFile(string filename)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(BlendShapeCollection));
        FileStream fs = new FileStream(filename, FileMode.Open);

        return (BlendShapeCollection)serializer.Deserialize(fs);
    }

    #endregion


    public static void ValidateAttributeAndSourceMeshCompatibility(Mesh sourceMesh, Mesh[] attributeMeshes)
    {
        int i = 0;
        foreach (var atrbMesh in attributeMeshes)
        {
            if (!atrbMesh)
            { 
                Debug.LogWarning("An Attribute Mesh is unassigned."); 
                continue; 
            }

            if (atrbMesh.vertexCount != sourceMesh.vertexCount)
            {
                String msg = String.Format(
                    "attributeMeshes[{0}].vertexCount ({1})  != sourceMesh.vertexCount ({2}).",
                    i, atrbMesh.vertexCount, sourceMesh.vertexCount);
                throw new Exception(msg);
            }

            i++;
        }
    }

}
