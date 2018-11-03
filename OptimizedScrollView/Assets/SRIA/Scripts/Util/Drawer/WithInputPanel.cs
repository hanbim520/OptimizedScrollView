
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

namespace UnityEngine.UI.Extension.Tools.Util.Drawer
{
	public class WithInputPanel : MonoBehaviour
	{
		public InputField inputField;

		public float InputFieldValueAsFloat { get { return float.Parse(inputField.text); } }
		public int InputFieldValueAsInt { get { return int.Parse(inputField.text); } }
	}
}
