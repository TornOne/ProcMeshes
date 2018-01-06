using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class NaiveSmoothSculpture : MonoBehaviour {
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
		Dictionary<int, int> vertexMap = new Dictionary<int, int>();
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		int layerLength = segments.x * segments.z;
		int vLayerLength = (segments.x + 1) * (segments.z + 1);
		int vRowLength = segments.x + 1;

		//Check every cube
		for (int y = 0; y < segments.y; y++) {
			int yIndex = y * layerLength;
			int y1 = y + 1;
			int y0Index = y * vLayerLength;
			int y1Index = y0Index + vLayerLength;
			for (int z = 0; z < segments.z; z++) {
				int zIndex = z * segments.x;
				int z1 = z + 1;
				int z0Index = z * vRowLength;
				int z1Index = z0Index + vRowLength;
				for (int x = 0; x < segments.x; x++) {
					int index = yIndex + zIndex + x;
					int x1 = x + 1;
					//Only check filled cubes
					if (gridPoints[index]) {
						//Top side
						if (y == segments.y - 1 || !gridPoints[index + layerLength]) {
							int i1 = y1Index + z0Index + x;
							int i2 = y1Index + z1Index + x;
							int i3 = y1Index + z0Index + x1;
							int i4 = y1Index + z1Index + x1;
							int vi1, vi2, vi3, vi4;
							if (!vertexMap.TryGetValue(i1, out vi1)) {
								vi1 = vertices.Count;
								vertices.Add(new Vector3(x, y1, z));
								vertexMap.Add(i1, vi1);
							}
							if (!vertexMap.TryGetValue(i2, out vi2)) {
								vi2 = vertices.Count;
								vertices.Add(new Vector3(x, y1, z1));
								vertexMap.Add(i2, vi2);
							}
							if (!vertexMap.TryGetValue(i3, out vi3)) {
								vi3 = vertices.Count;
								vertices.Add(new Vector3(x1, y1, z));
								vertexMap.Add(i3, vi3);
							}
							if (!vertexMap.TryGetValue(i4, out vi4)) {
								vi4 = vertices.Count;
								vertices.Add(new Vector3(x1, y1, z1));
								vertexMap.Add(i4, vi4);
							}
							triangles.Add(vi1);
							triangles.Add(vi2);
							triangles.Add(vi3);
							triangles.Add(vi2);
							triangles.Add(vi4);
							triangles.Add(vi3);
						}
						//Bottom side
						if (y == 0 || !gridPoints[index - layerLength]) {
							int i1 = y0Index + z0Index + x;
							int i2 = y0Index + z1Index + x;
							int i3 = y0Index + z0Index + x1;
							int i4 = y0Index + z1Index + x1;
							int vi1, vi2, vi3, vi4;
							if (!vertexMap.TryGetValue(i1, out vi1)) {
								vi1 = vertices.Count;
								vertices.Add(new Vector3(x, y, z));
								vertexMap.Add(i1, vi1);
							}
							if (!vertexMap.TryGetValue(i2, out vi2)) {
								vi2 = vertices.Count;
								vertices.Add(new Vector3(x, y, z + 1));
								vertexMap.Add(i2, vi2);
							}
							if (!vertexMap.TryGetValue(i3, out vi3)) {
								vi3 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y, z));
								vertexMap.Add(i3, vi3);
							}
							if (!vertexMap.TryGetValue(i4, out vi4)) {
								vi4 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y, z + 1));
								vertexMap.Add(i4, vi4);
							}
							triangles.Add(vi3);
							triangles.Add(vi2);
							triangles.Add(vi1);
							triangles.Add(vi3);
							triangles.Add(vi4);
							triangles.Add(vi2);
						}
						//Forward side
						if (z == segments.z - 1 || !gridPoints[index + segments.x]) {
							int i1 = y1Index + z1Index + x;
							int i2 = y1Index + z1Index + x1;
							int i3 = y0Index + z1Index + x1;
							int i4 = y0Index + z1Index + x;
							int vi1, vi2, vi3, vi4;
							if (!vertexMap.TryGetValue(i1, out vi1)) {
								vi1 = vertices.Count;
								vertices.Add(new Vector3(x, y + 1, z + 1));
								vertexMap.Add(i1, vi1);
							}
							if (!vertexMap.TryGetValue(i2, out vi2)) {
								vi2 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y + 1, z + 1));
								vertexMap.Add(i2, vi2);
							}
							if (!vertexMap.TryGetValue(i3, out vi3)) {
								vi3 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y, z + 1));
								vertexMap.Add(i3, vi3);
							}
							if (!vertexMap.TryGetValue(i4, out vi4)) {
								vi4 = vertices.Count;
								vertices.Add(new Vector3(x, y, z + 1));
								vertexMap.Add(i4, vi4);
							}
							triangles.Add(vi1);
							triangles.Add(vi4);
							triangles.Add(vi3);
							triangles.Add(vi1);
							triangles.Add(vi3);
							triangles.Add(vi2);
						}
						//Backward side
						if (z == 0 || !gridPoints[index - segments.x]) {
							int i1 = y1Index + z0Index + x;
							int i2 = y1Index + z0Index + x1;
							int i3 = y0Index + z0Index + x1;
							int i4 = y0Index + z0Index + x;
							int vi1, vi2, vi3, vi4;
							if (!vertexMap.TryGetValue(i1, out vi1)) {
								vi1 = vertices.Count;
								vertices.Add(new Vector3(x, y + 1, z));
								vertexMap.Add(i1, vi1);
							}
							if (!vertexMap.TryGetValue(i2, out vi2)) {
								vi2 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y + 1, z));
								vertexMap.Add(i2, vi2);
							}
							if (!vertexMap.TryGetValue(i3, out vi3)) {
								vi3 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y, z));
								vertexMap.Add(i3, vi3);
							}
							if (!vertexMap.TryGetValue(i4, out vi4)) {
								vi4 = vertices.Count;
								vertices.Add(new Vector3(x, y, z));
								vertexMap.Add(i4, vi4);
							}
							triangles.Add(vi1);
							triangles.Add(vi3);
							triangles.Add(vi4);
							triangles.Add(vi1);
							triangles.Add(vi2);
							triangles.Add(vi3);
						}
						//Right side
						if (x == segments.x - 1 || !gridPoints[index + 1]) {
							int i1 = y1Index + z0Index + x1;
							int i2 = y1Index + z1Index + x1;
							int i3 = y0Index + z1Index + x1;
							int i4 = y0Index + z0Index + x1;
							int vi1, vi2, vi3, vi4;
							if (!vertexMap.TryGetValue(i1, out vi1)) {
								vi1 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y + 1, z));
								vertexMap.Add(i1, vi1);
							}
							if (!vertexMap.TryGetValue(i2, out vi2)) {
								vi2 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y + 1, z + 1));
								vertexMap.Add(i2, vi2);
							}
							if (!vertexMap.TryGetValue(i3, out vi3)) {
								vi3 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y, z + 1));
								vertexMap.Add(i3, vi3);
							}
							if (!vertexMap.TryGetValue(i4, out vi4)) {
								vi4 = vertices.Count;
								vertices.Add(new Vector3(x + 1, y, z));
								vertexMap.Add(i4, vi4);
							}
							triangles.Add(vi1);
							triangles.Add(vi3);
							triangles.Add(vi4);
							triangles.Add(vi1);
							triangles.Add(vi2);
							triangles.Add(vi3);
						}
						//Left side
						if (x == 0 || !gridPoints[index - 1]) {
							int i1 = y1Index + z0Index + x;
							int i2 = y1Index + z1Index + x;
							int i3 = y0Index + z1Index + x;
							int i4 = y0Index + z0Index + x;
							int vi1, vi2, vi3, vi4;
							if (!vertexMap.TryGetValue(i1, out vi1)) {
								vi1 = vertices.Count;
								vertices.Add(new Vector3(x, y + 1, z));
								vertexMap.Add(i1, vi1);
							}
							if (!vertexMap.TryGetValue(i2, out vi2)) {
								vi2 = vertices.Count;
								vertices.Add(new Vector3(x, y + 1, z + 1));
								vertexMap.Add(i2, vi2);
							}
							if (!vertexMap.TryGetValue(i3, out vi3)) {
								vi3 = vertices.Count;
								vertices.Add(new Vector3(x, y, z + 1));
								vertexMap.Add(i3, vi3);
							}
							if (!vertexMap.TryGetValue(i4, out vi4)) {
								vi4 = vertices.Count;
								vertices.Add(new Vector3(x, y, z));
								vertexMap.Add(i4, vi4);
							}
							triangles.Add(vi1);
							triangles.Add(vi4);
							triangles.Add(vi3);
							triangles.Add(vi1);
							triangles.Add(vi3);
							triangles.Add(vi2);
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
