using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter
{
	/// <summary>Required if building for UWP (WSA), but recommended in all cases: Replace your TouchInputModule(if exists) with this one.</summary>
	public class SRIATouchInputModule : TouchInputModule, ISRIAPointerInputModule
	{
		public Dictionary<int, PointerEventData> GetPointerEventData() { return m_PointerData; }
	}
}