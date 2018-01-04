using UnityEngine;

public class DestroyOnClick : MonoBehaviour {
	void OnMouseDown() {
		Destroy(gameObject);
	}
}
