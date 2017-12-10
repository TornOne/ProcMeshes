using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JKMesh : MonoBehaviour
{
    public Vector3[] newVertices;
    public Vector2[] newUV;
    public int[] newTriangles;
    void Start()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        
        mesh.vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0),
                                        new Vector3(1, 0, 0)};
        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new int[] { 0, 1, 2,
                                     0, 2, 3 };

    }
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int i = 0;
        while (i < vertices.Length)
        {
            vertices[i] += normals[i] * Mathf.Sin(Time.time);
            i++;
        }
        mesh.vertices = vertices;
    }
}