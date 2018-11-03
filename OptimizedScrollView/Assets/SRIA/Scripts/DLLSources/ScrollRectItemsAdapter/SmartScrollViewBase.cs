using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI.Extension;

namespace UnityEngine.UI.Extension.Tools
{
	public abstract partial class SmartScrollView<TParams> : MonoBehaviour, ISmartScrollView
    where TParams : BaseParams
	{
		IEnumerator SmoothScrollProgressCoroutine(
			int itemIndex, 
			float duration, 
			float normalizedOffsetFromViewportStart = 0f, 
			float normalizedPositionOfItemPivotToUse = 0f, 
			Func<float, bool> onProgress = null)
		{
			double minContentVirtualInsetFromVPAllowed = -_InternalState.VirtualScrollableArea;
			if (minContentVirtualInsetFromVPAllowed >= 0d)
			{
				_SmoothScrollCoroutine = null;

				if (onProgress != null)
					onProgress(1f);
				yield break;
			}

			var ignorOnScroll_lastValue = _SkipComputeVisibilityInUpdateOrOnScroll;
			_SkipComputeVisibilityInUpdateOrOnScroll = true;

			_Params.scrollRect.StopMovement();
			Canvas.ForceUpdateCanvases();

			Func<double> getTargetVrtInset = () =>
			{
				minContentVirtualInsetFromVPAllowed = -_InternalState.VirtualScrollableArea;

				return ScrollToHelper_GetContentStartVirtualInsetFromViewportStart_Clamped(
							minContentVirtualInsetFromVPAllowed, 
							itemIndex, 
							normalizedOffsetFromViewportStart, 
							normalizedPositionOfItemPivotToUse
						);
			};

			double initialVrtInsetFromParent = -1d, targetVrtInsetFromParent = -1d; 
			bool needToCalculateInitialInset = true, needToCalculateTargetInset = true, notCanceledByCaller = true;
			float startTime = Time.time, elapsedTime;
			double progress, value;
			var endOfFrame = new WaitForEndOfFrame();
			do
			{
				yield return null;
				yield return endOfFrame;

				elapsedTime = Time.time - startTime;
				if (elapsedTime >= duration)
					progress = 1d;
				else
					progress = Math.Sin((elapsedTime / duration) * Math.PI / 2);

				if (needToCalculateInitialInset)
				{
					initialVrtInsetFromParent = _InternalState.ContentPanelVirtualInsetFromViewportStart;
					needToCalculateInitialInset = _Params.loopItems;
				}

				if (needToCalculateTargetInset || _InternalState.lastComputeVisibilityHadATwinPass)
				{
					targetVrtInsetFromParent = getTargetVrtInset();
					needToCalculateTargetInset = _Params.loopItems || _InternalState.lastComputeVisibilityHadATwinPass;
				}
				value = initialVrtInsetFromParent * (1d - progress) + targetVrtInsetFromParent * progress; // Lerp for double
			
				if (Math.Abs(targetVrtInsetFromParent - value) < 1f)
				{
					value = targetVrtInsetFromParent;
					progress = 1d;
				}

				if (value > 0d)
				{
					progress = 1d; 
					value = 0d;
				}
				else
				{
					ScrollToHelper_SetContentVirtualInsetFromViewportStart(value, false);
				}
			}
			while (progress < 1d && (onProgress == null || (notCanceledByCaller = onProgress((float)progress))));

			if (notCanceledByCaller)
			{
				ScrollToHelper_SetContentVirtualInsetFromViewportStart(getTargetVrtInset(), false);

				ComputeVisibilityForCurrentPosition(false, true, false, -.1);
				ComputeVisibilityForCurrentPosition(true, true, false, +.1);

				_SmoothScrollCoroutine = null;

				if (onProgress != null)
					onProgress(1f);


			}
	
			_SkipComputeVisibilityInUpdateOrOnScroll = ignorOnScroll_lastValue;
		}

		void CancelAnimationsIfAny()
		{
			if (_SmoothScrollCoroutine != null)
			{
				StopCoroutine(_SmoothScrollCoroutine);
				_SmoothScrollCoroutine = null;

				_SkipComputeVisibilityInUpdateOrOnScroll = false;

			}
		}

		void MarkViewsHoldersForRebuild(List<UILoopSmartItem> vhs)
		{
			if (vhs != null)
				foreach (var v in vhs)
					if (v != null && v.root != null)
						v.MarkForRebuild();
		}

		double ScrollToHelper_GetContentStartVirtualInsetFromViewportStart_Clamped(double minContentVirtualInsetFromVPAllowed, int itemIndex, float normalizedItemOffsetFromStart, float normalizedPositionOfItemPivotToUse)
		{
			float maxContentInsetFromVPAllowed = _Params.loopItems ? _InternalState.viewportSize/2 : 0f; // if looping, there's no need to clamp. in addition, clamping would cancel a scrollTo if the content is exactly at start or end
			minContentVirtualInsetFromVPAllowed -= maxContentInsetFromVPAllowed;
			int itemViewIdex = _ItemsDesc.GetItemViewIndexFromRealIndex(itemIndex);
			float itemSize = _ItemsDesc[itemViewIdex];
			float insetToAdd = _InternalState.viewportSize * normalizedItemOffsetFromStart - itemSize * normalizedPositionOfItemPivotToUse;

			double itemVrtInsetFromStart = _InternalState.GetItemVirtualInsetFromParentStartUsingcellIndex(itemViewIdex);
			double ctInsetFromStart_Clamped = Math.Max(
						minContentVirtualInsetFromVPAllowed,
						Math.Min(maxContentInsetFromVPAllowed, -itemVrtInsetFromStart + insetToAdd)
					);


			return ctInsetFromStart_Clamped;
		}

		void ScrollToHelper_SetContentVirtualInsetFromViewportStart(double virtualInset, bool cancelSnappingIfAny)
		{
			var ignoreOnScroll_valueBefore = _SkipComputeVisibilityInUpdateOrOnScroll;
			_SkipComputeVisibilityInUpdateOrOnScroll = true;


			_Params.scrollRect.StopMovement();
			_InternalState.SetContentVirtualInsetFromViewportStart(virtualInset);

			ComputeVisibilityForCurrentPosition(true, true, false);
			if (!_CorrectedPositionInCurrentComputeVisibilityPass)
				CorrectPositionsOfVisibleItems(false);// false);

			_SkipComputeVisibilityInUpdateOrOnScroll = ignoreOnScroll_valueBefore;
		}

		void ChangeItemsCountInternal(ItemCountChangeMode changeMode, int count, int indexIfInsertingOrRemoving, bool contentPanelEndEdgeStationary, bool keepVelocity)
		{
			CancelAnimationsIfAny();

			var ignoreOnScroll_valueBefore = _SkipComputeVisibilityInUpdateOrOnScroll;
			_SkipComputeVisibilityInUpdateOrOnScroll = true;

			int prevCount = _ItemsDesc.itemsCount;
			var velocity = _Params.scrollRect.velocity;
			if (!keepVelocity)
				_Params.scrollRect.StopMovement();

			double sizeOfAllItemsBefore = _ItemsDesc.CumulatedSizeOfAllItems;

			_ItemsDesc.realIndexOfFirstItemInView = count > 0 ? 0 : -1;
			CollectItemsSizes(changeMode, count, indexIfInsertingOrRemoving, _ItemsDesc);

			double sizeOfAllItemsAfter = _ItemsDesc.CumulatedSizeOfAllItems;
			bool vrtContentPanelIsAtOrBeforeEnd = _InternalState.ContentPanelVirtualInsetFromViewportEnd >= 0d;
			if (sizeOfAllItemsAfter <= sizeOfAllItemsBefore) // content has shrunk
			{
				if (_InternalState.ContentPanelVirtualInsetFromViewportStart >= 0d)
					contentPanelEndEdgeStationary = false; 
				else if (vrtContentPanelIsAtOrBeforeEnd)
					contentPanelEndEdgeStationary = true; 
			}

			_InternalState.OnItemsCountChanged(prevCount, contentPanelEndEdgeStationary);
			if (GetNumExcessObjects() > 0)
				throw new UnityException("ChangeItemsCountInternal: GetNumExcessObjects() > 0 when calling ChangeItemsCountInternal(); this may be due ComputeVisibility not being finished executing yet");

            _RecyclableItems.AddRange(_VisibleItems);

			if (count == 0)
				ClearCachedRecyclableItems();

			_VisibleItems.Clear();
			_VisibleItemsCount = 0;

			double reportedScrollDelta;
			if (contentPanelEndEdgeStationary)
				reportedScrollDelta = .1f;
			else
			{
				reportedScrollDelta = -.1f;

				if (vrtContentPanelIsAtOrBeforeEnd)
					reportedScrollDelta = .1f;
			}

			ComputeVisibilityForCurrentPosition(true, true, true, reportedScrollDelta);
			if (!_CorrectedPositionInCurrentComputeVisibilityPass)
				CorrectPositionsOfVisibleItems(true);

			if (keepVelocity)
				_Params.scrollRect.velocity = velocity;

			if (ItemsRefreshed != null)
				ItemsRefreshed(prevCount, count);

			_SkipComputeVisibilityInUpdateOrOnScroll = ignoreOnScroll_valueBefore;
		}

		void MyUpdate()
		{
			if (_InternalState.computeVisibilityTwinPassScheduled)
				throw new UnityException("ScheduleComputeVisibilityTwinPass() can only be called in UpdateViewsHolder() !!!");

			bool scrollviewSizeChanged = _InternalState.HasScrollViewSizeChanged;
			if (scrollviewSizeChanged)
			{
				OnScrollViewSizeChanged(); 
				RebuildLayoutDueToScrollViewSizeChange(); // will call a refresh
				return;
			}

			if (_InternalState.updateRequestPending)
			{
				_InternalState.updateRequestPending = _Params.updateMode != BaseParams.UpdateMode.ON_SCROLL;
				if (!_SkipComputeVisibilityInUpdateOrOnScroll)
				{
					ComputeVisibilityForCurrentPosition(false, true, false);

				}
			}

			UpdateGalleryEffectIfNeeded();
		}

		void OnScrollViewValueChanged(Vector2 _)
		{
			if (_SkipComputeVisibilityInUpdateOrOnScroll)
				return;

			if (_Params.updateMode != BaseParams.UpdateMode.MONOBEHAVIOUR_UPDATE)
			{
				ComputeVisibilityForCurrentPosition(
					false, // ScrollPositionChanged will be called below 
					true, 
					false
				);
			}

			if (_Params.updateMode != BaseParams.UpdateMode.ON_SCROLL) // ScrollPositionChanged will be called after the next ComputeVisibility
				_InternalState.updateRequestPending = true;

			if (ScrollPositionChanged != null)
				ScrollPositionChanged(GetNormalizedPosition());
		}

		void ComputeVisibilityForCurrentPosition(bool forceFireScrollViewPositionChangedEvent, bool virtualizeContentPositionIfNeeded, bool alsoCorrectTransversalPositions, double overrideScrollingDelta)
		{
			double curInset = _InternalState.ContentPanelVirtualInsetFromViewportStart;
			_InternalState.lastProcessedCTVirtualInsetFromParentStart = curInset - overrideScrollingDelta;
			ComputeVisibilityForCurrentPosition(forceFireScrollViewPositionChangedEvent, virtualizeContentPositionIfNeeded, alsoCorrectTransversalPositions);
		}

		//float lastSpeed = -1f;
		void ComputeVisibilityForCurrentPosition(bool forceFireScrollViewPositionChangedEvent, bool virtualizeContentPositionIfNeeded, bool alsoCorrectTransversalPositions)
		{
			_CorrectedPositionInCurrentComputeVisibilityPass = false;

			if (_InternalState.computeVisibilityTwinPassScheduled)
				throw new UnityException("ScheduleComputeVisibilityTwinPass() can only be called in UpdateViewsHolder() !!!");

			double curPos = _InternalState.ContentPanelVirtualInsetFromViewportStart;
			double delta = (curPos - _InternalState.lastProcessedCTVirtualInsetFromParentStart);
			var velocityToSet = _Params.scrollRect.velocity;
			PointerEventData pev = null;
			bool triedRetrievingPev = false;

			if (virtualizeContentPositionIfNeeded && _VisibleItemsCount > 0)
				triedRetrievingPev = VirtualizeContentPositionIfNeeded(ref pev, delta, pev == null, alsoCorrectTransversalPositions);

			ComputeVisibility(delta);

			if (_InternalState.computeVisibilityTwinPassScheduled)
				ComputeVisibilityTwinPass(ref pev, delta, !triedRetrievingPev);
			else
				_InternalState.lastComputeVisibilityHadATwinPass = false;

			if (pev != null)
			{
				Utils.SetPointerEventDistanceToZero(pev);
				velocityToSet += pev.delta * Vector3.Distance(pev.pressPosition, pev.position) / 10;
			}

			if (!IsDragging) // if dragging, the velocity is not needed
				_Params.scrollRect.velocity = velocityToSet;

			_InternalState.UpdateLastProcessedCTVirtualInsetFromParentStart();
			
			if ((forceFireScrollViewPositionChangedEvent || delta != 0d) && ScrollPositionChanged != null)
				ScrollPositionChanged(GetNormalizedPosition());
		}

		void ComputeVisibilityTwinPass(ref PointerEventData pev, double delta, bool returnPointerEventDataIfNeeded)
		{
			_InternalState.computeVisibilityTwinPassScheduled = false;

			if (_VisibleItemsCount == 0)
				throw new UnityException("computeVisibilityTwinPassScheduled, but there are no visible items. Only call ScheduleComputeVisibilityTwinPass() in UpdateViewsHolder() !!!");

			// Prevent onValueChanged callbacks from being processed when setting inset and size of content
			var ignoreOnScroll_valueBefore = _SkipComputeVisibilityInUpdateOrOnScroll;
			_SkipComputeVisibilityInUpdateOrOnScroll = true;

			Canvas.ForceUpdateCanvases();

			float[] sizes = new float[_VisibleItemsCount];
			UILoopSmartItem v;
			// 2 fors are more efficient
			if (_Params.scrollRect.horizontal)
			{
				for (int i = 0; i < _VisibleItemsCount; ++i)
				{
					v = _VisibleItems[i];
					sizes[i] = v.root.rect.width;
					OnItemWidthChangedPreTwinPass(v);
				}
			}
			else
			{
				for (int i = 0; i < _VisibleItemsCount; ++i)
				{
					v = _VisibleItems[i];
					sizes[i] = v.root.rect.height;
					OnItemHeightChangedPreTwinPass(v);
				}
			}

			//bool endEdgeStationary = delta > 0d;
			bool endEdgeStationary = delta == 0d ? _InternalState.preferKeepingContentEndEdgeStationaryInNextComputeVisibilityTwinPass : delta > 0d;
			//Debug.Log("delta="+ delta + ", endStationary="+ endEdgeStationary);
			_InternalState.OnItemsSizesChangedExternally(_VisibleItems, sizes, endEdgeStationary);

			_SkipComputeVisibilityInUpdateOrOnScroll = ignoreOnScroll_valueBefore;

			if (returnPointerEventDataIfNeeded)
				pev = Utils.GetOriginalPointerEventDataByDrag(_Params.scrollRect.gameObject);

			_InternalState.lastComputeVisibilityHadATwinPass = true;
		}
		bool VirtualizeContentPositionIfNeeded(ref PointerEventData pev, double delta, bool returnPointerEventDataIfNeeded, bool alsoCorrectTransversalPositions)
		{
			if (delta == 0d)
				return false;

			int potentialResetDir = delta > 0d /*positive scroll -> going to start*/ ? 2 : 1;
			float curRealAbstrNormPos = _Params.scrollRect.horizontal ? 1f - _Params.scrollRect.horizontalNormalizedPosition : _Params.scrollRect.verticalNormalizedPosition;
			int firstVisibleItem_IndexInView = _VisibleItems[0].cellIndex, lastVisibleItem_IndexInView = firstVisibleItem_IndexInView + _VisibleItemsCount - 1;
			bool firstVisibleIsFirstIndexInView = firstVisibleItem_IndexInView == 0;
			bool lastVisibleIsLastIndexInView = lastVisibleItem_IndexInView == _ItemsDesc.itemsCount - 1;
			float contentInsetFromVPStart_Prev = _Params.content.GetInsetFromParentEdge(_Params.viewport, _InternalState.startEdge);
			float contentInsetFromVPEnd_Prev = _Params.content.GetInsetFromParentEdge(_Params.viewport, _InternalState.endEdge);
			double sizeCummForLastVisibleItem = _ItemsDesc.GetItemSizeCumulative(lastVisibleItem_IndexInView);
			double sizeCummForFirstVisibleItem = _ItemsDesc.GetItemSizeCumulative(firstVisibleItem_IndexInView);

			double sizVis = sizeCummForLastVisibleItem;
			if (!firstVisibleIsFirstIndexInView)
				sizVis -= (sizeCummForFirstVisibleItem - _ItemsDesc[firstVisibleItem_IndexInView]);
			sizVis += (lastVisibleItem_IndexInView - firstVisibleItem_IndexInView) * _InternalState.spacing;

			float firstVisibleItemAmountOutside = -contentInsetFromVPStart_Prev - _InternalState.GetItemInferredRealInsetFromParentStart(firstVisibleItem_IndexInView);
			float lastVisibleItemAmountOutside = -contentInsetFromVPEnd_Prev - _InternalState.GetItemInferredRealInsetFromParentEnd(lastVisibleItem_IndexInView);

			//Debug.Log("lastVisIdxInView="+ lastVisibleItem_IndexInView + ", lastIdxInView="+(_ItemsDesc.itemsCount-1));

			RectTransform.Edge edgeToInsetContentFrom;
			float contentNewInsetFromVPStartOrEnd;
			if (potentialResetDir == 1)
			{
				if (firstVisibleIsFirstIndexInView)
					return false;

				//sizVis += _InternalState.paddingContentStart;

				int itemsAfter = _ItemsDesc.itemsCount - lastVisibleItem_IndexInView - 1;
				double sizAft = _InternalState.spacing * itemsAfter
									+ (_ItemsDesc.GetItemSizeCumulative(_ItemsDesc.itemsCount - 1) - sizeCummForLastVisibleItem)
									+ _InternalState.paddingContentEnd;


				float contentNewInsetFromVPStart;
				if (sizAft < sizVis
					 && (!_Params.loopItems || !lastVisibleIsLastIndexInView)  
				)
				{
					float sizAftPlusLastAmountOutside = (float)sizAft + lastVisibleItemAmountOutside;
					contentNewInsetFromVPStart = -(_InternalState.contentPanelSize - _InternalState.viewportSize - sizAftPlusLastAmountOutside);
				}
				else
				{
					if ((!_Params.loopItems || !lastVisibleIsLastIndexInView) && curRealAbstrNormPos >= 1d - InternalState.proximityToLimitNeeded01ToResetPos)
						return false; 

					contentNewInsetFromVPStart = -(firstVisibleItemAmountOutside + _InternalState.paddingContentStart);
				}

				if (Math.Abs(contentInsetFromVPStart_Prev - contentNewInsetFromVPStart) < 1f)// && !_Params.loopItems)
					return false;

				contentNewInsetFromVPStartOrEnd = contentNewInsetFromVPStart;
				edgeToInsetContentFrom = _InternalState.startEdge;
			}
			else
			{
				if (lastVisibleIsLastIndexInView)
					return false;

				int itemsBefore = firstVisibleItem_IndexInView;
				double sizBef = _InternalState.spacing * itemsBefore
									+ sizeCummForFirstVisibleItem - _ItemsDesc[firstVisibleItem_IndexInView]
									+ _InternalState.paddingContentStart;


				float contentNewInsetFromVPEnd;
				if (sizBef < sizVis
					&& (!_Params.loopItems || !firstVisibleIsFirstIndexInView) 
				)
				{
					float sizBefPlusFirstAmountOutside = (float)sizBef + firstVisibleItemAmountOutside;
					contentNewInsetFromVPEnd = -(_InternalState.contentPanelSize - _InternalState.viewportSize - sizBefPlusFirstAmountOutside);
				}
				else
				{
					if ((!_Params.loopItems || !firstVisibleIsFirstIndexInView) && curRealAbstrNormPos <= InternalState.proximityToLimitNeeded01ToResetPos)
						return false;

					contentNewInsetFromVPEnd = -(lastVisibleItemAmountOutside + _InternalState.paddingContentEnd);
				}

				if (Math.Abs(contentInsetFromVPEnd_Prev - contentNewInsetFromVPEnd) < 1f)// && !_Params.loopItems)
					return false;

				contentNewInsetFromVPStartOrEnd = contentNewInsetFromVPEnd;
				edgeToInsetContentFrom = _InternalState.endEdge;
			}

			var ignoreOnScroll_valueBefore = _SkipComputeVisibilityInUpdateOrOnScroll;
			_SkipComputeVisibilityInUpdateOrOnScroll = true;

			if (returnPointerEventDataIfNeeded)
				pev = Utils.GetOriginalPointerEventDataByDrag(_Params.scrollRect.gameObject);
			_Params.content.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(_Params.viewport, edgeToInsetContentFrom, contentNewInsetFromVPStartOrEnd, _InternalState.contentPanelSize);
			_Params.scrollRect.Rebuild(CanvasUpdate.PostLayout);
			Canvas.ForceUpdateCanvases(); 

			// Restore to normal
			_SkipComputeVisibilityInUpdateOrOnScroll = ignoreOnScroll_valueBefore;

			bool looped = false;
			if (_Params.loopItems)
			{
				int newRealIndexOfFirstItemInView = -1;
				if (potentialResetDir == 1) // going towards end
				{
					if (lastVisibleIsLastIndexInView)
					{
						_InternalState.contentPanelSkippedInsetDueToVirtualization = 0d;

						newRealIndexOfFirstItemInView = _VisibleItems[0].ItemIndex;

						for (int i = 0; i < _VisibleItemsCount; ++i)
							_VisibleItems[i].cellIndex = i;
					}
				}
				else // going towards start
				{
					if (firstVisibleIsFirstIndexInView)
					{
						_InternalState.contentPanelSkippedInsetDueToVirtualization = -(_InternalState.contentPanelVirtualSize - _InternalState.contentPanelSize);

						newRealIndexOfFirstItemInView = _ItemsDesc.GetItemRealIndexFromViewIndex(lastVisibleItem_IndexInView + 1);

						for (int i = 0; i < _VisibleItemsCount; ++i)
							_VisibleItems[i].cellIndex = _ItemsDesc.itemsCount - _VisibleItemsCount + i;
					}
				}

				looped = newRealIndexOfFirstItemInView != -1;
				if (looped)
					_ItemsDesc.RotateItemsSizesOnScrollViewLooped(newRealIndexOfFirstItemInView);
			}

			if (!looped)
				_InternalState.contentPanelSkippedInsetDueToVirtualization += contentInsetFromVPStart_Prev - _Params.content.GetInsetFromParentEdge(_Params.viewport, _InternalState.startEdge);

			CorrectPositionsOfVisibleItems(alsoCorrectTransversalPositions);//delta > 0d);
			_CorrectedPositionInCurrentComputeVisibilityPass = true;


			_InternalState.UpdateLastProcessedCTVirtualInsetFromParentStart();

			return true;
		}

		void CorrectPositionsOfVisibleItems(bool alsoCorrectTransversalPositioning)//bool itemEndEdgeStationary)
		{
			//_CorrectedPositionInCurrentComputeVisibilityPass = true;

			// Update the positions of the visible items so they'll retain their position relative to the viewport
			if (_VisibleItemsCount > 0)
				_InternalState.CorrectPositions(_VisibleItems, alsoCorrectTransversalPositioning);//, itemEndEdgeStationary);
		}

		/// <summary>The very core of <see cref="SmartScrollView{TParams, UILoopSmartItem}"/>. You must be really brave if you think about trying to understand it :)</summary>
		void ComputeVisibility(double abstractDelta)
		{
			bool negativeScroll = abstractDelta <= 0d;
			//bool verticalScroll = _Params.scrollRect.vertical;

			// Viewport constant values
			float vpSize = _InternalState.viewportSize;

			// Content panel constant values
			float ctSpacing = _InternalState.spacing,
				  ctPadTransvStart = _InternalState.transversalPaddingContentStart;

			// Items constant values
			float allItemsTransversalSizes = _ItemsDesc.itemsConstantTransversalSize;

			// Items variable values
			UILoopSmartItem nlvHolder = null;
			//int currentLVItemIndex;
			int currentLVcellIndex;

			double negCurrentVrtInsetFromCTSToUseForNLV_posCurrentVrtInsetFromCTEToUseForNLV;
			//RectTransform.Edge negStartEdge_posEndEdge;
			RectTransform.Edge transvStartEdge = _InternalState.transvStartEdge;

			int endcellIndex,
				  neg1_posMinus1,
				  //negMinus1_pos1,
				  neg1_pos0,
				  neg0_pos1;

			if (negativeScroll)
			{
				neg1_posMinus1 = 1;
			}
			else
			{
				neg1_posMinus1 = -1;
			}
			neg1_pos0 = (neg1_posMinus1 + 1) / 2;
			neg0_pos1 = 1 - neg1_pos0;
			
			currentLVcellIndex = neg0_pos1 * (_ItemsDesc.itemsCount - 1) - neg1_posMinus1;
			bool thereWereVisibletems = _VisibleItemsCount > 0;
			endcellIndex = neg1_pos0 * (_ItemsDesc.itemsCount - 1);

			double ctVrtInsetFromVPS = _InternalState.ContentPanelVirtualInsetFromViewportStart;
			double negCTVrtInsetFromVPS_posCTVrtInsetFromVPE = negativeScroll ? ctVrtInsetFromVPS : (-_InternalState.contentPanelVirtualSize + _InternalState.viewportSize - ctVrtInsetFromVPS);
			UILoopSmartItem startingLVHolder = null;

			if (thereWereVisibletems)
			{

				int startingLVHolderIndex;
				startingLVHolderIndex = neg1_pos0 * (_VisibleItemsCount - 1);
				startingLVHolder = _VisibleItems[startingLVHolderIndex];

				currentLVcellIndex = startingLVHolder.cellIndex;

				bool currentIsOutside;
				//RectTransform curRecCandidateRT;
				float curRecCandidateSizePlusSpacing;

				// vItemHolder is:
				// first in _VisibleItems, if negativeScroll
				// last in _VisibleItems, else
				int curRecCandidateVHIndex = neg0_pos1 * (_VisibleItemsCount - 1);
				UILoopSmartItem curRecCandidateVH = _VisibleItems[curRecCandidateVHIndex];
				double curInsetFromParentEdge = negativeScroll ? _InternalState.GetItemVirtualInsetFromParentStartUsingcellIndex(curRecCandidateVH.cellIndex)
																: _InternalState.GetItemVirtualInsetFromParentEndUsingcellIndex(curRecCandidateVH.cellIndex);
				while (true)
				{
					
					curRecCandidateSizePlusSpacing = _ItemsDesc[curRecCandidateVH.cellIndex] + ctSpacing; 

					
					currentIsOutside = negCTVrtInsetFromVPS_posCTVrtInsetFromVPE + (curInsetFromParentEdge + curRecCandidateSizePlusSpacing) <= 0d;

					if (currentIsOutside)
					{
                        _RecyclableItems.Add(curRecCandidateVH);
						_VisibleItems.RemoveAt(curRecCandidateVHIndex);
						--_VisibleItemsCount;

						if (_VisibleItemsCount == 0) 
							break;
					}
					else
						break; 

					curRecCandidateVHIndex -= neg0_pos1; 

					curInsetFromParentEdge += curRecCandidateSizePlusSpacing;
					curRecCandidateVH = _VisibleItems[curRecCandidateVHIndex];
				}
			}

			double currentItemVrtInset_negStart_posEnd = double.PositiveInfinity;
			int estimatedAVGVisibleItems = -1;
			if (Math.Abs(abstractDelta) > 10000d // huge jumps need optimization
				&& (estimatedAVGVisibleItems = (int)Math.Round(Math.Min(_InternalState.viewportSize / ((_Params.DefaultItemSize + _InternalState.spacing)), _AVGVisibleItemsCount)))
					< _ItemsDesc.itemsCount
			){
				int estimatedIndexInViewOfNewFirstVisible = (int)
					Math.Round(
						(1d - _InternalState.GetVirtualAbstractNormalizedScrollPosition()) * ((_ItemsDesc.itemsCount - 1) - neg1_pos0 * estimatedAVGVisibleItems)
					);

				double negCTVrtAmountBeforeVP_posCTVrtAmountAfterVP = Math.Max(-negCTVrtInsetFromVPS_posCTVrtInsetFromVPE, 0d);
				int initialEstimatedIndexInViewOfNewFirstVisible = estimatedIndexInViewOfNewFirstVisible;
				int index = initialEstimatedIndexInViewOfNewFirstVisible;
				float itemSize = _ItemsDesc[index];
				double negInsetStart_posInsetEnd =
						neg0_pos1 * (_InternalState.contentPanelVirtualSize - itemSize)
						+ neg1_posMinus1 * _InternalState.GetItemVirtualInsetFromParentStartUsingcellIndex(index);

				while (negInsetStart_posInsetEnd <= /*minEstimatedItemInsetFrom_negStart_posEnd*/ negCTVrtAmountBeforeVP_posCTVrtAmountAfterVP - itemSize)
				{
					index += neg1_posMinus1;
					itemSize = _ItemsDesc[index];
					negInsetStart_posInsetEnd += itemSize + _InternalState.spacing;
				}

				if (index == initialEstimatedIndexInViewOfNewFirstVisible)
				{
					// Executes at least once
					while (negInsetStart_posInsetEnd > /*minEstimatedItemInsetFrom_negStart_posEnd*/ negCTVrtAmountBeforeVP_posCTVrtAmountAfterVP - itemSize)
					{
						index -= neg1_posMinus1;
						itemSize = _ItemsDesc[index];
						negInsetStart_posInsetEnd -= itemSize + _InternalState.spacing;
					}

					negInsetStart_posInsetEnd += itemSize + _InternalState.spacing;
				}
				else // index bigger (lesser, if pos scroll) than initial
				{
					index -= neg1_posMinus1;
				}

				if (!thereWereVisibletems ||
					negativeScroll && index > currentLVcellIndex || 
					!negativeScroll && index < currentLVcellIndex // analogous explanation for pos scroll
				){
					//Debug.Log("est=" + estimatedIndexInViewOfNewFirstVisible + ", def=" + currentLVcellIndex + ", actual=" + index);
					currentLVcellIndex = index;
					currentItemVrtInset_negStart_posEnd = negInsetStart_posInsetEnd;
				}
			}

			if (double.IsInfinity(currentItemVrtInset_negStart_posEnd) 
				&& currentLVcellIndex != endcellIndex) 
			{
				int nextValueOf_NLVIndexInView = currentLVcellIndex + neg1_posMinus1; // next value in the loop below
				if (negativeScroll)
					currentItemVrtInset_negStart_posEnd = _InternalState.GetItemVirtualInsetFromParentStartUsingcellIndex(nextValueOf_NLVIndexInView);
				else
					currentItemVrtInset_negStart_posEnd = _InternalState.GetItemVirtualInsetFromParentEndUsingcellIndex(nextValueOf_NLVIndexInView);
			}

			do
			{
				if (currentLVcellIndex == endcellIndex)
					break;

				//int nlvIndex = currentLVcellIndex; // TODO pending removal
				int nlvIndexInView = currentLVcellIndex;
				float nlvSize;
				bool breakBigLoop = false,
					 negNLVCandidateIsBeforeVP_posNLVCandidateIsAfterVP; // before vpStart, if negative scroll; after vpEnd, else

				do
				{
					nlvIndexInView += neg1_posMinus1;
					nlvSize = _ItemsDesc[nlvIndexInView];
					negCurrentVrtInsetFromCTSToUseForNLV_posCurrentVrtInsetFromCTEToUseForNLV = currentItemVrtInset_negStart_posEnd;
					negNLVCandidateIsBeforeVP_posNLVCandidateIsAfterVP = negCTVrtInsetFromVPS_posCTVrtInsetFromVPE + (negCurrentVrtInsetFromCTSToUseForNLV_posCurrentVrtInsetFromCTEToUseForNLV + nlvSize) <= 0d;
					if (negNLVCandidateIsBeforeVP_posNLVCandidateIsAfterVP)
					{
						if (nlvIndexInView == endcellIndex) // all items are outside viewport => abort
						{
							breakBigLoop = true;
							break;
						}
					}
					else
					{
						
						if (negCTVrtInsetFromVPS_posCTVrtInsetFromVPE + negCurrentVrtInsetFromCTSToUseForNLV_posCurrentVrtInsetFromCTEToUseForNLV > vpSize)
						{
							breakBigLoop = true;
							break;
						}

						break;
					}
					currentItemVrtInset_negStart_posEnd += nlvSize + _InternalState.spacing;
				}
				while (true);

				if (breakBigLoop)
					break;

				int nlvRealIndex = _ItemsDesc.GetItemRealIndexFromViewIndex(nlvIndexInView);

				int i = 0;
				UILoopSmartItem potentiallyRecyclable;
				while (true)
				{
					if (i < _RecyclableItems.Count)
					{
						potentiallyRecyclable = _RecyclableItems[i];
						if (IsRecyclable(potentiallyRecyclable, nlvRealIndex, nlvSize))
						{
							OnBeforeRecycleOrDisableViewsHolder(potentiallyRecyclable, nlvRealIndex);

                            _RecyclableItems.RemoveAt(i);
							nlvHolder = potentiallyRecyclable;
							break;
						}
						++i;
					}
					else
					{
						// Found no recyclable view with the requested height
						nlvHolder = CreateViewsHolder(nlvRealIndex);
						break;
					}
				}

				// Add it in list at [end]
				_VisibleItems.Insert(neg1_pos0 * _VisibleItemsCount, nlvHolder);
				++_VisibleItemsCount;

				// Update its index
				nlvHolder.ItemIndex = nlvRealIndex;
				nlvHolder.cellIndex = nlvIndexInView;

				//// Cache its height
				//nlvHolder.cachedSize = _ItemsDescriptor.itemsSizes[nlvIndexInView];

				// Make sure it's parented to content panel
				RectTransform nlvRT = nlvHolder.root;
				nlvRT.SetParent(_Params.content, false);

				// Update its views
				UpdateViewsHolder(nlvHolder);

				// Make sure it's GO is activated
				nlvHolder.root.gameObject.SetActive(true);

				nlvRT.anchorMin = nlvRT.anchorMax = _InternalState.constantAnchorPosForAllItems;

				double currentVirtualInsetFromCTSToUseForNLV =
					neg0_pos1 * (_InternalState.contentPanelVirtualSize - nlvSize) + neg1_posMinus1 * negCurrentVrtInsetFromCTSToUseForNLV_posCurrentVrtInsetFromCTEToUseForNLV;

				nlvRT.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(
					_Params.content, 
					_InternalState.startEdge, 
					_InternalState.ConvertItemInsetFromParentStart_FromVirtualToReal(currentVirtualInsetFromCTSToUseForNLV), 
					nlvSize
				);

				
				nlvRT.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(_Params.content, transvStartEdge, ctPadTransvStart, allItemsTransversalSizes);

				currentLVcellIndex = nlvIndexInView;
				currentItemVrtInset_negStart_posEnd += nlvSize + _InternalState.spacing;
			}
			while (true);

		
			if (_VisibleItemsCount > _ItemsDesc.maxVisibleItemsSeenSinceLastScrollViewSizeChange)
                _ItemsDesc.maxVisibleItemsSeenSinceLastScrollViewSizeChange = _VisibleItemsCount;

			GameObject go;
			UILoopSmartItem vh;
			for (int i = 0; i < _RecyclableItems.Count;)
			{
				vh = _RecyclableItems[i];
				go = vh.root.gameObject;
				if (go.activeSelf)
					OnBeforeRecycleOrDisableViewsHolder(vh, -1); // -1 means it'll be disabled, not re-used ATM

				go.SetActive(false);
				if (ShouldDestroyRecyclableItem(vh, GetNumExcessObjects() > 0))
				{
					GameObject.Destroy(go);
                    _RecyclableItems.RemoveAt(i);
					++_ItemsDesc.destroyedItemsSinceLastScrollViewSizeChange;
				}
				else
					++i;
			}

			_AVGVisibleItemsCount = _AVGVisibleItemsCount * .9d + _VisibleItemsCount * .1d;
		}

		int GetNumExcessObjects()
		{
			if (_RecyclableItems.Count > 1)
			{
				int excess = (_RecyclableItems.Count + _VisibleItemsCount) - GetMaxNumObjectsToKeepInMemory();
				if (excess > 0)
					return excess;
			}

			return 0;
		}

		int GetMaxNumObjectsToKeepInMemory()
		{
			return _Params.recycleBinCapacity > 0 ?
					  _Params.recycleBinCapacity + _VisibleItemsCount
					  : _ItemsDesc.maxVisibleItemsSeenSinceLastScrollViewSizeChange
						+ _ItemsDesc.destroyedItemsSinceLastScrollViewSizeChange + 1;
		}

		void UpdateGalleryEffectIfNeeded()
		{
			if (_Params.galleryEffectAmount == 0f)
			{
				if (_PrevGalleryEffectAmount == _Params.galleryEffectAmount)
					return;

				foreach (var recycled in _RecyclableItems)
					if (recycled != null && recycled.root)
						recycled.root.localScale = Vector3.one;
			}

			if (_VisibleItemsCount == 0)
				return;

			Func<RectTransform, float> getCornerFn;
			float viewportPivot_MinimumIsMinus1_MaximumIs1 = _Params.galleryEffectViewportPivot * 2 - 1f;
			float hor1_vertMinus1;
			if (_Params.scrollRect.horizontal)
			{
				hor1_vertMinus1 = 1;
				getCornerFn = RectTransformHelper.GetWorldRight;
			}
			else
			{
				hor1_vertMinus1 = -1;
				getCornerFn = RectTransformHelper.GetWorldTop;
			}

            float halfVPSize = _InternalState.viewportSize / 2, 
				 vpPivot = (getCornerFn(_Params.viewport) - halfVPSize) + (halfVPSize * (viewportPivot_MinimumIsMinus1_MaximumIs1 * hor1_vertMinus1));
			for (int i = 0; i < _VisibleItemsCount; i++)
			{
				var vh = _VisibleItems[i];
				float center = getCornerFn(vh.root) - _ItemsDesc[vh.cellIndex] / 2;

				float t01 = 1f - Mathf.Clamp01(Mathf.Abs(center - vpPivot) / halfVPSize);
				vh.root.localScale = Vector3.Lerp(Vector3.one * (1f - _Params.galleryEffectAmount), Vector3.one, t01);
			}
			_PrevGalleryEffectAmount = _Params.galleryEffectAmount;
		}
		

		/// <inheritdoc/>
		class InternalState : InternalStateGeneric<TParams>
		{
			internal static InternalState CreateFromSourceParamsOrThrow(TParams sourceParams, ItemsDescriptor itemsDescriptor)
			{
				if (sourceParams.scrollRect.horizontal && sourceParams.scrollRect.vertical)
					throw new UnityException("Can't optimize a ScrollRect with both horizontal and vertical scrolling modes. Disable one of them");

				return new InternalState(sourceParams, itemsDescriptor);
			}


			protected InternalState(TParams sourceParams, ItemsDescriptor itemsDescriptor) : base(sourceParams, itemsDescriptor) {}
		}
	}
}
