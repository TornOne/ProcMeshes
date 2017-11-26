using UnityEngine;
using UnityEngine.UI;

public class SliderToField : MonoBehaviour {
	public InputField field;

	public void setField() {
		field.text = GetComponent<Slider>().value.ToString();
	}
}
