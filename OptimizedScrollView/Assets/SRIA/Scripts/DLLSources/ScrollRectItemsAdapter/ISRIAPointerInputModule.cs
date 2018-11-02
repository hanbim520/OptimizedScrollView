using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter
{
	/// <summary>
	/// Used if you have your own custom input module. Otherwise, see <see cref="SRIAStandaloneInputModule"/> and <see cref="SRIATouchInputModule"/>.
	/// Required if building for UWP (WSA), but recommended in all cases: your InputModule should implement this interface and return the "PointerInputModule.m_PointerData" field (like <see cref="SRIAStandaloneInputModule"/> does)</summary>
	public interface ISRIAPointerInputModule
	{
		Dictionary<int, PointerEventData> GetPointerEventData();
	}
}