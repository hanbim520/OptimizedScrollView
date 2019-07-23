using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEngine.UI.Extension
{
    public static class RectTransformHelper
	{
		static Dictionary<int, Func<RectTransform, RectTransform, float>> _GetInsetFromParentEdge_MappedActions =
			new Dictionary<int, Func<RectTransform, RectTransform, float>>()
		{
			{ (int)RectTransform.Edge.Bottom, GetInsetFromParentBottomEdge },
			{ (int)RectTransform.Edge.Top, GetInsetFromParentTopEdge },
			{ (int)RectTransform.Edge.Left, GetInsetFromParentLeftEdge },
			{ (int)RectTransform.Edge.Right, GetInsetFromParentRightEdge }
		};

		static Dictionary<int, Action<RectTransform, RectTransform, float, float>> _SetInsetAndSizeFromParentEdgeWithCurrentAnchors_MappedActions =
			new Dictionary<int, Action<RectTransform, RectTransform, float, float>>()
		{
			{
                (int)RectTransform.Edge.Bottom,
				(child, parentHint, newInset, newSize) => {
					var offsetChange = newInset - child.GetInsetFromParentBottomEdge(parentHint);
					var offsetMin = new Vector2(child.offsetMin.x, child.offsetMin.y + offsetChange); // need to store it before modifying anything, because the offsetmax will change the offsetmin and vice-versa
					child.offsetMax = new Vector2(child.offsetMax.x, child.offsetMax.y + (newSize - child.rect.height + offsetChange));
					child.offsetMin = offsetMin;
				}
			},
			{
            (int)RectTransform.Edge.Top,
				(child, parentHint, newInset, newSize) => {
					var offsetChange = newInset - child.GetInsetFromParentTopEdge(parentHint);
					var offsetMax = new Vector2(child.offsetMax.x, child.offsetMax.y - offsetChange);
					child.offsetMin = new Vector2(child.offsetMin.x, child.offsetMin.y - (newSize - child.rect.height + offsetChange));
					child.offsetMax = offsetMax;
				}
			},
			{
                (int)RectTransform.Edge.Left,
				(child, parentHint, newInset, newSize) => {
					var offsetChange = newInset - child.GetInsetFromParentLeftEdge(parentHint);
					var offsetMin = new Vector2(child.offsetMin.x + offsetChange, child.offsetMin.y);
					child.offsetMax = new Vector2(child.offsetMax.x + (newSize - child.rect.width + offsetChange), child.offsetMax.y);
					child.offsetMin = offsetMin;
				}
			},
			{
                (int)RectTransform.Edge.Right,
				(child, parentHint, newInset, newSize) => {
					var offsetChange = newInset - child.GetInsetFromParentRightEdge(parentHint);
					var offsetMax = new Vector2(child.offsetMax.x - offsetChange, child.offsetMax.y);
					child.offsetMin = new Vector2(child.offsetMin.x - (newSize - child.rect.width + offsetChange), child.offsetMin.y);
					child.offsetMax = offsetMax;
				}
			}
		};


		public static float GetWorldTop(this RectTransform rt)
        { return rt.position.y + (1f - rt.pivot.y) * rt.rect.height; }

        public static float GetWorldBottom(this RectTransform rt)
        { return rt.position.y - rt.pivot.y * rt.rect.height; }

        public static float GetWorldLeft(this RectTransform rt)
        { return rt.position.x - rt.pivot.x * rt.rect.width; }

        public static float GetWorldRight(this RectTransform rt)
        { return rt.position.x + (1f - rt.pivot.x) * rt.rect.width; }

		public static float GetWorldSignedHorDistanceBetweenCustomPivots(
			this RectTransform rt,
			float customPivotOnThisRect01,
			RectTransform other,
			float customPivotOnOtherRect01
		){
			// Horizontal distance
			float pointOnThisRect_WorldSpace = rt.GetWorldRight() - (1f - customPivotOnThisRect01) * rt.rect.width;
			float pointOnOtherRect_WorldSpace = other.GetWorldRight() - (1f - customPivotOnOtherRect01) * other.rect.width;
			return pointOnOtherRect_WorldSpace - pointOnThisRect_WorldSpace;
		}

		public static float GetWorldSignedVertDistanceBetweenCustomPivots(
			this RectTransform rt,
			float customPivotOnThisRect01,
			RectTransform other,
			float customPivotOnOtherRect01
		){
			// Vertical distance
			float pointOnThisRect_WorldSpace = rt.GetWorldTop() - (1f - customPivotOnThisRect01) * rt.rect.height;
			float pointOnOtherRect_WorldSpace = other.GetWorldTop() - (1f - customPivotOnOtherRect01) * other.rect.height;
			return pointOnOtherRect_WorldSpace - pointOnThisRect_WorldSpace;
		}

		public static float GetInsetFromParentTopEdge(this RectTransform child, RectTransform parentHint)
        {
            float parentPivotYDistToParentTop = (1f - parentHint.pivot.y) * parentHint.rect.height;
            float childLocPosY = child.localPosition.y;

            return parentPivotYDistToParentTop - child.rect.yMax - childLocPosY;
        }

        public static float GetInsetFromParentBottomEdge(this RectTransform child, RectTransform parentHint)
        {
            float parentPivotYDistToParentBottom = parentHint.pivot.y * parentHint.rect.height;
            float childLocPosY = child.localPosition.y;

            return parentPivotYDistToParentBottom + child.rect.yMin + childLocPosY;
        }

        public static float GetInsetFromParentLeftEdge(this RectTransform child, RectTransform parentHint)
        {
            float parentPivotXDistToParentLeft = parentHint.pivot.x * parentHint.rect.width;
            float childLocPosX = child.localPosition.x;

            return parentPivotXDistToParentLeft + child.rect.xMin + childLocPosX;
        }

        public static float GetInsetFromParentRightEdge(this RectTransform child, RectTransform parentHint)
        {
            float parentPivotXDistToParentRight = (1f - parentHint.pivot.x) * parentHint.rect.width;
            float childLocPosX = child.localPosition.x;

            return parentPivotXDistToParentRight - child.rect.xMax - childLocPosX;
        }


        public static float GetInsetFromParentEdge(this RectTransform child, RectTransform parentHint, RectTransform.Edge parentEdge)
		{ return _GetInsetFromParentEdge_MappedActions[(int)parentEdge](child, parentHint); }

        public static void SetSizeFromParentEdgeWithCurrentAnchors(this RectTransform child, RectTransform.Edge fixedEdge, float newSize)
        {
            var par = child.parent as RectTransform;
            child.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(par, fixedEdge, child.GetInsetFromParentEdge(par, fixedEdge), newSize);
        }

        public static void SetSizeFromParentEdgeWithCurrentAnchors(this RectTransform child, RectTransform parentHint, RectTransform.Edge fixedEdge, float newSize)
        {
            child.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(parentHint, fixedEdge, child.GetInsetFromParentEdge(parentHint, fixedEdge), newSize);
        }

        public static void SetInsetAndSizeFromParentEdgeWithCurrentAnchors(this RectTransform child, RectTransform.Edge fixedEdge, float newInset, float newSize)
        {
            child.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(child.parent as RectTransform, fixedEdge, newInset, newSize);
        }

		public static void SetInsetAndSizeFromParentEdgeWithCurrentAnchors(this RectTransform child, RectTransform parentHint, RectTransform.Edge fixedEdge, float newInset, float newSize)
        { _SetInsetAndSizeFromParentEdgeWithCurrentAnchors_MappedActions[(int)fixedEdge](child, parentHint, newInset, newSize); }



		public static void MatchParentSize(this RectTransform rt)
		{
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.sizeDelta = Vector3.zero; // same size as anchors
			rt.pivot = Vector2.one * .5f; // center pivot
			rt.anchoredPosition = Vector3.zero; // centered at the anchors' center
		}
	}
}
