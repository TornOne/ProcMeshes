using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
	public float speed, rotSpeed;
	CharacterController controller;

	public GameObject cam;
	bool menuActive = false;

	void Start() {
		controller = GetComponent<CharacterController>();
		Cursor.lockState = CursorLockMode.Locked;
	}

	void Update() {
		if (Input.GetButtonDown("Cancel")) {
			menuActive = !menuActive;
			//menu.SetActive(menuActive);

			if (menuActive) {
				Cursor.lockState = CursorLockMode.None;
			} else {
				Cursor.lockState = CursorLockMode.Locked;
			}
		}

		transform.localPosition = new Vector3(transform.localPosition.x, 1, transform.localPosition.z);
		cam.transform.Rotate(Time.deltaTime * -rotSpeed * Input.GetAxisRaw("Mouse Y"), 0, 0);
		transform.Rotate(0, Time.deltaTime * rotSpeed * Input.GetAxisRaw("Mouse X"), 0);
		controller.Move(transform.TransformDirection(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * speed * (Input.GetButton("Sprint") ? 2 : 1) * Time.deltaTime);
	}
}
