using UnityEngine;

public class CameraController : MonoBehaviour {
	public GameObject menu;
	bool menuActive = false;
	public float speed, rotSpeed;

	void Start() {
		Cursor.lockState = CursorLockMode.Locked;
	}

	void Update() {
		if (!menuActive) {
			transform.Translate(new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("UpDown"), Input.GetAxisRaw("Vertical")) * (Time.deltaTime * speed));
			transform.Rotate(Vector3.right * Time.deltaTime * rotSpeed * -Input.GetAxisRaw("Mouse Y"));
			transform.Rotate(Vector3.up * Time.deltaTime * rotSpeed * Input.GetAxisRaw("Mouse X"), Space.World);
		}

		if (Input.GetButtonDown("Cancel")) {
			menuActive = !menuActive;
			menu.SetActive(menuActive);

			if (menuActive) {
				Cursor.lockState = CursorLockMode.None;
			} else {
				Cursor.lockState = CursorLockMode.Locked;
			}
		}
	}
}
