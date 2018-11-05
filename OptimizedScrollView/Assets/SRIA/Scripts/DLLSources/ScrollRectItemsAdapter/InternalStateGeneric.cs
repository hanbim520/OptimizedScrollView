using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI.Extension;

namespace UnityEngine.UI.Extension.Tools
{
    abstract class InternalStateGeneric<TParams> 
		where TParams : BaseCollocation
    {
		#region Fields & Props
		internal const double proximityToLimitNeeded01ToResetPos = 1d; 
		internal readonly Vector2 constantAnchorPosForAllItems = new Vector2(0f, 1f); // top-left
		internal float viewportSize;
		internal float paddingContentStart; // top/left
		internal float transversalPaddingContentStart; // left/top
		internal float paddingContentEnd; // bottom/right
		internal float paddingStartPlusEnd;
		internal float spacing;
		internal RectTransform.Edge startEdge; // RectTransform.Edge.Top/RectTransform.Edge.Left
		internal RectTransform.Edge endEdge; // RectTransform.Edge.Bottom/RectTransform.Edge.Right
		internal RectTransform.Edge transvStartEdge; // RectTransform.Edge.Left/RectTransform.Edge.Top

		// Cache params
		internal double lastProcessedCTVirtualInsetFromParentStart; // normY / 1-normX
		//internal int realIndexOfFirstItemInView;
		// 0, if contentSize < MaxContentSize; else, it's the number of times the content panel's position was reset to reserve space after it for the new items which become visible
		internal double contentPanelSkippedInsetDueToVirtualization;
		internal Vector2 scrollViewSize;
		//internal float[] itemsSizes; // heights/widths
		//internal double[] itemsSizesCumulative; // heights/widths
		//internal double cumulatedSizesOfAllItemsPlusSpacing;
		internal float contentPanelSize; // height/width
		internal double contentPanelVirtualSize; // height/width
		//internal bool onScrollPositionChangedFiredAndVisibilityComputedForCurrentItems;
		internal bool updateRequestPending;
		//internal bool layoutRebuildPendingDueToScrollViewSizeChangeEvent;
		//internal bool layoutIsBeingRebuildDueToScrollViewSizeChangeEvent;
		internal bool computeVisibilityTwinPassScheduled;
		internal bool preferKeepingContentEndEdgeStationaryInNextComputeVisibilityTwinPass;
		internal bool lastComputeVisibilityHadATwinPass;

		internal bool HasScrollViewSizeChanged { get { return scrollViewSize != _SourceParams.ScrollViewRT.rect.size; } }
		// (Update: setting it to 10 solved some issues. It's better this way. Maybe 100 is the upper-bound)
		// it should stay this should be at least 3. But it's HIGHLY RECOMMENDED to be 4 (this fixed ScrollTo in horizontal scroll views)
		// Even if "skipping" is on (the content virtual size is >= MaxContentPanelRealSize), the content's real size may slightly differ from this, 
		// even if this very value is passed to SetInsetAndSize as size (the biggest observer error was ~.001). 
		// This should be taken into consideration when checking wether the skipping is on or off (ex. ctRealSize < MaxContentPanelRealSize DOES NOT imply skipping is on)
		internal float MaxContentPanelRealSize { get { return viewportSize * 10; } } 
		internal double ContentPanelVirtualInsetFromViewportStart { get { return contentPanelSkippedInsetDueToVirtualization + _SourceParams.content.GetInsetFromParentEdge(_SourceParams.viewport, startEdge); } }
		internal double ContentPanelVirtualInsetFromViewportEnd { get { return -contentPanelVirtualSize + viewportSize - ContentPanelVirtualInsetFromViewportStart; } }
		internal double VirtualScrollableArea { get { return contentPanelVirtualSize - viewportSize; } }
		internal float RealScrollableArea { get { return contentPanelSize - viewportSize; } }

		ItemsDescriptor _ItemsDesc;
		TParams _SourceParams;
		Func<RectTransform, float> _GetRTCurrentSizeFn;
        #endregion


		protected InternalStateGeneric(TParams sourceParams, ItemsDescriptor itemsDescriptor)
		{
			_SourceParams = sourceParams;
            _ItemsDesc = itemsDescriptor;

			var lg = sourceParams.content.GetComponent<LayoutGroup>();
			if (lg && lg.enabled)
			{
				lg.enabled = false;
				Debug.Log("LayoutGroup on GameObject " + lg.name + " has beed disabled in order to use Tools");
			}

			var contentSizeFitter = sourceParams.content.GetComponent<ContentSizeFitter>();
			if (contentSizeFitter && contentSizeFitter.enabled)
			{
				contentSizeFitter.enabled = false;
				Debug.Log("ContentSizeFitter on GameObject " + contentSizeFitter.name + " has beed disabled in order to use Tools");
			}

			var layoutElement = sourceParams.content.GetComponent<LayoutElement>();
			if (layoutElement)
			{
				GameObject.Destroy(layoutElement);
				Debug.Log("LayoutElement on GameObject " + contentSizeFitter.name + " has beed DESTROYED in order to use Tools");
			}

			if (sourceParams.scrollRect.horizontal)
			{
				startEdge = RectTransform.Edge.Left;
				endEdge = RectTransform.Edge.Right;
				transvStartEdge = RectTransform.Edge.Top;
				_GetRTCurrentSizeFn = root => root.rect.width;
			}
			else
			{
				startEdge = RectTransform.Edge.Top;
				endEdge = RectTransform.Edge.Bottom;
				transvStartEdge = RectTransform.Edge.Left;
				_GetRTCurrentSizeFn = root => root.rect.height;
			}


			_SourceParams.UpdateContentPivotFromGravityType();

			CacheScrollViewInfo();
		}


		internal void CacheScrollViewInfo()
		{
			scrollViewSize = _SourceParams.ScrollViewRT.rect.size;
			RectTransform vpRT = _SourceParams.viewport;
			Rect vpRect = vpRT.rect;
			if (_SourceParams.scrollRect.horizontal)
			{
				viewportSize = vpRect.width;
				paddingContentStart = _SourceParams.contentPadding.left;
				paddingContentEnd = _SourceParams.contentPadding.right;
				transversalPaddingContentStart = _SourceParams.contentPadding.top;
				_ItemsDesc.itemsConstantTransversalSize = _SourceParams.content.rect.height - (transversalPaddingContentStart + _SourceParams.contentPadding.bottom);
			}
			else
			{
				viewportSize = vpRect.height;
				paddingContentStart = _SourceParams.contentPadding.top;
				paddingContentEnd = _SourceParams.contentPadding.bottom;
				transversalPaddingContentStart = _SourceParams.contentPadding.left;
                _ItemsDesc.itemsConstantTransversalSize = _SourceParams.content.rect.width - (transversalPaddingContentStart + _SourceParams.contentPadding.right);
			}

			spacing = _SourceParams.contentSpacing;

			if (_SourceParams.loopItems)
				paddingContentStart = paddingContentEnd = spacing;

			paddingStartPlusEnd = paddingContentStart + paddingContentEnd;
		}

		internal void OnItemsCountChanged(int itemsPrevCount, bool contentPanelEndEdgeStationary)
		{
			OnCumulatedSizesOfAllItemsChanged(contentPanelEndEdgeStationary, true);

			computeVisibilityTwinPassScheduled = false;
			lastComputeVisibilityHadATwinPass = false;
		}

		internal float ChangeItemSizeAndUpdateContentSizeAccordingly(UILoopSmartItem viewsHolder, int cellIndex, float requestedSize, bool itemEndEdgeStationary, bool rebuild = true)
		{
			float resolvedSize;
			if (viewsHolder == null)
				resolvedSize = requestedSize;
			else
			{
				if (viewsHolder.root == null)
					throw new UnityException("God bless: shouldn't happen if implemented according to documentation/examples"); // shouldn't happen if implemented according to documentation/examples

				RectTransform.Edge edge;
				float realInsetToSet;
				if (itemEndEdgeStationary)
				{
					edge = endEdge;
					realInsetToSet = GetItemInferredRealInsetFromParentEnd(cellIndex);
				}
				else
				{
					edge = startEdge;
					realInsetToSet = GetItemInferredRealInsetFromParentStart(cellIndex);
				}
				viewsHolder.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(_SourceParams.content, edge, realInsetToSet, requestedSize);

				resolvedSize = _GetRTCurrentSizeFn(viewsHolder.root);
				//viewsHolder.cachedSize = resolvedSize;
			}

            _ItemsDesc.BeginChangingItemsSizes(cellIndex);
            _ItemsDesc[cellIndex] = resolvedSize;
            _ItemsDesc.EndChangingItemsSizes();
			OnCumulatedSizesOfAllItemsChanged(itemEndEdgeStationary, rebuild);

			return resolvedSize;
		}
		internal void OnItemsSizesChangedExternally(List<UILoopSmartItem> vhs, float[] sizes, bool itemEndEdgeStationary)
		{
			if (_ItemsDesc.itemsCount == 0)
				throw new UnityException("Cannot change item sizes externally if the items count is 0!");

			int vhsCount = vhs.Count;
			int viewIndex;
            UILoopSmartItem vh;

			_ItemsDesc.BeginChangingItemsSizes(vhs[0].cellIndex);
			for (int i = 0; i < vhsCount; ++i)
			{
				vh = vhs[i];
				viewIndex = vh.cellIndex;

				_ItemsDesc[viewIndex] = sizes[i];
			}
			_ItemsDesc.EndChangingItemsSizes();

			OnCumulatedSizesOfAllItemsChanged(itemEndEdgeStationary, true);

			if (vhsCount > 0)
				CorrectPositions(vhs, true);//, itemEndEdgeStationary);
		}

		internal void CorrectPositions(List<UILoopSmartItem> vhs, bool alsoCorrectTransversalPositioning)//, bool itemEndEdgeStationary)
		{
            UILoopSmartItem vh;
			int count = vhs.Count;

			double insetStartOfCurItem = GetItemVirtualInsetFromParentStartUsingcellIndex(vhs[0].cellIndex);
			float curSize;
			for (int i = 0; i < count; ++i)
			{
				vh = vhs[i];
				curSize = _ItemsDesc[vh.cellIndex];
				vh.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(
					_SourceParams.content,
					startEdge,
					ConvertItemInsetFromParentStart_FromVirtualToReal(insetStartOfCurItem),
					curSize
				);
				insetStartOfCurItem += curSize + spacing;

				if (alsoCorrectTransversalPositioning)
					vh.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(transvStartEdge, transversalPaddingContentStart, _ItemsDesc.itemsConstantTransversalSize);
			}
		}

		internal void UpdateLastProcessedCTVirtualInsetFromParentStart() { lastProcessedCTVirtualInsetFromParentStart = ContentPanelVirtualInsetFromViewportStart; }

		/// <summary> See the <see cref="SmartScrollView{TParams, UILoopSmartItem}.GetVirtualAbstractNormalizedScrollPosition"/> for documentation</summary>
		internal double GetVirtualAbstractNormalizedScrollPosition()
		{
			float vpSize = viewportSize;
			double ctVrtSize = contentPanelVirtualSize;
			if (vpSize > ctVrtSize)
				return _SourceParams.content.GetInsetFromParentEdge(_SourceParams.viewport, startEdge) / vpSize;

			double ctVrtInsetWhenScrolledToMaxEnd = -ctVrtSize + vpSize;
			return 1d - ContentPanelVirtualInsetFromViewportStart / ctVrtInsetWhenScrolledToMaxEnd;
		}

		internal bool SetVirtualAbstractNormalizedScrollPosition(double pos)
		{
			if (viewportSize > contentPanelVirtualSize)
				return false; 

			double virtualScrollArea = contentPanelVirtualSize - viewportSize;
			double newVirtualInsetIfRealOffsetIsZero = (1d - pos) * virtualScrollArea;

			float realScrollArea = contentPanelSize - viewportSize;
			float newRealInset;
			if (pos < .000001d) // manual clamp
				newRealInset = -realScrollArea;
			else if (pos > .999999d) // manual clamp
				newRealInset = 0f;
			else
				newRealInset = -(float)Math.Min(float.MaxValue, Math.Max(float.MinValue, newVirtualInsetIfRealOffsetIsZero % realScrollArea));
			_SourceParams.content.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(startEdge, newRealInset, contentPanelSize);
			contentPanelSkippedInsetDueToVirtualization = -newVirtualInsetIfRealOffsetIsZero - newRealInset;

			return true;
		}

		internal void SetContentVirtualInsetFromViewportStart(double virtualInset)
		{
			double vsa = VirtualScrollableArea;
			if (!_SourceParams.loopItems)
			{
				if (virtualInset > 0d)
				{
					virtualInset = 0d;
					Debug.Log("virtualInset>0: " + virtualInset + ". Clamping...");
				}
				else if (-virtualInset > vsa)
				{
					Debug.Log("-virtualInset("+(-virtualInset)+") > virtualScrollableArea("+vsa+ "). Clamping...");
					virtualInset = -vsa;
				}
			}

			double newRealCTInsetFromVPS;
			float rsa = 12345f;
			double insetDistance = 12345f;
				rsa = RealScrollableArea; // real scrollable area
				insetDistance = Math.Abs(virtualInset);
				newRealCTInsetFromVPS = Math.Sign(virtualInset) * (insetDistance % rsa);
				if (insetDistance < rsa) // the content panel can handle it without the need to skip inset
					contentPanelSkippedInsetDueToVirtualization = 0;
				else
					contentPanelSkippedInsetDueToVirtualization = virtualInset - newRealCTInsetFromVPS;

			_SourceParams.content.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(startEdge, (float)newRealCTInsetFromVPS, contentPanelSize);

			Canvas.ForceUpdateCanvases();

		}

		internal double GetItemVirtualInsetFromParentStartUsingcellIndex(int cellIndex)
		{
			double cumulativeSizeOfAllItemsBeforePlusSpacing = 0d;
			if (cellIndex > 0)
				cumulativeSizeOfAllItemsBeforePlusSpacing = _ItemsDesc.GetItemSizeCumulative(cellIndex - 1) + cellIndex * (double)spacing;

			return paddingContentStart + cumulativeSizeOfAllItemsBeforePlusSpacing;
		}
		internal double GetItemVirtualInsetFromParentEndUsingcellIndex(int cellIndex)
		{ return contentPanelVirtualSize - GetItemVirtualInsetFromParentStartUsingcellIndex(cellIndex) - _ItemsDesc[cellIndex]; }
		internal float GetItemInferredRealInsetFromParentStart(int cellIndex)
		{ return ConvertItemInsetFromParentStart_FromVirtualToReal(GetItemVirtualInsetFromParentStartUsingcellIndex(cellIndex)); }
		internal float GetItemInferredRealInsetFromParentEnd(int cellIndex)
		{ return contentPanelSize - GetItemInferredRealInsetFromParentStart(cellIndex) - _ItemsDesc[cellIndex]; }

		//internal double ConvertItemOffsetFromParentStart_FromRealToVirtual(float realOffsetFromParrentStart)
		//{ return -contentPanelSkippedInsetDueToVirtualization + realOffsetFromParrentStart; }
		internal float ConvertItemInsetFromParentStart_FromVirtualToReal(double virtualInsetFromParrentStart)
		{ return (float)(virtualInsetFromParrentStart + contentPanelSkippedInsetDueToVirtualization); }

		void OnCumulatedSizesOfAllItemsChanged(bool contentPanelEndEdgeStationary, bool rebuild = true)
		{
			_ItemsDesc.cumulatedSizesOfAllItemsPlusSpacing = _ItemsDesc.CumulatedSizeOfAllItems + Math.Max(0d, _ItemsDesc.itemsCount - 1) * spacing;
			OnCumulatedSizesOfAllItemsPlusSpacingChanged(contentPanelEndEdgeStationary, rebuild);
		}

		//public float maxStored;
		void OnCumulatedSizesOfAllItemsPlusSpacingChanged(bool contentPanelEndEdgeStationary, bool rebuild = true)
		{
			double contentPrevVrtInsetFromVPEnd = ContentPanelVirtualInsetFromViewportEnd;
			double contentPrevVrtInsetFromVPStart = ContentPanelVirtualInsetFromViewportStart;
			double contentPanelPrevVirtualSize = contentPanelVirtualSize;

			contentPanelVirtualSize = _ItemsDesc.cumulatedSizesOfAllItemsPlusSpacing + paddingStartPlusEnd;
			float newContentPanelSize;
			bool contentPanelNewVirtualSizeIsSmallerThanMaxRealSize = contentPanelVirtualSize < MaxContentPanelRealSize;
			if (contentPanelNewVirtualSizeIsSmallerThanMaxRealSize) // the virtual size will be the same as the real size, because the data set is too small
			{
				newContentPanelSize = (float)contentPanelVirtualSize;
				contentPanelSkippedInsetDueToVirtualization = 0;
			}
			else
				newContentPanelSize = MaxContentPanelRealSize;
			float prevContentPanelSize = contentPanelSize;
			contentPanelSize = newContentPanelSize;
			float ctRealSizeChange = contentPanelSize - prevContentPanelSize;
			var edgeToUse = contentPanelEndEdgeStationary ? endEdge : startEdge;
			float insetToUse = _SourceParams.content.GetInsetFromParentEdge(_SourceParams.viewport, edgeToUse);
			_SourceParams.content.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(_SourceParams.viewport, edgeToUse, insetToUse, contentPanelSize);

			if (rebuild)
			{
				_SourceParams.scrollRect.Rebuild(CanvasUpdate.PostLayout);
				Canvas.ForceUpdateCanvases();
			}

			if (contentPanelNewVirtualSizeIsSmallerThanMaxRealSize)
				return;
			double ctVirtualSizeChange = contentPanelVirtualSize - contentPanelPrevVirtualSize;
			float cutRealAmountFoundBeforeVPE_IfEndStat_RealSizeHasShrunk = 0f;

			if (ctVirtualSizeChange != 0d || ctRealSizeChange != 0f)
			{
				var cutInVirtualSize = -ctVirtualSizeChange;
				if (contentPanelEndEdgeStationary)
				{
					contentPanelSkippedInsetDueToVirtualization -= ctVirtualSizeChange;

					if (ctVirtualSizeChange < 0d) // smaller content
					{
						double prevCTSVrtDistanceFromVPS = Math.Abs(contentPrevVrtInsetFromVPStart);
						if (prevCTSVrtDistanceFromVPS < cutInVirtualSize)
						{
							var cutAmountFoundAfterVPE = cutInVirtualSize - prevCTSVrtDistanceFromVPS;
							contentPanelSkippedInsetDueToVirtualization -= cutAmountFoundAfterVPE;
						}
					}
					else
					{
						if (contentPanelSize > prevContentPanelSize)
							contentPanelSkippedInsetDueToVirtualization += ctRealSizeChange;
					}
				}
				else
				{
					if (ctVirtualSizeChange < 0d) // smaller content
					{
						double prevCTEVrtDistanceFromVPE = Math.Abs(contentPrevVrtInsetFromVPEnd);
						double cutInVrtAndRealSize = cutInVirtualSize;
						if (ctRealSizeChange < 0f)
							cutInVrtAndRealSize -= ctRealSizeChange;

						if (prevCTEVrtDistanceFromVPE < cutInVrtAndRealSize) 
						{
							var cutVrtAmountFoundBeforeVPE = cutInVrtAndRealSize - prevCTEVrtDistanceFromVPE;
							contentPanelSkippedInsetDueToVirtualization +=
								(cutVrtAmountFoundBeforeVPE); 
						}
					}
					else if (ctRealSizeChange < 0d) // smaller real content
					{
						float prevCTEDistanceFromVPE = Math.Abs(viewportSize - insetToUse /*inset from start*/ - prevContentPanelSize);
						var cutInRealSize = -ctRealSizeChange;
						if (prevCTEDistanceFromVPE < cutInRealSize)
						{
							cutRealAmountFoundBeforeVPE_IfEndStat_RealSizeHasShrunk = cutInRealSize - prevCTEDistanceFromVPE;
							contentPanelSkippedInsetDueToVirtualization += cutRealAmountFoundBeforeVPE_IfEndStat_RealSizeHasShrunk;
						}
					}
				}
			}
		}
	}
}
