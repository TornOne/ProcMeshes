using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class NaiveSculpture : MonoBehaviour {
	public Vector3 size;
	public Vector3Int segments;
	bool[] gridPoints;

	Mesh mesh;

	void Start() {
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
		mesh.MarkDynamic();
		gridPoints = new bool[segments.x * segments.y * segments.z];
		for (int i = 0; i < gridPoints.Length; i++) {
			gridPoints[i] = true;
		}
		RefreshMesh();
		mesh.RecalculateBounds();

		transform.localScale = new Vector3(size.x / segments.x, size.y / segments.y, size.z / segments.z);
		transform.localPosition = new Vector3(size.x * -0.5f, 0, size.z * -0.5f);
	}

	void RefreshMesh() {
		List<Vector3> vertices = new List<Vector3>();
		int vertexIndex = 0;
		List<int> triangles = new List<int>();
		int layerLength = segments.x * segments.z;

		//Check every cube
		for (int y = 0; y < segments.y; y++) {
			int yIndex = y * layerLength;
			for (int z = 0; z < segments.z; z++) {
				int zIndex = z * segments.x;
				for (int x = 0; x < segments.x; x++) {
					int index = yIndex + zIndex + x;
					//Only check filled cubes
					if (gridPoints[index]) {
						//Top side
						if (y == segments.y - 1 || !gridPoints[index + layerLength]) {
							vertices.Add(new Vector3(x, y + 1, z));
							vertices.Add(new Vector3(x, y + 1, z + 1));
							vertices.Add(new Vector3(x + 1, y + 1, z));
							vertices.Add(new Vector3(x + 1, y + 1, z + 1));
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 1);
							triangles.Add(vertexIndex + 2);
							triangles.Add(vertexIndex + 1);
							triangles.Add(vertexIndex + 3);
							triangles.Add(vertexIndex + 2);
							vertexIndex += 4;
						}
						//Bottom side
						if (y == 0 || !gridPoints[index - layerLength]) {
							vertices.Add(new Vector3(x, y, z));
							vertices.Add(new Vector3(x, y, z + 1));
							vertices.Add(new Vector3(x + 1, y, z));
							vertices.Add(new Vector3(x + 1, y, z + 1));
							triangles.Add(vertexIndex + 2);
							triangles.Add(vertexIndex + 1);
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 2);
							triangles.Add(vertexIndex + 3);
							triangles.Add(vertexIndex + 1);
							vertexIndex += 4;
						}
						//Forward side
						if (z == segments.z - 1 || !gridPoints[index + segments.x]) {
							vertices.Add(new Vector3(x, y + 1, z + 1));
							vertices.Add(new Vector3(x + 1, y + 1, z + 1));
							vertices.Add(new Vector3(x + 1, y, z + 1));
							vertices.Add(new Vector3(x, y, z + 1));
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 3);
							triangles.Add(vertexIndex + 2);
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 2);
							triangles.Add(vertexIndex + 1);
							vertexIndex += 4;
						}
						//Backward side
						if (z == 0 || !gridPoints[index - segments.x]) {
							vertices.Add(new Vector3(x, y + 1, z));
							vertices.Add(new Vector3(x + 1, y + 1, z));
							vertices.Add(new Vector3(x + 1, y, z));
							vertices.Add(new Vector3(x, y, z));
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 2);
							triangles.Add(vertexIndex + 3);
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 1);
							triangles.Add(vertexIndex + 2);
							vertexIndex += 4;
						}
						//Right side
						if (x == segments.x - 1 || !gridPoints[index + 1]) {
							vertices.Add(new Vector3(x + 1, y + 1, z));
							vertices.Add(new Vector3(x + 1, y + 1, z + 1));
							vertices.Add(new Vector3(x + 1, y, z + 1));
							vertices.Add(new Vector3(x + 1, y, z));
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 2);
							triangles.Add(vertexIndex + 3);
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 1);
							triangles.Add(vertexIndex + 2);
							vertexIndex += 4;
						}
						//Left side
						if (x == 0 || !gridPoints[index - 1]) {
							vertices.Add(new Vector3(x, y + 1, z));
							vertices.Add(new Vector3(x, y + 1, z + 1));
							vertices.Add(new Vector3(x, y, z + 1));
							vertices.Add(new Vector3(x, y, z));
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 3);
							triangles.Add(vertexIndex + 2);
							triangles.Add(vertexIndex);
							triangles.Add(vertexIndex + 2);
							triangles.Add(vertexIndex + 1);
							vertexIndex += 4;
						}
					}
				}
			}
		}

		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
	}

	void Update() {
		RefreshMesh();
	}
}
