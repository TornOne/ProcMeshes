using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
	TerrainMeshTools tools;

	void Start() {
		tools = GetComponent<TerrainMeshTools>();

		tools.ResetMesh();
		tools.RaiseTerrainHill(new Vector3(-60.0f, 0, 0), 20.0f, 8.0f);
	}

	void Update() {
		tools.RaiseTerrainHill(new Vector3(0, 0, 0), Random.Range(1.0f, 30.0f), 0.3f);
		tools.RaiseTerrain(new Vector3(-120.0f, 0, 0), Random.Range(1.0f, 30.0f), -0.5f);
		tools.Colorize();
	}
}
