
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class CombineMeshScript : MonoBehaviour
{
    //meshes to combine
    public List<MeshFilter> meshToCombine;
    //target meshes to be created
    public MeshFilter targetMesh;

    void CombineMesh()
    {
        var combine = new CombineInstance[meshToCombine.Count];

        for (int i = 0; i < meshToCombine.Count; i++)
        {
            combine[i].mesh = meshToCombine[i].sharedMesh;
            combine[i].transform = meshToCombine[i].transform.localToWorldMatrix;
        }

        //creeate an empty mesh for combined meshes
        var mesh = new Mesh();

        //call targetMesh and pass array of combined instance
        mesh.CombineMeshes(combine);
        //assign the target mesh to the combined game object
        targetMesh.mesh = mesh;

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
