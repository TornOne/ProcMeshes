using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TerrainMeshTools))]
public class TerrainGenerator : MonoBehaviour {
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
		}

		else if (message == "mountain") {
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
		}

		else if (message == "lake") {
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
		}

		else if (message == "hill") {
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
		}

		else if (message == "river") {
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

	// //Rivers seem to be ugly to implement and we can't actually indicate water above sea level anyways.
	IEnumerator River(float duration = 20, float size = 4.0f) {

		//	tools.RecalculateNormals();
		tools.GetNormals();

		float momentum = 0.9f; // How much the river wants to keep going in the same direction
		float turbulence = 0.9f; // How much the river turns randomly from the slope's normal

		float endTime = Time.time + duration;

		Dictionary<int, int> squaresVisited = new Dictionary<int, int>();

		int maxIndex = 0;
		float maxHeight = float.MinValue;

		float stepLength = 1.0f / tools.gridSize;
		float raiseTerrainStepLength = stepLength / 10; // The smaller this is, the smoother the rivers are


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
		List<float> pointNormalYCoord = new List<float>();

		uCoordinates.Add(coordU);
		vCoordinates.Add(coordV);
		pointNormalYCoord.Add(tools.normals[maxIndex].y);

		bool running = true;
		Vector3 momentumVector = new Vector3(0, 0, 0);


		do {
			int index = col + row * (tools.gridSize + 1);

			Vector3 normalBotLeft = tools.normals[index];
			Vector3 normalTopLeft = tools.normals[index + tools.gridSize + 1];
			Vector3 normalTopRight = tools.normals[index + tools.gridSize + 1 + 1];
			Vector3 normalBotRight = tools.normals[index + 1];
			// Debug.Log(normalBotLeft + " " + normalBotRight + " " + normalTopLeft + " " + normalTopRight);

			Vector3 surroundingNormal = normalBotLeft + normalTopLeft + normalTopRight + normalBotRight;

			Vector3 surroundingNormalPlanar = new Vector3(surroundingNormal.x, 0, surroundingNormal.z);

			surroundingNormalPlanar = (surroundingNormalPlanar).normalized * stepLength;

			if (Vector3.Dot(momentumVector, surroundingNormalPlanar) > -stepLength * stepLength / 4) { // Keep momentum as long as we're not going too steep a hill
				surroundingNormalPlanar += momentumVector;
			}


			// Apply turbulence:
			float randomTurn = Random.Range(-turbulence, turbulence) * surroundingNormal.y / 4;
			surroundingNormalPlanar += randomTurn * Vector3.Cross(surroundingNormalPlanar, new Vector3(0, 1, 0));
			// Debug.Log(randomTurn * Vector3.Cross(surroundingNormalPlanar, new Vector3(0, 1, 0)));


			momentumVector = surroundingNormalPlanar * momentum * (surroundingNormal.y / 4) * (surroundingNormal.y / 4);

			coordU += surroundingNormalPlanar.x;
			coordV += surroundingNormalPlanar.z;

			pointNormalYCoord.Add(surroundingNormal.y / 4);
			uCoordinates.Add(coordU);
			vCoordinates.Add(coordV);

			col = (int) (coordU * tools.gridSize);
			row = (int) (coordV * tools.gridSize);

			// Check for cycles
			int key = col + row * tools.gridSize;
			if (squaresVisited.ContainsKey(key)) {
				int value = squaresVisited[key];
				squaresVisited.Remove(key);
				squaresVisited.Add(key, 1 + value);
				if (squaresVisited[key] > 2) { // We've very likely reached a cycle
					running = false;
				}
			} else {
				squaresVisited.Add(key, 1);
			}

			if (col > tools.gridSize || row > tools.gridSize || col < 0 || row < 0) { // Check if we've run out of the map
				running = false;
			}
		} while (running);
	

		while (Time.time < endTime) {
			for (int i = 0; i < uCoordinates.Count - 1; i++) {
				int raiseTerrainSteps = (int) (Mathf.Sqrt(Mathf.Pow(uCoordinates[i] - uCoordinates[i + 1], 2)
														+ Mathf.Pow(vCoordinates[i] - vCoordinates[i + 1], 2))
														/ raiseTerrainStepLength) + 2; // Add some number just so we don't get 0 steps
				float nextPointVecUCoord = (uCoordinates[i + 1] - uCoordinates[i]) / raiseTerrainSteps;
				float nextPointVecVCoord = (vCoordinates[i + 1] - vCoordinates[i]) / raiseTerrainSteps;
				
				for (int j = 0; j < raiseTerrainSteps - 1; j++) { // Don't raise terrain at the endpoint
					tools.RaiseTerrainRiver(uCoordinates[i] + j * nextPointVecUCoord, vCoordinates[i] + j * nextPointVecVCoord, 3 + 3 * Mathf.Pow(pointNormalYCoord[i], 2), -Mathf.Pow(1 - pointNormalYCoord[i] / 2, 1) * size / duration * Time.deltaTime / raiseTerrainSteps, true);
				}
				// float underSeaLevelMultiplier = 
				// tools.RaiseTerrainRiver(uCoordinates[i], vCoordinates[i], 1 + 3 * Mathf.Pow(pointNormalYCoord[i], 2), -Mathf.Pow(1 - pointNormalYCoord[i] / 2, 1) * size / duration * Time.deltaTime, false);
				
			}
			geometryChanged = true;
			yield return null;
		}
	
		float lastU = uCoordinates[uCoordinates.Count - 1];
		float lastV = vCoordinates[vCoordinates.Count - 1];

		if (0 < lastU && lastU < 1 && 0 < lastV && lastV < 1) { // If we haven't run off the map, let's make a lake
			// MAKE HILL HERE
			//StartCoroutine(MakeHill(lastU, lastV, size, duration, false));
		} 

		Manager("river");
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
}
