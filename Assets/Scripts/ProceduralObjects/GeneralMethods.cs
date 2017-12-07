using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralMethods : MonoBehaviour {

    public Mesh ConvertToFlat(Mesh original) {
        // converts the mesh to nave no shared vertices, for the low poly look
        Mesh newMesh = new Mesh();
        newMesh.subMeshCount = original.subMeshCount;
        int trianglesSum = 0;
        for (int i = 0; i < original.subMeshCount; i++) {
            trianglesSum += original.GetTriangles(i).Length; 
        }
        newMesh.vertices = new Vector3[trianglesSum];
        Vector3[] newVertices = new Vector3[trianglesSum];
        int offset = 0;
        for (int sub = 0; sub < original.subMeshCount; sub++) {
            int[] oldTriangles = original.GetTriangles(sub);
            int[] newTriangles = new int[oldTriangles.Length];
            for (int i = 0; i < oldTriangles.Length; i++) {
                newVertices[offset + i] = original.vertices[oldTriangles[i]];
                newTriangles[i] = offset + i;
            }
            offset += oldTriangles.Length;
            newMesh.SetTriangles(newTriangles, sub, false);
        }
        newMesh.vertices = newVertices;
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
