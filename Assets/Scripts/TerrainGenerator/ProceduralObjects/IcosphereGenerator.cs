using UnityEngine;
using System.Collections.Generic;

// Code based on: http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html

public class IcosphereGenerator : MonoBehaviour {
    private Dictionary<long, int> middlePointIndexCache;
    private List<Vector3> vertices;

    private struct TriangleIndices {
        public int v1;
        public int v2;
        public int v3;

        public TriangleIndices(int v1, int v2, int v3) {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }

    private void addVertex(Vector3 vertex) {
        vertices.Add(Vector3.Normalize(vertex));
    }

    private int getMiddlePoint(int p1, int p2) {
        // first check if we have it already
        bool firstIsSmaller = p1 < p2;
        int smallerIndex = firstIsSmaller ? p1 : p2;
        int greaterIndex = firstIsSmaller ? p2 : p1;
        long key = long.Parse(smallerIndex.ToString() + greaterIndex.ToString());
        
        if (this.middlePointIndexCache.ContainsKey(key)) {
            return middlePointIndexCache[key];
        }

        //not in cache, calculate it
        Vector3 firstVertex = vertices[p1];
        Vector3 secondVertex = vertices[p2];
        Vector3 middleVertex = (firstVertex + secondVertex) / 2.0F;

        addVertex(middleVertex);
        int index = vertices.Count - 1;
        this.middlePointIndexCache.Add(key, index);
        return index;
    }

    public Mesh CreateIcosphere(bool subdivide) {
        middlePointIndexCache = new Dictionary<long, int>();
        vertices = new List<Vector3>();

        float t = (float)(1.0 + System.Math.Sqrt(5.0)) / 2.0F;

        addVertex(new Vector3(-1, t, 0));
        addVertex(new Vector3(1, t, 0));
        addVertex(new Vector3(-1, -t, 0));
        addVertex(new Vector3(1, -t, 0));

        addVertex(new Vector3(0, -1, t));
        addVertex(new Vector3(0, 1, t));
        addVertex(new Vector3(0, -1, -t));
        addVertex(new Vector3(0, 1, -t));

        addVertex(new Vector3(t, 0, -1));
        addVertex(new Vector3(t, 0, 1));
        addVertex(new Vector3(-t, 0, -1));
        addVertex(new Vector3(-t, 0, 1));

        // create 20 triangles of the icosahedron
        List<TriangleIndices> faces = new List<TriangleIndices>();

        // 5 faces around point 0
        faces.Add(new TriangleIndices(0, 11, 5));
        faces.Add(new TriangleIndices(0, 5, 1));
        faces.Add(new TriangleIndices(0, 1, 7));
        faces.Add(new TriangleIndices(0, 7, 10));
        faces.Add(new TriangleIndices(0, 10, 11));

        // 5 adjacent faces 
        faces.Add(new TriangleIndices(1, 5, 9));
        faces.Add(new TriangleIndices(5, 11, 4));
        faces.Add(new TriangleIndices(11, 10, 2));
        faces.Add(new TriangleIndices(10, 7, 6));
        faces.Add(new TriangleIndices(7, 1, 8));

        // 5 faces around point 3
        faces.Add(new TriangleIndices(3, 9, 4));
        faces.Add(new TriangleIndices(3, 4, 2));
        faces.Add(new TriangleIndices(3, 2, 6));
        faces.Add(new TriangleIndices(3, 6, 8));
        faces.Add(new TriangleIndices(3, 8, 9));

        // 5 adjacent faces 
        faces.Add(new TriangleIndices(4, 9, 5));
        faces.Add(new TriangleIndices(2, 4, 11));
        faces.Add(new TriangleIndices(6, 2, 10));
        faces.Add(new TriangleIndices(8, 6, 7));
        faces.Add(new TriangleIndices(9, 8, 1));

        if (subdivide) {
            List<TriangleIndices> faces2 = new List<TriangleIndices>();
            foreach (TriangleIndices tri in faces) {
                int a = getMiddlePoint(tri.v1, tri.v2);
                int b = getMiddlePoint(tri.v2, tri.v3);
                int c = getMiddlePoint(tri.v3, tri.v1);

                faces2.Add(new TriangleIndices(tri.v1, a, c));
                faces2.Add(new TriangleIndices(tri.v2, b, a));
                faces2.Add(new TriangleIndices(tri.v3, c, b));
                faces2.Add(new TriangleIndices(a, b, c));
            }
            faces = faces2;
        }
        
        int[] triangles = new int[faces.Count * 3];
        for (int i = 0; i < faces.Count; i++) {
            TriangleIndices tri = faces[i];
            triangles[3 * i] = tri.v1;
            triangles[3 * i + 1] = tri.v2;
            triangles[3 * i + 2] = tri.v3;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles;
        return mesh;
    }
}
