using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour {
	GameObject player;
	public string scene;

	void Start() {
		player = GameObject.FindGameObjectWithTag("Player");
	}

	void OnMouseDown() {
		if (Vector3.Distance(player.transform.position, transform.position) < 2) {
			SceneManager.LoadScene(scene);
		}
	}
}
