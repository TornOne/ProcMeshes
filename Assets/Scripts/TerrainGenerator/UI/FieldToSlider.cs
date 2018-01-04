using UnityEngine;
using UnityEngine.UI;

public class FieldToSlider : MonoBehaviour {
	public Slider slider;

	public void setSlider() {
		slider.value = int.Parse(GetComponent<InputField>().text);
	}
}
