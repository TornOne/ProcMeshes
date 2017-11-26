using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoSelecter : MonoBehaviour {
	public void SwitchScene(int scene) {
		SceneManager.LoadScene(scene);
	}
}
