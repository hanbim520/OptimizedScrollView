using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace UnityEngine.UI.Extension.ScrollRectItemsAdapter
{
	/// <summary>Required if building for UWP (WSA), but recommended in all cases: Replace your StandaloneInputModule with this one. If you have your own input module, then see <see cref="ISRIAPointerInputModule"/></summary>
	public class SRIAStandaloneInputModule : StandaloneInputModule, ISmartScrollViewPointerInputModule
    {
		public Dictionary<int, PointerEventData> GetPointerEventData() { return m_PointerData; }
	}
}