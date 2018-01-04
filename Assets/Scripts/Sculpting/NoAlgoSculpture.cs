using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoAlgoSculpture : MonoBehaviour {
	public Vector3Int segments;
	public Vector3 size;
	public GameObject cube;

	void Start () {
		for (int y = 0; y < segments.y; y++) {
			for (int z = 0; z < segments.z; z++) {
				for (int x = 0; x < segments.x; x++) {
					Instantiate(cube, new Vector3(x - segments.x * 0.5f, y + 0.5f, z - segments.z * 0.5f), Quaternion.identity, transform);
				}
			}
		}

		transform.localScale = new Vector3(size.x / segments.x, size.y / segments.y, size.z / segments.z);
	}

	void Update () {
		
	}
}
