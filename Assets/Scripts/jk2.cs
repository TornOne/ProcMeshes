using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TerrainMeshTools))]
public class jk2 : MonoBehaviour {
	TerrainMeshTools tools;
	List<GameObject> flora = new List<GameObject>();
	public GameObject bush, rock, tree, deadTree;
	bool geometryChanged = false;

	public Slider mountainCountSlider, lakeCountSlider, hillCountSlider, riverCountSlider; //int
	public Slider floraDensitySlider; //Permille
	public Slider mountainSizeSlider, lakeSizeSlider, hillSizeSlider, riverSizeSlider;
	public Slider durationSlider; //Divide by 4

	int mountainCount, lakeCount, hillCount, riverCount;
	int finishedMountainCount, finishedLakeCount, finishedHillCount, finishedRiverCount;

	void Start() {
		tools = GetComponent<TerrainMeshTools>();
		Manager("generate");
	}

	void Update() {
		if (geometryChanged) {
			tools.SetVertices();
			tools.RecalculateNormals();
			tools.Colorize();
			geometryChanged = false;
		}
	}

	//Mountains -> Lakes -> Hills -> Rivers -> Flora
	//Keeps track of generation progress, calls methods as necessary
	public void Manager(string message) {
		if (message == "generate") {
			StopAllCoroutines();
			tools.ResetMesh();
			foreach (GameObject o in flora) {
				Destroy(o);
			}
			flora.Clear();

			mountainCount = (int) mountainCountSlider.value;
			lakeCount = (int) lakeCountSlider.value;
			hillCount = (int) hillCountSlider.value;
			riverCount = (int) riverCountSlider.value;

			finishedMountainCount = 0;
			finishedLakeCount = 0;
			finishedHillCount = 0;
			finishedRiverCount = 0;

			if (mountainCount != 0) {
				for (int i = 0; i < mountainCount; i++) {
					StartCoroutine(MountainRange(25, durationSlider.value / 4, mountainSizeSlider.value));
				}
			} else {
				Manager("mountain");
			}
		} else if (message == "mountain") {
			finishedMountainCount++;

			if (finishedMountainCount >= mountainCount) {
				tools.RecalculateBounds();
				if (lakeCount != 0) {
					for (int i = 0; i < lakeCount; i++) {
						Lake(durationSlider.value / 4, lakeSizeSlider.value);
					}
				} else {
					Manager("lake");
				}
			}
		} else if (message == "lake") {
			finishedLakeCount++;

			if (finishedLakeCount >= lakeCount) {
				tools.RecalculateBounds();
				if (hillCount != 0) {
					for (int i = 0; i < hillCount; i++) {
						Hill(durationSlider.value / 4, hillSizeSlider.value);
					}
				} else {
					Manager("hill");
				}
			}
		} else if (message == "hill") {
			finishedHillCount++;

			if (finishedHillCount >= hillCount) {
				tools.RecalculateBounds();
				if (riverCount != 0) {
					for (int i = 0; i < riverCount; i++) {
						StartCoroutine(River());
					}
				} else {
					Manager("river");
				}
			}
		} else if (message == "river") {
			finishedRiverCount++;

			if (finishedRiverCount >= riverCount) {
				tools.RecalculateBounds();
				StartCoroutine(Flora(durationSlider.value / 4));
			}
		}
	}

	IEnumerator MountainRange(float density = 25, float duration = 12, float size = 25) {
		//Get differing start and end edges
		List<int> edges = new List<int>(new int[] { 0, 1, 2, 3 });
		int startEdge = edges[Random.Range(0, 4)];
		edges.RemoveAt(startEdge);
		int endEdge = edges[Random.Range(0, 3)];

		//Convert them to start and end points
		Vector2 start = EdgeToPoint(startEdge);
		Vector2 end = EdgeToPoint(endEdge);

		//Sample scattered points on the line proportional to its length
		float length = Vector2.Distance(start, end);
		float xDiff = Mathf.Abs(end.x - start.x);
		float yDiff = Mathf.Abs(end.y - start.y);
		Vector2[] mountains = new Vector2[Mathf.CeilToInt(density * length)];
		float[] heightMultipliers = new float[mountains.Length];

		for (int i = 0; i < mountains.Length; i++) {
			float offsetMultiplier = size / length / tools.gridSize / 2;
			float xOffset = yDiff * offsetMultiplier;
			float yOffset = xDiff * offsetMultiplier;
			float t = (i - 0.5f) / (mountains.Length - 1);
			mountains[i] = Vector2.LerpUnclamped(start, end, t) + new Vector2(Random.Range(-xOffset, xOffset), Random.Range(-yOffset, yOffset));
			heightMultipliers[i] = 1 - Mathf.Abs(t - 0.5f);
			heightMultipliers[i] *= heightMultipliers[i];
		}

		//Raise each mountain until the target time has passed
		float endTime = Time.time + duration;
		while (Time.time < endTime) {
			for (int i = 0; i < mountains.Length; i++) {
				tools.RaiseTerrain(mountains[i].x, mountains[i].y, size, size / duration * Time.deltaTime * heightMultipliers[i], true);
			}

			geometryChanged = true;
			yield return null;
		}

		Manager("mountain");
	}

	Vector2 EdgeToPoint(int edge) {
		switch (edge) {
			case 0:
				return new Vector2(0f, Random.Range(0f, 1f));
			case 1:
				return new Vector2(Random.Range(0f, 1f), 1f);
			case 2:
				return new Vector2(1f, Random.Range(0f, 1f));
			default:
				return new Vector2(Random.Range(0f, 1f), 0f);
		}
	}

	void Lake(float duration = 12, float size = 25) {
		//Try to find a mostly non-elevated area
		for (int attempt = 0; attempt < 50; attempt++) {
			int i = Random.Range(0, tools.vertices.Length);
			int row = i / (tools.gridSize + 1);
			int col = i % (tools.gridSize + 1);
			bool flatArea = true;
			int minCol = Mathf.Max(col - 2, 0);
			int maxCol = Mathf.Min(col + 3, tools.gridSize);
			int minRow = Mathf.Max(row - 2, 0);
			int maxRow = Mathf.Min(row + 3, tools.gridSize);

			for (int y = minCol; y < maxCol && flatArea; y++) {
				int colIndex = y * (tools.gridSize + 1);
				for (int x = minRow; x < maxRow; x++) {
					if (tools.vertices[colIndex + x].y > 10) {
						flatArea = false;
						break;
					}
				}
			}

			if (flatArea) {
				StartCoroutine(MakeHill((float) col / tools.gridSize, (float) row / tools.gridSize, size, duration, false));
				return;
			}
		}
	}

	IEnumerator MakeHill(float x, float y, float r, float duration, bool hill) {
		//Pick points around the hill / lake
		float[] xPoints = new float[10];
		float[] yPoints = new float[10];
		float offset = r / tools.gridSize;
		for (int i = 0; i < xPoints.Length; i++) {
			xPoints[i] = x + Random.Range(-offset, offset);
			yPoints[i] = y + Random.Range(-offset, offset);
		}

		//Raise each point until the target time has passed
		float endTime = Time.time + duration;
		while (Time.time < endTime) {
			for (int i = 0; i < xPoints.Length; i++) {
				tools.RaiseTerrain(xPoints[i], yPoints[i], r, r / duration * Time.deltaTime * (hill ? 2 : -2) / xPoints.Length, false);
			}

			geometryChanged = true;
			yield return null;
		}

		if (hill) {
			Manager("hill");
		} else {
			Manager("lake");
		}
	}

	void Hill(float duration = 12, float size = 25) {
		StartCoroutine(MakeHill(Random.Range(0f, 1f), Random.Range(0f, 1f), size, duration, true));
	}

	IEnumerator GrowFlora(GameObject flora, float duration, float size) {
		float endTime = Time.time + duration;
		flora.transform.localScale = Vector3.zero;

		while (Time.time < endTime) {
			float scale = size / duration * Time.deltaTime;
			flora.transform.localScale += new Vector3(scale, scale, scale);
			yield return null;
		}
	}

	IEnumerator Flora(float duration = 12) {
		float density = floraDensitySlider.value / 1000;

		foreach (Vector3 vertex in tools.vertices) {
			if (Random.Range(0f, 1f) > density || vertex.y >= 50) {
				continue;
			}
			GameObject o;
			if (vertex.y < 0 || vertex.y > 40) {
				o = Instantiate(rock, vertex, Quaternion.identity);
				StartCoroutine(GrowFlora(o, duration, 0.5f));
			} else {
				float random = Random.Range(0f, 1f);

				if (random < 0.1f) {
					o = Instantiate(rock, vertex, Quaternion.identity);
					StartCoroutine(GrowFlora(o, duration, 0.5f));
				} else if (random < 0.1f + Mathf.Lerp(0.3f, 0.2f, vertex.y / 40)) {
					o = Instantiate(bush, vertex, Quaternion.identity);
					StartCoroutine(GrowFlora(o, duration, 1));
				} else if (random < 0.1f + Mathf.Lerp(0.8f, 0.2f, vertex.y / 40)) {
					o = Instantiate(tree, vertex, Quaternion.identity);
					StartCoroutine(GrowFlora(o, duration, 0.5f));
				} else {
					o = Instantiate(deadTree, vertex, Quaternion.identity);
					StartCoroutine(GrowFlora(o, duration, 0.5f));
				}
			}
			flora.Add(o);
			yield return null;
		}
	}

	// //Rivers seem to be ugly to implement and we can't actually indicate water above sea level anyways.
	IEnumerator River(float duration = 12, float size = 25) {
		Dictionary<int, int> squaresVisited = new Dictionary<int, int>();

		int maxIndex = 0;
		float maxHeight = float.MinValue;

		float stepLength = 1.0f / tools.gridSize;

		for (int attempt = 0; attempt < 20; attempt++) {
			int index = Random.Range(0, tools.vertices.Length);
			if (tools.vertices[index].y > maxHeight) {
				maxHeight = tools.vertices[index].y;
				maxIndex = index;
			}
		}

		int col = maxIndex % (tools.gridSize + 1);
		int row = maxIndex / (tools.gridSize + 1);

		float coordU = (float) col / tools.gridSize;
		float coordV = (float) row / tools.gridSize;

		List<float> uCoordinates = new List<float>();
		List<float> vCoordinates = new List<float>();

		uCoordinates.Add(coordU);
		vCoordinates.Add(coordV);

		bool running = true;
		do {
			int index = col * (tools.gridSize + 1) + row;
			Vector3 normalBotLeft = tools.normals[index];
			Vector3 normalTopLeft = tools.normals[index + tools.gridSize + 1];
			Vector3 normalTopRight = tools.normals[index + tools.gridSize + 1 + 1];
			Vector3 normalBotRight = tools.normals[index + 1];

			Vector3 surroundingNormal = normalBotLeft + normalTopLeft + normalTopRight + normalBotRight;

			surroundingNormal = (new Vector3(surroundingNormal.x, 0, surroundingNormal.z)).normalized * stepLength;

			coordU += surroundingNormal.x;
			coordV += surroundingNormal.z;

			uCoordinates.Add(coordU);
			vCoordinates.Add(coordV);

			col = (int) coordU * tools.gridSize;
			row = (int) coordV * tools.gridSize;

			// Check for cycles
			int key = col + row * tools.gridSize;
			if (squaresVisited.ContainsKey(key)) {
				squaresVisited.Add(key, 1 + squaresVisited[key]);
				if (squaresVisited[key] > 2) { // We've very likely reached a cycle
					running = false;
				}
			} else {
				squaresVisited.Add(key, 1);
			}

			if (col > tools.gridSize || row > tools.gridSize || col < 0 || row < 0) {
				running = false;
			}
		} while (running);

		for (int i = 0; i < uCoordinates.Count; i++) {
			tools.RaiseTerrain(uCoordinates[i], vCoordinates[i], size, size / duration * Time.deltaTime, true);
		}

		yield return null;
	}

	/*
	public void GenerateRiver(Vector3 location, float length, float depth) {
		int gridSize = tools.gridSize;
		int columnIndex;
		int lineIndex;
		List<Vector3> locationsToBeRaised = new List<Vector3>();

		for (int i = 0; i < length; i++) {
			// Find the (approximate) square in which the point is in
			columnIndex = (int) (location.x * gridSize);
			Debug.Log(columnIndex);
			lineIndex = (int) (location.z * gridSize);
			Debug.Log(lineIndex);
			Vector3 surroundingNormal = new Vector3(0, 0, 0);
			// Calculate direction of river
			if (columnIndex + 1 < gridSize && lineIndex + 1 < gridSize) {
				// Calculate normals of surrounding vertices
				// Bottom left
				int verticeIndex = lineIndex * gridSize + columnIndex;
				surroundingNormal += normals[verticeIndex];
				Debug.Log(normals[verticeIndex]);

				// Bottom right
				verticeIndex++;
				surroundingNormal += normals[verticeIndex];

				// Top left
				verticeIndex -= gridSize + 1;
				surroundingNormal += normals[verticeIndex];

				// Top right
				verticeIndex++;
				surroundingNormal += normals[verticeIndex];
				Debug.Log(surroundingNormal);
			} else { // TO-DO: OTHER CASES
				surroundingNormal = new Vector3(1.0f, 0, 0);
				if (columnIndex + 1 < gridSize) {
				}
			}
			Vector3 riverDirection = new Vector3(surroundingNormal.x, 0, surroundingNormal.z);
			Debug.Log(riverDirection);
			riverDirection = Vector3.Normalize(riverDirection);
			
		if (riverDirection.x * riverDirection.x + riverDirection.z * riverDirection.z < 0.0000001f) {
				RaiseTerrainHill(new Vector3(0, 0, 0), 30.0f, 90.0f);
				break;
			}

			locationsToBeRaised.Add(location);
			location = location + riverDirection;
		}
		for (int i = 0; i < locationsToBeRaised.Count; i++) {
			//RaiseTerrainHill(new Vector3(0, 0, 0), 30.0f, 90.0f);
			RaiseTerrainHill(locationsToBeRaised[i], 3, -depth);
		}
		mesh.RecalculateNormals();
	}
	*/

	IEnumerator Flora() {
		yield return null;
	}
}


