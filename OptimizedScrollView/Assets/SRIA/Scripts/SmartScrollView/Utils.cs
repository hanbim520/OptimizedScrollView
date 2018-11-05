using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extension.Tools
{
    public static class Utils
	{
		public static Vector2? SetPointerEventDistanceToZero(PointerEventData pev)
		{
			var delta = pev.delta;
			pev.dragging = false;
			return delta;
		}

		public static PointerEventData GetOriginalPointerEventDataByDrag(GameObject pointerDragGOToLookFor)
		{
			if (EventSystem.current.currentInputModule == null)
				return null;

			var eventSystemAsPointerInputModule = EventSystem.current.currentInputModule as PointerInputModule;
			if (eventSystemAsPointerInputModule == null)
				throw new InvalidOperationException("currentInputModule is not a PointerInputModule");

			var asCompatInterface = eventSystemAsPointerInputModule as ISmartScrollViewPointerInputModule;
			Dictionary<int, PointerEventData> pointerEvents;
			if (asCompatInterface == null)
			{
#if UNITY_WSA || UNITY_WSA_10_0 
				throw new UnityException("Your InputModule should extend ISmartScrollViewPointerInputModule. See Instructions.pdf");
#else
                pointerEvents = eventSystemAsPointerInputModule
					.GetType()
					.GetField("m_PointerData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
					.GetValue(eventSystemAsPointerInputModule)
					as Dictionary<int, PointerEventData>;
#endif
			}
			else
				pointerEvents = asCompatInterface.GetPointerEventData();

			foreach (var pointer in pointerEvents.Values)
				if (pointer.pointerDrag == pointerDragGOToLookFor)
					return pointer;

			return null;
		}

		public static Color GetRandomColor(bool fullAlpha = false)
		{ return new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), fullAlpha ? 1f : UnityEngine.Random.Range(0f, 1f)); }
	}
}
