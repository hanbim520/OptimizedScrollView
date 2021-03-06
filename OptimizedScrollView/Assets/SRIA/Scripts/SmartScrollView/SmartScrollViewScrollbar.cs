﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace UnityEngine.UI.Extension
{
    [RequireComponent(typeof(Scrollbar))]
    public class SmartScrollViewScrollbar : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IScrollRectProxy
	{
        public bool hideWhenNotNeeded = true;
        public bool autoHide = true;

		public bool autoHideFadeEffect = true;

        public bool autoHideCollapseEffect = true;

        public float autoHideTime = 1f;

		public float autoHideFadeEffectMinAlpha = .8f;
		public float autoHideCollapseEffectMinScale = .2f;

		[Range(0.01f, 1f)]
        public float minSize = .1f;

        [Range(0.015f, 2f)]
        public float sizeUpdateInterval = .05f;

		public int skippedFramesBetweenPositionChanges;

        public ScrollRect scrollRect;

        public RectTransform viewport;

		/// <summary>
		/// Will be retrieved from the scrollrect. If not found, it can be assigned anytime before the first Update. 
		/// If not assigned, a default proxy will be used. The purpose of this is to allow custom implementations of ScrollRect to be used
		/// </summary>
		public IScrollRectProxy externalScrollRectProxy;

		public bool IsDragging { get { return _Dragging; } }

		IScrollRectProxy ScrollRectProxy { get { return externalScrollRectProxy == null ? this : externalScrollRectProxy; } }

		const float HIDE_EFFECT_START_DELAY_01 = .4f; // relative to this.autoHideTime

		RectTransform _ScrollRectRT, _ViewPortRT;
        Scrollbar _Scrollbar;
		CanvasGroup _CanvasGroupForFadeEffect;
        bool _HorizontalScrollBar;
        Vector3 _InitialScale = Vector3.one;
        bool _Hidden, _AutoHidden, _HiddenNotNeeded;
        float _LastValue;
        float _TimeOnLastValueChange;
        bool _Dragging;
        Coroutine _SlowUpdateCoroutine;
		float _TransversalScaleOnLastDrag, _AlphaOnLastDrag;
		bool _FullyInitialized;
		int _FrameCountOnLastPositionUpdate;


		void Awake()
        {
			if (autoHideTime == 0f)
				autoHideTime = 1f;

			_Scrollbar = GetComponent<Scrollbar>();
			_InitialScale = _Scrollbar.transform.localScale;
            _LastValue = _Scrollbar.value;
            _TimeOnLastValueChange = Time.time;
            _HorizontalScrollBar = _Scrollbar.direction == Scrollbar.Direction.LeftToRight || _Scrollbar.direction == Scrollbar.Direction.RightToLeft;
            if (!scrollRect)
            {
                scrollRect = GetComponentInParent<ScrollRect>();
                //if (!scrollRect)
                //    throw new UnityException("Please provide a ScrollRect for SmartScrollViewScrollbar to work");
            }

            if (scrollRect)
            {
                _ScrollRectRT = scrollRect.transform as RectTransform;
                if (!viewport)
                    viewport = _ScrollRectRT;

                if (_HorizontalScrollBar)
                {
                    if (!scrollRect.horizontal)
                        throw new UnityException("Can't use horizontal scrollbar with non-horizontal scrollRect");

                    if (scrollRect.horizontalScrollbar)
                    {
                        Debug.Log("SmartScrollViewScrollbar: setting scrollRect.horizontalScrollbar to null (the whole point of using SmartScrollViewScrollbar is to NOT have any scrollbars assigned)");
                        scrollRect.horizontalScrollbar = null;
                    }
                    if (scrollRect.verticalScrollbar == _Scrollbar)
                    {
                        Debug.Log("SmartScrollViewScrollbar: Can't use the same scrollbar for both vert and hor");
                        scrollRect.verticalScrollbar = null;
                    }
                }
                else
                {
                    if (!scrollRect.vertical)
                        throw new UnityException("Can't use vertical scrollbar with non-vertical scrollRect");

                    if (scrollRect.verticalScrollbar)
                    {
                        Debug.Log("SmartScrollViewScrollbar: setting scrollRect.verticalScrollbar to null (the whole point of using SmartScrollViewScrollbar is to NOT have any scrollbars assigned)");
                        scrollRect.verticalScrollbar = null;
                    }
                    if (scrollRect.horizontalScrollbar == _Scrollbar)
                    {
                        Debug.Log("SmartScrollViewScrollbar: Can't use the same scrollbar for both vert and hor");
                        scrollRect.horizontalScrollbar = null;
                    }
                }

            }
            else
                Debug.LogError("No ScrollRect assigned!");

			if (autoHide)
				UpdateStartingValuesForAutoHideEffect();

			scrollRect.onValueChanged.AddListener(ScrollRect_OnValueChangedCalled);

			// May be null
			externalScrollRectProxy = scrollRect.GetComponent(typeof(IScrollRectProxy)) as IScrollRectProxy;
		}

		void OnEnable()
        {
            _Dragging = false; // just in case dragging was stuck in true and the object was disabled
            _SlowUpdateCoroutine = StartCoroutine(SlowUpdate());
		}

		void Update()
		{
			if (!_FullyInitialized)
				InitializeInFirstUpdate();

			if (scrollRect)
			{
				// Don't override when dragging
				if (_Dragging)
					return;

				var value = ScrollRectProxy.GetNormalizedPosition();

				_Scrollbar.value = value;
				if (autoHide)
				{
					if (value == _LastValue)
					{
						if (!_Hidden)
						{
							float timePassedForHide01 = Mathf.Clamp01((Time.time - _TimeOnLastValueChange) / autoHideTime);
							if (timePassedForHide01 >= HIDE_EFFECT_START_DELAY_01)
							{
								float hideEffectAmount01 = (timePassedForHide01 - HIDE_EFFECT_START_DELAY_01) / (1f - HIDE_EFFECT_START_DELAY_01);
								hideEffectAmount01 = hideEffectAmount01 * hideEffectAmount01 * hideEffectAmount01; // slow in, fast-out effect
								if (CheckForAudoHideFadeEffectAndInitIfNeeded())
									_CanvasGroupForFadeEffect.alpha = Mathf.Lerp(_AlphaOnLastDrag, autoHideFadeEffectMinAlpha, hideEffectAmount01);

								if (autoHideCollapseEffect)
								{
									Vector3 localScale = transform.localScale;
									localScale[scrollRect.vertical ? 0 : 1] = Mathf.Lerp(_TransversalScaleOnLastDrag, autoHideCollapseEffectMinScale, hideEffectAmount01);
									transform.localScale = localScale;
								}
							}

							if (timePassedForHide01 == 1f)
							{
								_AutoHidden = true;
								Hide();
							}
						}
					}
					else
					{
						_TimeOnLastValueChange = Time.time;
						_LastValue = value;

						if (_Hidden && !_HiddenNotNeeded)
							Show();
					}
				}
				else if (!hideWhenNotNeeded)
				{
					if (_Hidden)
						Show();
				}
			}
		}

		void OnDisable()
        {
            StopCoroutine(_SlowUpdateCoroutine);
		}

		void OnDestroy()
		{
			if (scrollRect)
				scrollRect.onValueChanged.RemoveListener(ScrollRect_OnValueChangedCalled);
		}

        #region IScrollRectProxy implementation (used if _PositionChangesProxy is not manually assigned)
        public event Action<float> ScrollPositionChanged = null;
        public void SetNormalizedPosition(float normalizedPosition) { if (_HorizontalScrollBar) scrollRect.horizontalNormalizedPosition = normalizedPosition; else scrollRect.verticalNormalizedPosition = normalizedPosition; }
		public float GetNormalizedPosition() { return _HorizontalScrollBar ? scrollRect.horizontalNormalizedPosition : scrollRect.verticalNormalizedPosition; }
		public float GetContentSize() { return _HorizontalScrollBar ? scrollRect.content.rect.width : scrollRect.content.rect.height; }
		#endregion

		#region Unity UI event callbacks
		public void OnBeginDrag(PointerEventData eventData) { _Dragging = true; }
		public void OnEndDrag(PointerEventData eventData) { _Dragging = false; }
		public void OnDrag(PointerEventData eventData) { OnScrollRectValueChanged(false); }
		public void OnPointerDown(PointerEventData eventData) { scrollRect.StopMovement(); }
		#endregion

		void InitializeInFirstUpdate()
		{
			if (externalScrollRectProxy != null)
				externalScrollRectProxy.ScrollPositionChanged += ExternalScrollRectProxy_OnScrollPositionChanged;
			_FullyInitialized = true;
		}

		IEnumerator SlowUpdate()
        {
            var waitAmount = new WaitForSeconds(sizeUpdateInterval);

            while (true)
            {
                yield return waitAmount;

                if (!enabled)
                    break;

                if (_ScrollRectRT && scrollRect.content)
                {
                    float size, viewportSize, contentSize = ScrollRectProxy.GetContentSize();
                    if (_HorizontalScrollBar)
                        viewportSize = viewport.rect.width;
                    else
                        viewportSize = viewport.rect.height;

                    if (contentSize <= 0f || contentSize == float.NaN || contentSize == float.Epsilon || contentSize == float.NegativeInfinity || contentSize == float.PositiveInfinity)
                        size = 1f;
                    else
                        size = Mathf.Clamp(viewportSize / contentSize, minSize, 1f);

                    _Scrollbar.size = size;
                    if (hideWhenNotNeeded)
                    {
                        if (size > .99f)
                        {
                            if (!_Hidden)
                            {
                                _HiddenNotNeeded = true;
                                Hide();
                            }
                        }
                        else
                        {
                            if (_Hidden && !_AutoHidden) 
                            {

                                Show();
                            }
                        }
                    }
                    else if (!autoHide)
                    {
                        if (_Hidden)
                            Show();
                    }
                }
            }
        }

        void Hide()
        {
            _Hidden = true;
			if (!autoHide || _HiddenNotNeeded)
				gameObject.transform.localScale = Vector3.zero;
        }

        void Show()
        {
            gameObject.transform.localScale = _InitialScale;
            _HiddenNotNeeded = _AutoHidden = _Hidden = false;
			if (CheckForAudoHideFadeEffectAndInitIfNeeded())
				_CanvasGroupForFadeEffect.alpha = 1f;

			UpdateStartingValuesForAutoHideEffect();
		}

		void UpdateStartingValuesForAutoHideEffect()
		{
			if (CheckForAudoHideFadeEffectAndInitIfNeeded())
				_AlphaOnLastDrag = _CanvasGroupForFadeEffect.alpha;

			if (autoHideCollapseEffect)
				_TransversalScaleOnLastDrag = transform.localScale[scrollRect.vertical ? 0 : 1];
		}

		bool CheckForAudoHideFadeEffectAndInitIfNeeded()
		{
			if (autoHideFadeEffect && !_CanvasGroupForFadeEffect)
			{
				_CanvasGroupForFadeEffect = GetComponent<CanvasGroup>();
				if (!_CanvasGroupForFadeEffect)
					_CanvasGroupForFadeEffect = gameObject.AddComponent<CanvasGroup>();
			}

			return autoHideFadeEffect;
		}

		void ScrollRect_OnValueChangedCalled(Vector2 _)
		{
			if (externalScrollRectProxy == null)
				OnScrollRectValueChanged(true);
		}

		void ExternalScrollRectProxy_OnScrollPositionChanged(float _) { OnScrollRectValueChanged(true); }

		void OnScrollRectValueChanged(bool fromScrollRect)
		{
			if (!fromScrollRect)
			{
				scrollRect.StopMovement();

				if (_FrameCountOnLastPositionUpdate + skippedFramesBetweenPositionChanges < Time.frameCount)
				{
					ScrollRectProxy.SetNormalizedPosition(_Scrollbar.value);
					_FrameCountOnLastPositionUpdate = Time.frameCount;
				}
			}

			_TimeOnLastValueChange = Time.time;
			if (autoHide)
				UpdateStartingValuesForAutoHideEffect();

			if (!_HiddenNotNeeded
				&& _Scrollbar.size < 1f) // is needed
				Show();
		}
	}
}