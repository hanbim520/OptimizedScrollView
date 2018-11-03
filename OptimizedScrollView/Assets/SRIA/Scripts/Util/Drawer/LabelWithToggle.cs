
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

namespace UnityEngine.UI.Extension.Tools.Util.Drawer
{
	public class LabelWithToggle : MonoBehaviour
	{
		public Text labelText;
		public Toggle toggle;


		public LabelWithToggle Init(string text = "")
		{
			labelText.text = text;

			return this;
		}
	}
}
