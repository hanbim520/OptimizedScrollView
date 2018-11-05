using UnityEngine.UI.Extension;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI.Extension.Tools
{
	[System.Serializable]
	public class BaseCollocation
    {
		#region Configuration
		[Header("Optimizing process")]
		public int recycleBinCapacity = -1;


        [Tooltip("请参阅BasePARAM.UpDATEMDENE EnUM以进行完整描述。默认情况下是OnScScLyTyNoMaultAuthurux更新，如果帧率是可接受的，则应该以这种方式退出。")]
        public UpdateMode updateMode = UpdateMode.ON_SCROLL_THEN_MONOBEHAVIOUR_UPDATE;

		public bool loopItems;
		[Tooltip("如果为NULL，则将ScReReCt视为视图")]
		public RectTransform viewport;
		[Tooltip("这是用来代替在内容上添加禁用LayOutGood组件的老方法。")]
		public RectOffset contentPadding = new RectOffset();

		public ContentGravity contentGravity = ContentGravity.START;

		[Tooltip("这是用来代替在内容上添加禁用LayOutGood组件的老方法。")]
		public float contentSpacing;

		[Range(0f, 1f)]
		public float galleryEffectAmount;

        [Range(0f, 1f)]
		public float galleryEffectViewportPivot = .5f;

		[Tooltip("The size of all items for which the size is not specified in CollectItemSizes()")]
		[SerializeField]
		protected float _DefaultItemSize = 60f;

		#endregion

        [NonSerialized]
		public ScrollRect scrollRect;
		[NonSerialized]
		public RectTransform content;

		public float DefaultItemSize { get { return _DefaultItemSize; } }
		public RectTransform ScrollViewRT { get { if (!_ScrollViewRT) _ScrollViewRT = scrollRect.transform as RectTransform; return _ScrollViewRT; } }

		RectTransform _ScrollViewRT;


		public BaseCollocation() { }

        public BaseCollocation(ScrollRect scrollRect)
            :this(scrollRect, scrollRect.transform as RectTransform, scrollRect.content)
        {}

        public BaseCollocation(ScrollRect scrollRect, RectTransform viewport, RectTransform content)
        {
            this.scrollRect = scrollRect;
            this.viewport = viewport == null ? scrollRect.transform as RectTransform : viewport;
            this.content = content == null ? scrollRect.content : content;
        }

		public virtual void InitIfNeeded(ISmartScrollView scrollview)
        {
			if (!scrollRect)
				scrollRect = scrollview.AsMonoBehaviour.GetComponent<ScrollRect>();
			if (!scrollRect)
				throw new UnityException("Can't find ScrollRect component!");
            if (!viewport)
                viewport = scrollRect.transform as RectTransform;
            if (!content)
                content = scrollRect.content;
		}

		public void UpdateContentPivotFromGravityType()
		{
			if (contentGravity != ContentGravity.NONE)
			{
				int v1_h0 = scrollRect.horizontal ? 0 : 1;

				var piv = content.pivot;

				// The transfersal position is at the center
				piv[1 - v1_h0] = .5f;

				int contentGravityAsInt = ((int)contentGravity);
				float pivotInScrollingDirection_IfVerticalScrollView;
				if (contentGravityAsInt < 3)
					// 1 = TOP := 1f;
					// 2 = CENTER := .5f;
					pivotInScrollingDirection_IfVerticalScrollView = 1f / contentGravityAsInt;
				else
					// 3 = BOTTOM := 0f;
					pivotInScrollingDirection_IfVerticalScrollView = 0f;

				piv[v1_h0] = pivotInScrollingDirection_IfVerticalScrollView;
				if (v1_h0 == 0) // i.e. if horizontal
					piv[v1_h0] = 1f - piv[v1_h0];

				content.pivot = piv;
			}
		}


		public enum UpdateMode
		{
            /// <summary>
            /// <para>更新是由单行为.UpDead（）触发的（即每个ScReVIEW都是活动的框架），并且在每个OnLoad事件中触发。</para>
            /// <para>滚动时性能适中，但在所有情况下都能正常工作</para>
            /// </summary>
            ON_SCROLL_THEN_MONOBEHAVIOUR_UPDATE,

            /// <summary>
            /// <para>更新每个OnLoad事件触发的AR</para>
            /// <para>实验性的但是，如果您使用它并没有发现任何问题，则建议在OnjScLyTyNoMyActualOuthUp更新中使用。</para>
            /// <para>如果不希望优化器在空闲时使用CPU，这很有用。</para>
            /// <para>滚动时性能更好一点</para>
            /// </summary>
            ON_SCROLL,

            /// <summary>
            /// <para>更新由MonoBehaviour.Update（）触发（即ScrollView处于活动状态的每个帧）</para>
            /// <para>在此模式下，快速滚动时会出现一些临时间隙。 如果这是不可接受的，请使用其他模式。</para>
            /// <para>滚动时的最佳性能，快速滚动时项目显示有点延迟</para>
            /// </summary>
            MONOBEHAVIOUR_UPDATE
        }


		public enum ContentGravity
		{
            /// <summary>手动设置</summary>
            NONE,

            /// <summary>顶部如果垂直滚动视图，否则离开</summary>
            START,

            /// <summary>顶部如果垂直滚动视图，否则离开</summary>
            CENTER,

            /// <summary>如果垂直滚动视图，则为底部，否则为右</summary>
            END

        }

	}
}
