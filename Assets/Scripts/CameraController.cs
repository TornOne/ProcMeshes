using UnityEngine;

public class CameraController : MonoBehaviour {
	public float speed, rotSpeed;

	void Update () {
		transform.Translate(new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("UpDown"), Input.GetAxisRaw("Vertical")) * (Time.deltaTime * speed));
		transform.localRotation = Quaternion.Euler(transform.localEulerAngles + new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X")) * (Time.deltaTime * rotSpeed));
	}
}
