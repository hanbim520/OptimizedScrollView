using frame8.Logic.Misc.Other.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEngine.UI.Extension.ScrollRectItemsAdapter
{
	/// <summary>
	/// Contains commonly used members so that an <see cref="SmartScrollView{TParams, SmartScrollViewItem}"/> instance 
	/// can be referenced abstractly (since instances of derived generic classes cannot be referenced by a variable of base type).
	/// </summary>
	/// <seealso cref="IScrollRectProxy"/>
	public interface ISmartScrollView : IScrollRectProxy
	{
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.ItemsRefreshed"/></summary>
		event Action<int, int> ItemsRefreshed;

		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.Initialized"/></summary>
		bool Initialized { get; }
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.BaseParameters"/></summary>
		BaseParams BaseParameters { get; }
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.AsMonoBehaviour"/></summary>
		MonoBehaviour AsMonoBehaviour { get; }
		double ContentVirtualSizeToViewportRatio { get; }
		double ContentVirtualInsetFromViewportStart { get; }
		double ContentVirtualInsetFromViewportEnd { get; }
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.VisibleItemsCount"/></summary>
		int VisibleItemsCount { get; }
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.RecyclableItemsCount"/></summary>
		int RecyclableItemsCount { get; }
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.IsDragging"/></summary>
		bool IsDragging { get; }

		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.ChangeItemsCount(ItemCountChangeMode, int, int, bool, bool)"/></summary>
		void ChangeItemsCount(ItemCountChangeMode changeMode, int itemsCount, int indexIfAppendingOrRemoving = -1, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false);
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.SetVirtualAbstractNormalizedScrollPosition(double, bool)"/></summary>
		void SetVirtualAbstractNormalizedScrollPosition(double pos, bool computeVisibilityNow);
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.GetItemsCount"/></summary>
		int GetItemsCount();
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.ScrollTo(int, float, float)"/></summary>
		void ScrollTo(int itemIndex, float normalizedOffsetFromViewportStart = 0f, float normalizedPositionOfItemPivotToUse = 0f);
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.SmoothScrollTo(int, float, float, float, Func{float, bool}, bool)"/></summary>
		bool SmoothScrollTo(int itemIndex, float duration, float normalizedOffsetFromViewportStart = 0f, float normalizedPositionOfItemPivotToUse = 0f, Func<float, bool> onProgress = null, bool overrideCurrentScrollingAnimation = false);
		/// <summary>See <see cref="SmartScrollView{TParams, SmartScrollViewItem}.GetViewsHolderOfClosestItemToViewportPoint(float, float, out float)"/></summary>
		AbstractViewsHolder GetViewsHolderOfClosestItemToViewportPoint(float viewportPoint01, float itemPoint01, out float distance);
	}
}
