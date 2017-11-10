using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainMeshTools : MonoBehaviour {
	/*void Start() {
		ResetMesh(250, 1);
	}

	void Update() {
		SetRandomHeights(-2.5f, 2.5f);
	}*/

	Mesh mesh;
	Vector3[] vertices, normals;
	int[] triangles;

	//Max gridSize is 254. Tiling option are 0, 1, 2.
	public void ResetMesh(int gridSize = 100, float gridInverseDensity = 1.0f, int tiling = 1) {
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		#region Vertice assignment
		gridSize++; //We want a mesh with gridSize squares on each side, not vertices.
		vertices = new Vector3[gridSize * gridSize];

		for (int i = 0; i < vertices.Length; i++) {
			vertices[i] = new Vector3(i % gridSize * gridInverseDensity - (gridSize - 1) * gridInverseDensity * 0.5f,
			                          0,
			                          i / gridSize * gridInverseDensity - (gridSize - 1) * gridInverseDensity * 0.5f);
		}
		mesh.vertices = vertices;
		#endregion

		#region Normal assignment
		normals = new Vector3[vertices.Length];

		for (int i = 0; i < normals.Length; i++) {
			normals[i] = Vector3.up;
		}
		mesh.normals = normals;
		#endregion

		#region Triangle assignment
		triangles = new int[(gridSize - 1) * (gridSize - 1) * 6];

		switch (tiling) {
		case 0: //Stripes
			for (int i = 0; i < triangles.Length / 3; i++) {
				int row = i / 2 / (gridSize - 1);
				int col = i / 2 % (gridSize - 1);

				if (i % 2 == 0) {
					triangles[i * 3] = row * gridSize + col;
					triangles[i * 3 + 1] = (row + 1) * gridSize + col + 1;
					triangles[i * 3 + 2] = row * gridSize + col + 1;
				} else {
					triangles[i * 3] = row * gridSize + col;
					triangles[i * 3 + 1] = (row + 1) * gridSize + col;
					triangles[i * 3 + 2] = (row + 1) * gridSize + col + 1;
				}
			}
			break;
		case 1: //Zig-zag
			for (int i = 0; i < triangles.Length / 3; i++) {
				int row = i / 2 / (gridSize - 1);
				int col = i / 2 % (gridSize - 1);

				if (row % 2 == 0) {
					if (i % 2 == 0) {
						triangles[i * 3] = row * gridSize + col;
						triangles[i * 3 + 1] = (row + 1) * gridSize + col + 1;
						triangles[i * 3 + 2] = row * gridSize + col + 1;
					} else {
						triangles[i * 3] = row * gridSize + col;
						triangles[i * 3 + 1] = (row + 1) * gridSize + col;
						triangles[i * 3 + 2] = (row + 1) * gridSize + col + 1;
					}
				} else {
					if (i % 2 == 0) {
						triangles[i * 3] = row * gridSize + col;
						triangles[i * 3 + 1] = (row + 1) * gridSize + col;
						triangles[i * 3 + 2] = row * gridSize + col + 1;
					} else {
						triangles[i * 3] = row * gridSize + col + 1;
						triangles[i * 3 + 1] = (row + 1) * gridSize + col;
						triangles[i * 3 + 2] = (row + 1) * gridSize + col + 1;
					}
				}
			}
			break;
		case 2: //Criss-cross
			for (int i = 0; i < triangles.Length / 3; i++) {
				int row = i / 2 / (gridSize - 1);
				int col = i / 2 % (gridSize - 1);

				if ((row + col) % 2 == 0) {
					if (i % 2 == 0) {
						triangles[i * 3] = row * gridSize + col;
						triangles[i * 3 + 1] = (row + 1) * gridSize + col + 1;
						triangles[i * 3 + 2] = row * gridSize + col + 1;
					} else {
						triangles[i * 3] = row * gridSize + col;
						triangles[i * 3 + 1] = (row + 1) * gridSize + col;
						triangles[i * 3 + 2] = (row + 1) * gridSize + col + 1;
					}
				} else {
					if (i % 2 == 0) {
						triangles[i * 3] = row * gridSize + col;
						triangles[i * 3 + 1] = (row + 1) * gridSize + col;
						triangles[i * 3 + 2] = row * gridSize + col + 1;
					} else {
						triangles[i * 3] = row * gridSize + col + 1;
						triangles[i * 3 + 1] = (row + 1) * gridSize + col;
						triangles[i * 3 + 2] = (row + 1) * gridSize + col + 1;
					}
				}
			}
			break;
		}

		mesh.triangles = triangles;
		#endregion
	}

	public void SetRandomHeights(float min = -1, float max = 1) {
		for (int i = 0; i < vertices.Length; i++) {
			vertices[i].y = Random.Range(min, max);
		}
		mesh.vertices = vertices;
		mesh.RecalculateNormals();
	}

	public void UpdateVertices() {
		vertices = mesh.vertices;
	}

	public void UpdateNormals() {
		normals = mesh.normals;
	}

	public void UpdateTriangles() {
		triangles = mesh.triangles;
	}
}

//PS. mesh.MarkDynamic() once or every frame?
