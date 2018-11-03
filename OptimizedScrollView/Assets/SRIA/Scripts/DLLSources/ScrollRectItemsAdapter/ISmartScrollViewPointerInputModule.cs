using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace UnityEngine.UI.Extension.Tools
{
	public interface ISmartScrollViewPointerInputModule
    {
		Dictionary<int, PointerEventData> GetPointerEventData();
	}
}