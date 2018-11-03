using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace UnityEngine.UI.Extension.Tools
{
	public class SmartScrollViewTouchInputModule : StandaloneInputModule, ISmartScrollViewPointerInputModule
    {
		public Dictionary<int, PointerEventData> GetPointerEventData() { return m_PointerData; }
	}
}