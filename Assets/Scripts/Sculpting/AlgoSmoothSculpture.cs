using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class AlgoSmoothSculpture : MonoBehaviour {
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
		gridPoints = new bool[(segments.x + 1) * (segments.y + 1) * (segments.z + 1)];
		for (int y = 1; y < segments.y; y++) {
			for (int z = 1; z < segments.z; z++) {
				for (int x = 1; x < segments.x; x++) {
					gridPoints[y * (segments.x + 1) * (segments.z + 1) + z * (segments.x + 1) + x] = true;
				}
			}
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

	//Idea and lookup tables from http://paulbourke.net/geometry/polygonise/
	void RefreshMesh() {
		Dictionary<int, int> vertexMap = new Dictionary<int, int>();
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		int vLayerLength = (segments.x + 1) * (segments.z + 1);
		int vRowLength = segments.x + 1;
		int eLayerLength = vLayerLength * 4;
		int eRowLength = vRowLength * 2;

		//Check every cube
		for (int y = 0; y < segments.y; y++) {
			int y0Index = y * vLayerLength;
			int y1Index = y0Index + vLayerLength;
			for (int z = 0; z < segments.z; z++) {
				int z0Index = z * vRowLength;
				int z1Index = z0Index + vRowLength;
				for (int x = 0; x < segments.x; x++) {
					int x1 = x + 1;
					Vector3 position = new Vector3(x, y, z);

					//Calculate the triangle lookup table index from vertices
					int triIndex = 0;
					if (gridPoints[y0Index + z1Index + x]) triIndex |= 1;
					if (gridPoints[y0Index + z1Index + x1]) triIndex |= 2;
					if (gridPoints[y0Index + z0Index + x1]) triIndex |= 4;
					if (gridPoints[y0Index + z0Index + x]) triIndex |= 8;
					if (gridPoints[y1Index + z1Index + x]) triIndex |= 16;
					if (gridPoints[y1Index + z1Index + x1]) triIndex |= 32;
					if (gridPoints[y1Index + z0Index + x1]) triIndex |= 64;
					if (gridPoints[y1Index + z0Index + x]) triIndex |= 128;

					//Construct all the triangles under the specified index at the specified offset
					Vector3[] triOffsets = triTable[triIndex];
					for (int i = 0; i < triOffsets.Length; i += 3) {
						Vector3 pos1 = position + triOffsets[i];
						Vector3 pos2 = position + triOffsets[i + 1];
						Vector3 pos3 = position + triOffsets[i + 2];
						int i1 = Mathf.RoundToInt(pos1.y * 2) * eLayerLength + Mathf.RoundToInt(pos1.z * 2) * eRowLength + Mathf.RoundToInt(pos1.x * 2);
						int i2 = Mathf.RoundToInt(pos2.y * 2) * eLayerLength + Mathf.RoundToInt(pos2.z * 2) * eRowLength + Mathf.RoundToInt(pos2.x * 2);
						int i3 = Mathf.RoundToInt(pos3.y * 2) * eLayerLength + Mathf.RoundToInt(pos3.z * 2) * eRowLength + Mathf.RoundToInt(pos3.x * 2);
						int vi1, vi2, vi3;
						if (!vertexMap.TryGetValue(i1, out vi1)) {
							vi1 = vertices.Count;
							vertices.Add(pos1);
							vertexMap.Add(i1, vi1);
						}
						if (!vertexMap.TryGetValue(i2, out vi2)) {
							vi2 = vertices.Count;
							vertices.Add(pos2);
							vertexMap.Add(i2, vi2);
						}
						if (!vertexMap.TryGetValue(i3, out vi3)) {
							vi3 = vertices.Count;
							vertices.Add(pos3);
							vertexMap.Add(i3, vi3);
						}
						triangles.Add(vi1);
						triangles.Add(vi2);
						triangles.Add(vi3);
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
		Vector3 radius = new Vector3(size / transform.localScale.x, size / transform.localScale.y, size / transform.localScale.z);

		int endY = Mathf.Min((int)(location.y + radius.y), segments.y);
		int endZ = Mathf.Min((int)(location.z + radius.z), segments.z);
		int endX = Mathf.Min((int)(location.x + radius.x), segments.x);
		int layerLength = (segments.x + 1) * (segments.z + 1);
		for (int y = Mathf.Max(Mathf.CeilToInt(location.y - radius.y), 1); y < endY; y++) {
			int yIndex = y * layerLength;
			for (int z = Mathf.Max(Mathf.CeilToInt(location.z - radius.z), 1); z < endZ; z++) {
				int zIndex = z * (segments.x + 1);
				for (int x = Mathf.Max(Mathf.CeilToInt(location.x - radius.x), 1); x < endX; x++) {
					Vector3 distance = new Vector3((location.x - x) / radius.x, (location.y - y) / radius.y, (location.z - z) / radius.z);
					if (Vector3.Dot(distance, distance) <= 1) {
						gridPoints[yIndex + zIndex + x] = add;
					}
				}
			}
		}

		meshModified = true;
	}

	static Vector3[][] triTable = new Vector3[][] {
		new Vector3[] {},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 0), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(1, 1, 0.5f), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 1), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 1, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 1, 1)},
		new Vector3[] {new Vector3(0.5f, 1, 1), new Vector3(0, 1, 0.5f), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0, 0.5f)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 1)},
		new Vector3[] {new Vector3(0, 0, 0.5f), new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0)},
		new Vector3[] {new Vector3(0.5f, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 0), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1)},
		new Vector3[] {new Vector3(1, 0.5f, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0.5f, 0, 0), new Vector3(1, 0.5f, 0)},
		new Vector3[] {new Vector3(1, 0, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1), new Vector3(1, 0, 0.5f)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(1, 0, 0.5f), new Vector3(1, 0.5f, 1)},
		new Vector3[] {new Vector3(0.5f, 0, 1), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 0.5f)},
		new Vector3[] {}
	};
}
