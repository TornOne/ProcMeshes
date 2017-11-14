using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainMeshTools : MonoBehaviour {
	void Start() {
		ResetMesh(250, 1);
		RaiseTerrainHill(new Vector3(-60.0f, 0, 0), 20.0f, 8.0f);
	}

	void Update() {
		RaiseTerrainHill(new Vector3(0, 0, 0), Random.Range(1.0f, 30.0f), 0.3f);
		RaiseTerrain(new Vector3(-120.0f, 0, 0), Random.Range(1.0f, 30.0f), -0.5f);
	}

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

	/** Recalculates the normal of one vertice
     */
	public void RecalculateNormals(int i) {
		// Should probably change these to be inputs or calculated somehow
		int xStep = 1;
		int zStep = 1;

		int verticeTotal = vertices.Length;
		int gridSize = (int) Mathf.Sqrt(verticeTotal); // Assumes square grid
		int column = i % gridSize;
		int row = i / gridSize;

		Vector3 surroundingNormalSum = new Vector3(0, 0, 0);
		Vector3 vec1 = new Vector3(0, 0, 0);
		Vector3 vec2 = new Vector3(0, 0, 0);
		Vector3 tempVec = new Vector3(0, 0, 0);

		if (row < gridSize - 1 && row > 0
			&& column > 0 && column < gridSize - 1) {
			float yTop = vertices[i - gridSize].y;
			float yTopRight = vertices[i - (gridSize - 1)].y;
			float yRight = vertices[i + 1].y;
			float yBot = vertices[i + gridSize].y;
			float yBotLeft = vertices[i + (gridSize - 1)].y;
			float yLeft = vertices[i - 1].y;

			// Inner vertice calculations can be simplified with algebra.
			// Idea for simplification: https://stackoverflow.com/questions/6656358/calculating-normals-in-a-triangle-mesh/21660173#21660173
			normals[i] = Vector3.Normalize(new Vector3(
											 zStep * (2 * yLeft + yTop - yTopRight - 2 * yRight - yBot + yBotLeft),
											 xStep * zStep * 6,
											 xStep * (2 * yTop + yTopRight - yRight - 2 * yBot - yBotLeft + yLeft))
											);
		} else {
			// Literal edge-case:
			// Check top-left triangle
			if (row - 1 >= 0 && column - 1 >= 0) { // It exists, now we can calculate its normal
				vec1 = vertices[i - gridSize] - vertices[i];
				vec2 = vertices[i - 1] - vertices[i];
				tempVec = Vector3.Cross(vec1, vec2);
				surroundingNormalSum += tempVec;
			}
			// Check top triangle
			if (row - 1 >= 0 && column + 1 < gridSize) {
				vec1 = vertices[i - (gridSize - 1)] - vertices[i];
				vec2 = vertices[i - gridSize] - vertices[i];
				tempVec = Vector3.Cross(vec1, vec2);
				surroundingNormalSum += tempVec;
			}
			// Check top-right triangle
			if (row - 1 >= 0 && column + 1 < gridSize) {
				vec1 = vertices[i + 1] - vertices[i];
				vec2 = vertices[i - (gridSize - 1)] - vertices[i];
				tempVec = Vector3.Cross(vec1, vec2);
				surroundingNormalSum += tempVec;
			}
			// Check bot-right triangle
			if (column + 1 < gridSize && row + 1 < gridSize) {
				vec1 = vertices[i + gridSize] - vertices[i];
				vec2 = vertices[i + 1] - vertices[i];
				tempVec = Vector3.Cross(vec1, vec2);
				surroundingNormalSum += tempVec;
			}
			// Check bot triangle
			if (column - 1 >= 0 && row + 1 < gridSize) {
				vec1 = vertices[i + (gridSize - 1)] - vertices[i];
				vec2 = vertices[i + gridSize] - vertices[i];
				tempVec = Vector3.Cross(vec1, vec2);
				surroundingNormalSum += tempVec;
			}
			// Check bot-left triangle
			if (column - 1 >= 0 && row + 1 < gridSize) {
				vec1 = vertices[i - 1] - vertices[i];
				vec2 = vertices[i + (gridSize - 1)] - vertices[i];
				tempVec = Vector3.Cross(vec1, vec2);
				surroundingNormalSum += tempVec;
			}

			normals[i] = Vector3.Normalize(surroundingNormalSum); // Assigns the appropriate normal the calculated value
		}
	}

	public void RecalculateNormalsSurroundingPoint(Vector3 location, float radius) {
		for (int i = 0; i < vertices.Length; i++) {
			Vector3 vertice = vertices[i];
			float distVertSquared = Mathf.Pow(vertice.x - location.x, 2)
									+ Mathf.Pow(vertice.z - location.z, 2);
			if (distVertSquared < Mathf.Pow(radius + 2, 2)) { // Vertice is close enough to the center for its normal to be recalculated
				RecalculateNormals(i);
			}
		}
	}

	/** Method for moving vertices up and down. Moves all vertices within radius from location by speed
     *  (along the y axis). Recalculates the normals where necessary.
	 *  Ignores distance in y-coordinates.
     */
	public void RaiseTerrain(Vector3 location, float radius, float speed) {
		// On the first pass we move the vertices
		for (int i = 0; i < vertices.Length; i++) {
			Vector3 vertice = vertices[i];
			float distVertSquared = Mathf.Pow(vertice.x - location.x, 2)
									+ Mathf.Pow(vertice.z - location.z, 2);
			if (distVertSquared < Mathf.Pow(radius, 2)) { // Vertice is close enough to the center for it to be moved
				vertices[i] = vertice + new Vector3(0, speed, 0);
			}
		}

		RecalculateNormalsSurroundingPoint(location, radius);

		mesh.normals = normals;
		mesh.vertices = vertices;
	}

	/** Method which raises terrain in a hill-formation (more or less)
	 *  Recalculates normals and ignores distance on the y-axis.
	 */
	public void RaiseTerrainHill(Vector3 location, float radius, float speed) {
		float radiusSquared = Mathf.Pow(radius, 2);
		// On the first pass we move the vertices
		for (int i = 0; i < vertices.Length; i++) {
			Vector3 vertice = vertices[i];
			float distVertSquared = Mathf.Pow(vertice.x - location.x, 2)
									+ Mathf.Pow(vertice.z - location.z, 2);
			if (distVertSquared < Mathf.Pow(radius, 2)) { // Vertice is close enough to the center for it to be moved
				vertices[i] = vertice + Mathf.Pow(Mathf.Cos(distVertSquared / radiusSquared), 10) * new Vector3(0, speed, 0);
			}
		}


		RecalculateNormalsSurroundingPoint(location, radius);

		mesh.normals = normals;
		mesh.vertices = vertices;
	}
}

//PS. mesh.MarkDynamic() once or every frame?
