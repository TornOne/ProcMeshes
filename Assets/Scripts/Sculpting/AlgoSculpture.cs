using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class AlgoSculpture : MonoBehaviour {
	public Vector3 size;
	public Vector3Int segments;
	bool[] gridPoints;

	Mesh mesh;
	MeshCollider coll;
	float brushSize = 0.1f;
	bool meshModified = false;

	void Start() {
		mesh = new Mesh();
		coll = GetComponent<MeshCollider>();
		GetComponent<MeshFilter>().mesh = mesh;
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

	void Update() {
		if (Input.GetButtonDown("Fire1")) {
			RaycastHit hitInfo;
			if (coll.Raycast(Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)), out hitInfo, 2)) {
				Modify(hitInfo.point, brushSize, false);
			}
		} else if (Input.GetButtonDown("Fire2")) {
			RaycastHit hitInfo;
			if (coll.Raycast(Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)), out hitInfo, 2)) {
				Modify(hitInfo.point, brushSize, true);
			}
		}
		brushSize = Mathf.Clamp01(brushSize + Input.GetAxisRaw("Mouse ScrollWheel") * 0.1f);

		if (meshModified) {
			RefreshMesh();
			meshModified = false;
		}
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

		mesh.Clear();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		coll.sharedMesh = mesh;
	}

	void Modify(Vector3 location, float size, bool add) {
		location -= transform.localPosition;
		location = new Vector3(location.x / transform.localScale.x, location.y / transform.localScale.y, location.z / transform.localScale.z);
		location -= new Vector3(0.5f, 0.5f, 0.5f);
		Vector3 radius = new Vector3(size / transform.localScale.x, size / transform.localScale.y, size / transform.localScale.z);

		int endY = Mathf.Min((int)(location.y + radius.y), segments.y - 1);
		int endZ = Mathf.Min((int)(location.z + radius.z), segments.z - 1);
		int endX = Mathf.Min((int)(location.x + radius.x), segments.x - 1);
		int layerLength = segments.x * segments.z;
		for (int y = Mathf.Max(Mathf.CeilToInt(location.y - radius.y), 0); y <= endY; y++) {
			int yIndex = y * layerLength;
			for (int z = Mathf.Max(Mathf.CeilToInt(location.z - radius.z), 0); z <= endZ; z++) {
				int zIndex = z * segments.x;
				for (int x = Mathf.Max(Mathf.CeilToInt(location.x - radius.x), 0); x <= endX; x++) {
					Vector3 distance = new Vector3((location.x - x) / radius.x, (location.y - y) / radius.y, (location.z - z) / radius.z);
					if (Vector3.Dot(distance, distance) <= 1) {
						gridPoints[yIndex + zIndex + x] = add;
					}
				}
			}
		}

		meshModified = true;
	}

}
