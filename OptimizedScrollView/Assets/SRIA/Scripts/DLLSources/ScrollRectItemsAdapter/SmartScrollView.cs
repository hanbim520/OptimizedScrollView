using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI.Extension;

namespace UnityEngine.UI.Extension.Tools
{
	
	public abstract partial class SmartScrollView<TParams> : MonoBehaviour, ISmartScrollView, IBeginDragHandler, IEndDragHandler
	where TParams : BaseParams
	{
		#region Configuration
		[SerializeField]
		protected TParams _Params;
		#endregion

		#region IScrollRectProxy events implementaion
		public event Action<float> ScrollPositionChanged;
        #endregion

        #region SmartScrollView events & properties implementaion
        public event Action<int, int> ItemsRefreshed;
		public bool Initialized { get; private set; }
		public BaseParams BaseParameters { get { return Parameters; } }
		public MonoBehaviour AsMonoBehaviour { get { return this; } }
		public double ContentVirtualSizeToViewportRatio { get { return _InternalState.contentPanelVirtualSize / _InternalState.viewportSize; } }
		public double ContentVirtualInsetFromViewportStart { get { return _InternalState.ContentPanelVirtualInsetFromViewportStart; } }
		public double ContentVirtualInsetFromViewportEnd { get { return _InternalState.ContentPanelVirtualInsetFromViewportEnd; } }
		public int VisibleItemsCount { get { return _VisibleItemsCount; } }
		public int RecyclableItemsCount { get { return _RecyclableItems.Count; } }
		public bool IsDragging { get; private set; }
		
		#endregion

		public TParams Parameters { get { return _Params; } }

		protected List<UILoopSmartItem> _VisibleItems;
		protected int _VisibleItemsCount;
		protected List<UILoopSmartItem> _RecyclableItems = new List<UILoopSmartItem>();

		InternalState _InternalState;
        ItemsDescriptor _ItemsDesc;
		Coroutine _SmoothScrollCoroutine;
		bool _SkipComputeVisibilityInUpdateOrOnScroll;
		bool _CorrectedPositionInCurrentComputeVisibilityPass;
		float _PrevGalleryEffectAmount;
		double _AVGVisibleItemsCount; // never reset


		#region Unity methods
		protected virtual void Awake() { Init(); }
        protected virtual void Update() { MyUpdate(); }
		protected virtual void OnDestroy() { Dispose(); }
		#endregion

		#region IScrollRectProxy methods implementaion
		public void SetNormalizedPosition(float normalizedPosition)
		{
			float abstractNormPos = _Params.scrollRect.horizontal ? 1f - normalizedPosition : normalizedPosition;
			SetVirtualAbstractNormalizedScrollPosition(abstractNormPos, true);
		}

		public float GetNormalizedPosition()
		{
			float abstractVirtNormPos = (float)_InternalState.GetVirtualAbstractNormalizedScrollPosition();
			return _Params.scrollRect.horizontal ? 1f - abstractVirtNormPos : abstractVirtNormPos;
		}

		public float GetContentSize() { return (float)Math.Min(_InternalState.contentPanelVirtualSize, float.MaxValue); }
		#endregion

		#region Unity UI events callbacks
		public void OnBeginDrag(PointerEventData eventData) { IsDragging = true; }
		public void OnEndDrag(PointerEventData eventData) { IsDragging = false; }
		#endregion

		public void Init()
		{
			Canvas.ForceUpdateCanvases();

			_Params.InitIfNeeded(this);
 
			if (_Params.scrollRect.horizontalScrollbar != null || _Params.scrollRect.verticalScrollbar != null)
				throw new UnityException("SmartScrollView only works with a "+typeof(SmartScrollViewScrollbar).Name + " component added to the Scrollbar and the ScrollRect shouldn't have any scrollbar set up in the inspector (it hooks up automatically)");

			_ItemsDesc = new ItemsDescriptor(_Params.DefaultItemSize);
            _InternalState = InternalState.CreateFromSourceParamsOrThrow(_Params, _ItemsDesc);

			_VisibleItems = new List<UILoopSmartItem>();
			_AVGVisibleItemsCount = 0;

			Refresh();
			_InternalState.UpdateLastProcessedCTVirtualInsetFromParentStart();
			SetVirtualAbstractNormalizedScrollPosition(1f, false); // scroll to start
			_Params.scrollRect.onValueChanged.AddListener(OnScrollViewValueChanged);
			
			if (ScrollPositionChanged != null)
				ScrollPositionChanged(GetNormalizedPosition());

			Initialized = true;
		}

		public virtual void Refresh() { ChangeItemsCount(ItemCountChangeMode.RESET, _ItemsDesc.itemsCount); }

		public virtual void ResetItems(int itemsCount, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
		{ ChangeItemsCount(ItemCountChangeMode.RESET, itemsCount, -1, contentPanelEndEdgeStationary, keepVelocity); }

		public virtual void InsertItems(int index, int itemsCount, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
		{ ChangeItemsCount(ItemCountChangeMode.INSERT, itemsCount, index, contentPanelEndEdgeStationary, keepVelocity); }

		public virtual void RemoveItems(int index, int itemsCount, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
		{ ChangeItemsCount(ItemCountChangeMode.REMOVE, itemsCount, index, contentPanelEndEdgeStationary, keepVelocity); }

		public virtual void ChangeItemsCount(ItemCountChangeMode changeMode, int itemsCount, int indexIfInsertingOrRemoving = -1, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
		{ ChangeItemsCountInternal(changeMode, itemsCount, indexIfInsertingOrRemoving, contentPanelEndEdgeStationary, keepVelocity); }

		public virtual int GetItemsCount() { return _ItemsDesc.itemsCount; }

		public UILoopSmartItem GeUILoopSmartItem(int vhIndex)
		{
			if (vhIndex >= _VisibleItemsCount)
				return null;
			return _VisibleItems[vhIndex];
		}

		public UILoopSmartItem GeUILoopSmartItemIfVisible(int withItemIndex)
		{
			int curVisibleIndex = 0;
			int curIndexInList;
            UILoopSmartItem curItemViewsHolder;
			for (curVisibleIndex = 0; curVisibleIndex < _VisibleItemsCount; ++curVisibleIndex)
			{
				curItemViewsHolder = _VisibleItems[curVisibleIndex];
				curIndexInList = curItemViewsHolder.ItemIndex;
				if (curIndexInList == withItemIndex)
					return curItemViewsHolder;
			}

			return null;
		}

		public UILoopSmartItem GeUILoopSmartItemIfVisible(RectTransform withRoot)
		{
            UILoopSmartItem curItemViewsHolder;
			for (int i = 0; i < _VisibleItemsCount; ++i)
			{
				curItemViewsHolder = _VisibleItems[i];
				if (curItemViewsHolder.root == withRoot)
					return curItemViewsHolder;
			}

			return null;
		}
		public virtual AbstractViewsBase GetViewsHolderOfClosestItemToViewportPoint(float viewportPoint01, float itemPoint01, out float distance)
		{
			Func<RectTransform, float, RectTransform, float, float> getDistanceFn;
			if (_Params.scrollRect.horizontal)
				getDistanceFn = RectTransformHelper.GetWorldSignedHorDistanceBetweenCustomPivots;
			else
				getDistanceFn = RectTransformHelper.GetWorldSignedVertDistanceBetweenCustomPivots;

			return GetViewsHolderOfClosestItemToViewportPoint(_VisibleItems, getDistanceFn, viewportPoint01, itemPoint01, out distance);
		}

		public virtual void ScrollTo(int itemIndex, float normalizedOffsetFromViewportStart = 0f, float normalizedPositionOfItemPivotToUse = 0f)
		{

			CancelAnimationsIfAny();

			double minContentVirtualInsetFromVPAllowed = -_InternalState.VirtualScrollableArea;
			if (minContentVirtualInsetFromVPAllowed >= 0d)
				return; // can't, because content is not bigger than viewport

			ScrollToHelper_SetContentVirtualInsetFromViewportStart(
				ScrollToHelper_GetContentStartVirtualInsetFromViewportStart_Clamped(
					minContentVirtualInsetFromVPAllowed, 
					itemIndex, 
					normalizedOffsetFromViewportStart, 
					normalizedPositionOfItemPivotToUse
				),
				false
			);

			ComputeVisibilityForCurrentPosition(false, true, false, -.1);
			ComputeVisibilityForCurrentPosition(true, true, false, +.1);
		}

		public virtual bool SmoothScrollTo(int itemIndex, float duration, float normalizedOffsetFromViewportStart = 0f, float normalizedPositionOfItemPivotToUse = 0f, Func<float, bool> onProgress = null, bool overrideCurrentScrollingAnimation = false)
		{
			if (_SmoothScrollCoroutine != null)
			{
				if (overrideCurrentScrollingAnimation)
				{

					StopCoroutine(_SmoothScrollCoroutine);
					_SmoothScrollCoroutine = null;

					_SkipComputeVisibilityInUpdateOrOnScroll = false;

				}
				else
					return false;
			}

			_SmoothScrollCoroutine = StartCoroutine(SmoothScrollProgressCoroutine(itemIndex, duration, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse, onProgress));

			return true;
		}

		public float RequestChangeItemSizeAndUpdateLayout(UILoopSmartItem withVH, float requestedSize, bool itemEndEdgeStationary = false, bool computeVisibility = true)
		{ return RequestChangeItemSizeAndUpdateLayout(withVH.ItemIndex, requestedSize, itemEndEdgeStationary, computeVisibility); }

		public float RequestChangeItemSizeAndUpdateLayout(int itemIndex, float requestedSize, bool itemEndEdgeStationary = false, bool computeVisibility = true)
		{
			CancelAnimationsIfAny();

			var skipCompute_oldValue = _SkipComputeVisibilityInUpdateOrOnScroll;
			_SkipComputeVisibilityInUpdateOrOnScroll = true;

			_Params.scrollRect.StopMovement(); 

			int cellIndex = _ItemsDesc.GetItemViewIndexFromRealIndex(itemIndex);
			var viewsHolderIfVisible = GeUILoopSmartItemIfVisible(itemIndex);
			float oldSize = _ItemsDesc[cellIndex];
			bool vrtContentPanelIsAtOrBeforeEnd = _InternalState.ContentPanelVirtualInsetFromViewportEnd >= 0d;
			if (requestedSize <= oldSize) // collapsing
			{
				if (_InternalState.ContentPanelVirtualInsetFromViewportStart >= 0d)
					itemEndEdgeStationary = false; 

				else if (vrtContentPanelIsAtOrBeforeEnd)
					itemEndEdgeStationary = true;
			}

			float resolvedSize = 
				_InternalState.ChangeItemSizeAndUpdateContentSizeAccordingly(
					viewsHolderIfVisible,
					cellIndex, 
					requestedSize, 
					itemEndEdgeStationary
				);

			float reportedScrollDelta;
			if (itemEndEdgeStationary)
				reportedScrollDelta = .1f;
			else
			{
				reportedScrollDelta = -.1f;

				if (vrtContentPanelIsAtOrBeforeEnd)
					reportedScrollDelta = .1f;
			}

			if (computeVisibility)
				ComputeVisibilityForCurrentPosition(true, true, false, reportedScrollDelta);
			if (!_CorrectedPositionInCurrentComputeVisibilityPass)
				CorrectPositionsOfVisibleItems(false);

			_SkipComputeVisibilityInUpdateOrOnScroll = skipCompute_oldValue;

			return resolvedSize;
		}

		public double GetItemVirtualInsetFromParentStart(int itemIndex)
		{ return _InternalState.GetItemVirtualInsetFromParentStartUsingcellIndex(_ItemsDesc.GetItemViewIndexFromRealIndex(itemIndex)); }
		public double GetVirtualAbstractNormalizedScrollPosition() { return _InternalState.GetVirtualAbstractNormalizedScrollPosition(); }

		public void SetVirtualAbstractNormalizedScrollPosition(double pos, bool computeVisibilityNow)
		{
			CancelAnimationsIfAny();

			var ignoreOnScroll_valueBefore = _SkipComputeVisibilityInUpdateOrOnScroll;
			_SkipComputeVisibilityInUpdateOrOnScroll = true;

			bool executed = _InternalState.SetVirtualAbstractNormalizedScrollPosition(pos);
			if (computeVisibilityNow && executed)
			{
				ComputeVisibilityForCurrentPosition(true, false, false); 
				if (!_CorrectedPositionInCurrentComputeVisibilityPass)
					CorrectPositionsOfVisibleItems(false);
			}
			else if (ScrollPositionChanged != null)
				ScrollPositionChanged(GetNormalizedPosition());
				
			_SkipComputeVisibilityInUpdateOrOnScroll = ignoreOnScroll_valueBefore;
		}

		protected virtual void CollectItemsSizes(ItemCountChangeMode changeMode, int count, int indexIfInsertingOrRemoving, ItemsDescriptor itemsDesc)
		{ itemsDesc.ReinitializeSizes(changeMode, count, indexIfInsertingOrRemoving, _Params.DefaultItemSize); }

		protected abstract UILoopSmartItem CreateViewsHolder(int itemIndex);

		protected abstract void UpdateViewsHolder(UILoopSmartItem newOrRecycled);

		protected virtual bool IsRecyclable(UILoopSmartItem potentiallyRecyclable, int indexOfItemThatWillBecomeVisible, float heightOfItemThatWillBecomeVisible)
		{ return true; }

		protected virtual bool ShouldDestroyRecyclableItem(UILoopSmartItem inRecycleBin, bool isInExcess)
		{ return isInExcess; }

		protected virtual void OnBeforeRecycleOrDisableViewsHolder(UILoopSmartItem inRecycleBinOrVisible, int newItemIndex)
		{ }

		protected virtual void ClearCachedRecyclableItems()
		{
            if (_RecyclableItems != null)
			{
				foreach (var recyclable in _RecyclableItems)
				{
					if (recyclable != null && recyclable.root != null)
						try { GameObject.Destroy(recyclable.root.gameObject); } catch (Exception e) { Debug.LogException(e); }
				}
				_RecyclableItems.Clear();
			}
		}

		protected virtual void ClearVisibleItems()
		{
			if (_VisibleItems != null)
			{
				foreach (var item in _VisibleItems)
				{
					if (item != null && item.root != null)
						try { GameObject.Destroy(item.root.gameObject); } catch (Exception e) { Debug.LogException(e); }
				}
				_VisibleItems.Clear();
				_VisibleItemsCount = 0;
			}
		}

		protected virtual void OnScrollViewSizeChanged()
		{
		
		}

		protected virtual void RebuildLayoutDueToScrollViewSizeChange()
		{

			MarkViewsHoldersForRebuild(_VisibleItems);
			ClearCachedRecyclableItems();

			Canvas.ForceUpdateCanvases();

			_Params.InitIfNeeded(this);

			_InternalState.CacheScrollViewInfo(); // update vp size etc.
            _ItemsDesc.maxVisibleItemsSeenSinceLastScrollViewSizeChange = 0;
            _ItemsDesc.destroyedItemsSinceLastScrollViewSizeChange = 0;

			Refresh();
		}

		protected virtual void OnItemHeightChangedPreTwinPass(UILoopSmartItem viewsHolder) { }

		protected virtual void OnItemWidthChangedPreTwinPass(UILoopSmartItem viewsHolder) { }
		protected void ScheduleComputeVisibilityTwinPass(bool preferContentEndEdgeStationaryIfSizeChanges = false)
		{
			_InternalState.computeVisibilityTwinPassScheduled = true;
			_InternalState.preferKeepingContentEndEdgeStationaryInNextComputeVisibilityTwinPass = preferContentEndEdgeStationaryIfSizeChanges;
		}

		protected AbstractViewsBase GetViewsHolderOfClosestItemToViewportPoint(
			ICollection<UILoopSmartItem> viewsHolders,
			Func<RectTransform, float, RectTransform, float, float> getDistanceFn,
			float viewportPoint01,
			float itemPoint01,
			out float distance
		){
            UILoopSmartItem result = null;
			float minDistance = float.MaxValue;
			float curDistance;

			foreach (var vh in viewsHolders)
			{
				curDistance = Mathf.Abs(getDistanceFn(vh.root, itemPoint01, _Params.viewport, viewportPoint01));
				if (curDistance < minDistance)
				{
					result = vh;
					minDistance = curDistance;
				}
			}

			distance = minDistance;
			return result;
		}
		protected virtual void Dispose()
		{
			Initialized = false;

			if (_Params != null && _Params.scrollRect)
				_Params.scrollRect.onValueChanged.RemoveListener(OnScrollViewValueChanged);

			if (_SmoothScrollCoroutine != null)
			{
				try { StopCoroutine(_SmoothScrollCoroutine); } catch { }

				_SmoothScrollCoroutine = null;
			}

			ClearCachedRecyclableItems();
			_RecyclableItems = null;

			ClearVisibleItems();
			_VisibleItems = null;

			_Params = null;
			_InternalState = null;

			if (ItemsRefreshed != null)
				ItemsRefreshed = null;
		}
	}
}
