using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateGrassTuft : MonoBehaviour {
    private float decreaseFactor = 0.8F;
    private float leanDecreaseFactor = 1.2F;
    private float baseHeight = 0.25F;
    private float baseWidth = 0.03F;
    private float bladesDistance = 0.1F;

    private Color grassColor = new Color(0.52F, 0.68F, 0.17F, 1.0F);

    private GeneralMethods met;

    void Start () {
        met = GetComponent<GeneralMethods>();
        Mesh mesh = GenerateTuft();
        GetComponent<MeshFilter>().mesh = met.ConvertToFlat(mesh);
        GetComponent<Renderer>().material.color = grassColor;
        GetComponent<Renderer>().material.SetFloat("_Glossiness", 0.0F);
    }

    private Mesh GenerateTuft() {
        List<Mesh> blades = new List<Mesh>();
        List<float> directions = new List<float>();
        for (int i = 0; i < 9; i++) {
            float r = Random.Range(0.0F, 1.0F);
            if (blades.Count < 4 || r < 0.5F) {
                float sizeFactor = Random.Range(0.7F, 1.5F);
                float direction = Random.Range(0.0F, 360.0F);
                directions.Add(direction);
                float angle = Random.Range(0.0F, 20.0F);
                Mesh blade = GenerateBlade(sizeFactor, direction, angle);
                blades.Add(blade);
            }
        }
        blades[0].vertices = met.ApplyTransformation(blades[0].vertices, met.Rotate(0.0F, Random.Range(0.0F, 360.0F), 0.0F));
        List<int> locations = new List<int>();
        for (int i = 1; i < blades.Count; i++) {
            int loc = (int)Random.Range(0.0F, 7.99F);
            while (locations.Contains(loc)) {
                loc++;
                if (loc > 7) {
                    loc = 0;
                }
            }
            locations.Add(loc);
            float locDirection = 45.0F * loc;
            float directionDiff = locDirection - directions[i];
            Matrix4x4 rotateMat = met.Rotate(0.0F, directionDiff + Random.Range(-20.0F, 20.0F), 0.0F);
            Matrix4x4 translateMat = met.Translate((float)System.Math.Sin(met.Radians(locDirection)) * bladesDistance * Random.Range(0.8F, 1.2F), 0.0F, (float)System.Math.Cos(met.Radians(locDirection)) * bladesDistance * Random.Range(0.8F, 1.2F));
            blades[i].vertices = met.ApplyTransformation(blades[i].vertices, translateMat * rotateMat);
        }
        Mesh tuft = CombineMeshes(blades);
        tuft.vertices = met.ApplyTransformation(tuft.vertices, met.Rotate(0.0F, Random.Range(0.0F, 45.0F), 0.0F));
        return tuft;
    }

    private Mesh GenerateBlade(float sizeFactor, float direction, float angle) {
        float widthScale = Random.Range(0.7F, 1.2F);
        float leanDistance = (float)System.Math.Tan(met.Radians(angle)) * baseHeight;
        float xLean = (float)System.Math.Sin(met.Radians(direction)) * leanDistance;
        float zLean = (float)System.Math.Cos(met.Radians(direction)) * leanDistance;

        List<Vector3> vertices = new List<Vector3>();
        float widthDist = baseWidth * widthScale;
        vertices.Add(new Vector3(widthDist, 0, widthDist));
        vertices.Add(new Vector3(widthDist, 0, -widthDist));
        vertices.Add(new Vector3(-widthDist, 0, -widthDist));
        vertices.Add(new Vector3(-widthDist, 0, widthDist));
        
        for (int i = 1; i <= 4; i++) {
            int listOffset = i * 4;
            float decrease = (float)System.Math.Pow(decreaseFactor, i);
            float xLoc = ((vertices[listOffset - 4].x + vertices[listOffset - 2].x) / 2) + (xLean * (float)System.Math.Pow(leanDecreaseFactor, i) * Random.Range(0.8F, 1.2F)) + (0.3F * baseHeight * decrease * Random.Range(-1.0F, 1.0F));
            float xDist = baseWidth * widthScale * decrease;
            float zLoc = ((vertices[listOffset - 4].z + vertices[listOffset - 3].z) / 2) + (zLean * (float)System.Math.Pow(leanDecreaseFactor, i) * Random.Range(0.8F, 1.2F)) + (0.3F * baseWidth * decrease * Random.Range(-1.0F, 1.0F));
            float zDist = baseWidth * widthScale * decrease;
            float yLoc = vertices[listOffset - 4].y + decrease * baseHeight;
            if (i != 4) {
                vertices.Add(new Vector3(xLoc + xDist, yLoc, zLoc + zDist));
                vertices.Add(new Vector3(xLoc + xDist, yLoc, zLoc - zDist));
                vertices.Add(new Vector3(xLoc - xDist, yLoc, zLoc - zDist));
                vertices.Add(new Vector3(xLoc - xDist, yLoc, zLoc + zDist));
            }
            else {
                vertices.Add(new Vector3(xLoc, yLoc, zLoc));
            }
        }

        List<int> triangles = new List<int>();
        for (int i = 0; i < 3; i++) {
            int k = 4 * i;
            triangles.Add(k + 4);
            triangles.Add(k + 0);
            triangles.Add(k + 1);
            triangles.Add(k + 4);
            triangles.Add(k + 1);
            triangles.Add(k + 5);

            triangles.Add(k + 5);
            triangles.Add(k + 1);
            triangles.Add(k + 2);
            triangles.Add(k + 5);
            triangles.Add(k + 2);
            triangles.Add(k + 6);

            triangles.Add(k + 6);
            triangles.Add(k + 2);
            triangles.Add(k + 3);
            triangles.Add(k + 6);
            triangles.Add(k + 3);
            triangles.Add(k + 7);

            triangles.Add(k + 7);
            triangles.Add(k + 3);
            triangles.Add(k + 0);
            triangles.Add(k + 7);
            triangles.Add(k + 0);
            triangles.Add(k + 4);
        }
        triangles.Add(16);
        triangles.Add(12);
        triangles.Add(13);

        triangles.Add(16);
        triangles.Add(13);
        triangles.Add(14);

        triangles.Add(16);
        triangles.Add(14);
        triangles.Add(15);

        triangles.Add(16);
        triangles.Add(15);
        triangles.Add(12);

        Mesh mesh = new Mesh();
        mesh.vertices = met.ApplyTransformation(vertices.ToArray(), met.Scale(sizeFactor, sizeFactor, sizeFactor));
        mesh.triangles = triangles.ToArray();
        return mesh;
    }

    private Mesh CombineMeshes(List<Mesh> meshes) {
        int verticesCount = 0;
        int trianglesCount = 0;
        foreach (Mesh mesh in meshes) {
            verticesCount += mesh.vertices.Length;
            trianglesCount += mesh.triangles.Length;
        }
        Vector3[] vertices = new Vector3[verticesCount];
        int[] triangles = new int[trianglesCount];
        int verticesOffset = 0;
        int trianglesOffset = 0;
        for (int i = 0; i < meshes.Count; i++) {
            Mesh mesh = meshes[i];
            mesh.vertices.CopyTo(vertices, verticesOffset);
            for (int j = 0; j < mesh.triangles.Length; j++) {
                triangles[trianglesOffset + j] = mesh.triangles[j] + verticesOffset;
            }
            verticesOffset += mesh.vertices.Length;
            trianglesOffset += mesh.triangles.Length;
        }
        Mesh final = new Mesh();
        final.vertices = vertices;
        final.triangles = triangles;
        return final;
    }
}
