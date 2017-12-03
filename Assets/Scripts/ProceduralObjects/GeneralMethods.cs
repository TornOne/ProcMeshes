using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralMethods : MonoBehaviour {

    public Mesh ConvertToFlat(Mesh original) {
        // converts the mesh to nave no shared vertices, for the low poly look
        Vector3[] origVertices = original.vertices;
        int[] origTriangles = original.triangles;
        Vector3[] newVertices = new Vector3[origTriangles.Length];
        int[] newTriangles = new int[origTriangles.Length];

        for (int i = 0; i < origTriangles.Length; i += 3) {
            for (int j = i; j < i + 3; j++) {
                newVertices[j] = origVertices[origTriangles[j]];
                newTriangles[j] = j;
            }
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices;
        newMesh.triangles = newTriangles;
        newMesh.RecalculateNormals();
        return newMesh;
    }

    public Vector3[] Randomize(Vector3[] oldVertices, float maxRandomDistance) {
        Vector3[] newVertices = new Vector3[oldVertices.Length];
        for (int i = 0; i < oldVertices.Length; i++) {
            float rX = Random.Range(0.0F, maxRandomDistance);
            float rY = Random.Range(0.0F, maxRandomDistance);
            float rZ = Random.Range(0.0F, maxRandomDistance);
            newVertices[i] = new Vector3(oldVertices[i].x + rX, oldVertices[i].y + rY, oldVertices[i].z + rZ);
        }
        return newVertices;
    }

    public Matrix4x4 Scale(float x, float y, float z) {
        return Matrix4x4.Scale(new Vector3(x, y, z));
    }

    public Matrix4x4 Translate(float x, float y, float z) {
        return Matrix4x4.Translate(new Vector3(x, y, z));
    }

    public Matrix4x4 Rotate(float x, float y, float z) {
        return Matrix4x4.Rotate(Quaternion.Euler(x, y, z));
    }

    public Vector3[] ApplyTransformation(Vector3[] origVertices, Matrix4x4 mat) {
        Vector3[] newVertices = new Vector3[origVertices.Length];
        for (int i = 0; i < origVertices.Length; i++) {
            newVertices[i] = mat.MultiplyPoint3x4(origVertices[i]);
        }
        return newVertices;
    }

    public float Radians(float angleInDegrees) {
        // converts angle from degrees to radians
        return (float)(System.Math.PI / 180) * angleInDegrees;
    }
}
