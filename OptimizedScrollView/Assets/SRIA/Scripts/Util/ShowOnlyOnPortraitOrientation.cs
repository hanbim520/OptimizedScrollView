using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.UI.Extension;

namespace UnityEngine.UI.Extension.Tools.Util
{
    public class ShowOnlyOnPortraitOrientation : MonoBehaviour
    {
		public GameObject target;

		void Update()
		{
			if (Screen.height > Screen.width) // portrait
			{
				if (!target.activeSelf)
					target.SetActive(true);
			}
			else
			{
				if (target.activeSelf)
					target.SetActive(false);
			}
		}
	}
}