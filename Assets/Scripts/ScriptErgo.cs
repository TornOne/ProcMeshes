using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptErgo : MonoBehaviour {

    // Use this for initialization
    void Start () {
        gameObject.AddComponent(typeof(MeshFilter));
        gameObject.AddComponent(typeof(MeshRenderer));
        GetComponent<MeshFilter>().mesh = GenerateSquare();
        GetComponent<Renderer>().material.color = Color.white;
    }
	
	// Update is called once per frame
	void Update () {
        
	}

    Mesh GenerateSquare()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[] {
            new Vector3(-1, 0, -1),
            new Vector3(1, 0, -1),
            new Vector3(1, 0, 1),
            new Vector3(-1, 0, 1),
        };
        int[] triangles = new int[] { // mõlemalt poolt nähtav
            0, 1, 2, // alumine
            0, 2, 3, // alumine
            0, 2, 1, // pealmine
            0, 3, 2  // pealmine
        };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        return mesh;
    }
}
