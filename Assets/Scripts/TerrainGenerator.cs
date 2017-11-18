using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
	TerrainMeshTools tools;

	void Start() {
		tools = GetComponent<TerrainMeshTools>();

		tools.ResetMesh();
		tools.RaiseTerrain(0.3f, 0.5f, 20f, 10f, false);
	}

	void Update() {
		tools.RaiseTerrain(0.5f, 0.5f, 20f, Time.deltaTime, true);
		tools.RaiseTerrain(0.1f, 0.5f, Random.Range(1f, 30f), -Time.deltaTime);

		tools.SetVertices();
		tools.RecalculateNormals();
		tools.Colorize();
	}
}
