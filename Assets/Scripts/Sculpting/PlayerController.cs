using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
	public float gravity = 9.80665f;
	public float speed, rotSpeed;
	float yVelocity = 0;
	public GameObject camera;
	CharacterController controller;

	void Start() {
		controller = GetComponent<CharacterController>();
	}

	void Update() {
		transform.localPosition = new Vector3(transform.localPosition.x, 1, transform.localPosition.z);
		camera.transform.Rotate(Time.deltaTime * -rotSpeed * Input.GetAxisRaw("Mouse Y"), 0, 0);
		transform.Rotate(0, Time.deltaTime * rotSpeed * Input.GetAxisRaw("Mouse X"), 0);
		controller.Move(transform.TransformDirection(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * speed * (Input.GetButton("Sprint") ? 2 : 1) * Time.deltaTime);
	}
}
