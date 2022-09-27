using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Magikus {
	public class smartSlider : MonoBehaviour
	{
		public string labelFormat = "value: #";
		public string valueFormat = "";
		public float minUnit = 1f;

		private Slider slider;
		private Button addBtn;
		private Button subBtn;
		private Button oneBtn;
		private Button ceroBtn;
		private Button signBtn;
		private TMP_Text label;

		private void Awake()
		{
			label = gameObject.transform.GetChild(0).GetComponent<TMP_Text>();
			slider = gameObject.transform.GetChild(1).GetComponent<Slider>();
			addBtn = gameObject.transform.GetChild(2).GetComponent<Button>();
			subBtn = gameObject.transform.GetChild(3).GetComponent<Button>();
			signBtn = gameObject.transform.GetChild(4).GetComponent<Button>();
			oneBtn = gameObject.transform.GetChild(5).GetComponent<Button>();
			ceroBtn = gameObject.transform.GetChild(6).GetComponent<Button>();

			if (slider != null) { slider.onValueChanged.AddListener((value) => { UpdateLabel(); }); }
			if (addBtn != null) { addBtn.onClick.AddListener(() => { SetValue(slider.value + minUnit); }); }
			if (subBtn != null) { subBtn.onClick.AddListener(() => { SetValue(slider.value - minUnit); });}
			if (signBtn != null) { signBtn.onClick.AddListener(() => { SetValue(-slider.value); });}
			if (oneBtn != null) { oneBtn.onClick.AddListener(() => { SetValue(1); });}
			if (ceroBtn != null) { ceroBtn.onClick.AddListener(() => { SetValue(0); });}

			UpdateLabel();
		}

		public float GetValue() { return (slider == null) ? float.NaN : slider.value; }

		public void SetValue(float value) 
		{
			if (slider == null) { return; }
			slider.value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
			UpdateLabel();
		}

		public void UpdateLabel() 
		{
			if (label == null) { return; }
			label.text = labelFormat.Replace("#", slider.value.ToString(valueFormat));
		}
    }
}
