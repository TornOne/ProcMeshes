using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
	TerrainMeshTools tools;

	void Start() {
		tools = GetComponent<TerrainMeshTools>();

		tools.ResetMesh();
		StartCoroutine(MountainRange());
		StartCoroutine(MountainRange());
		StartCoroutine(MountainRange());
	}

	void Update() {
		tools.SetVertices();
		tools.RecalculateNormals();
		tools.Colorize();
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
			float offsetMultiplier = size / length / 500;
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

			yield return null;
		}
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
}
