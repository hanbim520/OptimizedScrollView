using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter
{
    public static class Utils
	{
		/// <summary>Returns the delta reported in the <see cref="PointerEventData"/></summary>
		//public static Vector2? ForceSetPointerEventDistanceToZero(GameObject pointerDragGOToLookFor)
		//{
		//	var pev = GetPointerEventDataWithPointerDragGO(pointerDragGOToLookFor);
		//	if (pev == null)
		//		return null;

		//	return ForceSetPointerEventDistanceToZero(pev);
		//}

		public static Vector2? ForceSetPointerEventDistanceToZero(PointerEventData pev)
		{
			// Modify the original pointer to look like it was pressed at the current position and it didn't move
			//PointerEventData originalPED = null;
			//Debug.Log("delta="+pointer.delta + ", scrollDelta=" + pointer.scrollDelta + ", dragging=" + pointer.dragging + ", pressPosition=" + pointer.pressPosition + ", position=" + pointer.position);
			//pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;
			//pointer.pressPosition = pointer.position;
			var delta = pev.delta;
			//pointer.delta = Vector2.zero;
			pev.dragging = false;
			//pointer.scrollDelta = Vector2.zero;

			//// TODO test
			////pointer.Use();
			////originalPED = pointer;
			//break;
			return delta;
		}

		/// <summary> This is needed because the PointerEventData received in OnDrag, OnEndDrag etc. is a copy of the original one </summary>
		public static PointerEventData GetOriginalPointerEventDataWithPointerDragGO(GameObject pointerDragGOToLookFor)
		{
			// Current input module not initialized yet
			if (EventSystem.current.currentInputModule == null)
				return null;

			var eventSystemAsPointerInputModule = EventSystem.current.currentInputModule as PointerInputModule;
			if (eventSystemAsPointerInputModule == null)
				throw new InvalidOperationException("currentInputModule is not a PointerInputModule");

			var asCompatInterface = eventSystemAsPointerInputModule as ISRIAPointerInputModule;
			Dictionary<int, PointerEventData> pointerEvents;
			if (asCompatInterface == null)
			{
#if UNITY_WSA || UNITY_WSA_10_0 // WSA uses .net core, which doesn't have reflection. in this case we expect the current input module to implement ISRIAPointerInputModule
				throw new UnityException("Your InputModule should extend ISRIAPointerInputModule. See Instructions.pdf");
#else
				// Dig into reflection and get the original pointer data
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
