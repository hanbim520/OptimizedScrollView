using System.Collections;
using UnityEngine.UI.Extension.Tools.Util.Drawer;
using UnityEngine.UI.Extension.Tools;
using UnityEngine.UI.Extension.Tools.Util;
using UnityEngine.UI.Extension;
using System;

namespace UnityEngine.UI.Extension
{
	/// <summary>This class demonstrates a basic chat implementation. A message can contain a text, image, or both</summary>
	public class UISmartScrollView : SmartScrollView<BaseParamsWithPrefab>
	{
		const string LOREM_IPSUM = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.";
        //  public RectTransform itemPrefab;
        private IList _datas;
        private GameObject _itemPrefab;
        private Type cellType;

        #region UISmartScrollView implementation
        /// <inheritdoc/>
        void Start()
		{

			DrawerCommandPanel.Instance.Init(this, false, false, false, false, true);
			DrawerCommandPanel.Instance.galleryEffectSetting.slider.value = .04f;

			// No adding/removing at the head of the list
			DrawerCommandPanel.Instance.addRemoveOnePanel.button2.gameObject.SetActive(false);
			DrawerCommandPanel.Instance.addRemoveOnePanel.button4.gameObject.SetActive(false);

			// No removing whatsoever. Only adding
			DrawerCommandPanel.Instance.addRemoveOnePanel.button3.gameObject.SetActive(false);

			//DrawerCommandPanel.Instance.ItemCountChangeRequested += OnItemCountChangeRequested;
			DrawerCommandPanel.Instance.AddItemRequested += OnAddItemRequested;
			//DrawerCommandPanel.Instance.RemoveItemRequested += OnRemoveItemRequested;

			
		}

        public void UpdateScrollView<T>(IList datas,GameObject prefab)where T: UILoopSmartItem
        {
            if(datas == null || prefab == null)
            {
                Debug.LogError("参数不能为空!!");
                return;
            }
            _datas = datas;
            _itemPrefab = prefab;
            cellType = typeof(T);
            OnItemCountChangeRequested(_datas.Count);
        }
		/// <inheritdoc/>
		protected override void Update()
		{
			base.Update();

		}

		/// <inheritdoc/>
		protected override UILoopSmartItem CreateViewsHolder(int itemIndex)
		{
            var instance = Activator.CreateInstance(cellType) as UILoopSmartItem;
			instance.Init(_itemPrefab, itemIndex);

			return instance;
		}

		/// <inheritdoc/>
		protected override void OnItemHeightChangedPreTwinPass(UILoopSmartItem vh)
		{
			base.OnItemHeightChangedPreTwinPass(vh);

		//	vh.contentSizeFitter.enabled = false;
		}

		/// <inheritdoc/>
		protected override void UpdateViewsHolder(UILoopSmartItem newOrRecycled)
		{
			// Initialize the views from the associated model

			newOrRecycled.UpdateFromModel(_datas, newOrRecycled.ItemIndex);


            newOrRecycled.MarkForRebuild(); // will enable the content size fitter
                                            //newOrRecycled.contentSizeFitter.enabled = true;
            ScheduleComputeVisibilityTwinPass(true);

            if (!newOrRecycled.IsPopupAnimationActive && newOrRecycled.itemIndexInView == GetItemsCount() - 1) // only animating the last one
				newOrRecycled.IsPopupAnimationActive = true;
		}

		/// <inheritdoc/>
		protected override void OnBeforeRecycleOrDisableViewsHolder(UILoopSmartItem inRecycleBinOrVisible, int newItemIndex)
		{
			inRecycleBinOrVisible.IsPopupAnimationActive = false;

			base.OnBeforeRecycleOrDisableViewsHolder(inRecycleBinOrVisible, newItemIndex);
		}

		/// <inheritdoc/>
		protected override void RebuildLayoutDueToScrollViewSizeChange()
		{
			base.RebuildLayoutDueToScrollViewSizeChange();
		}
		#endregion

		#region events from DrawerCommandPanel
		void OnAddItemRequested(bool atEnd)
		{
			int index = atEnd ? _datas.Count : 0;
			InsertItems(index, 1, true);
		}
		void OnItemCountChangeRequested(int newCount)
		{
			// Generating some random models
			ResetItems(_datas.Count, true);
		}
        #endregion
		static string GetRandomContent() { return LOREM_IPSUM.Substring(0, UnityEngine.Random.Range(LOREM_IPSUM.Length / 50 + 1, LOREM_IPSUM.Length / 2)); }
	}

 
}
