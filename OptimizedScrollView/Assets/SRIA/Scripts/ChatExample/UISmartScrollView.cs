using System.Collections;
using UnityEngine.UI.Extension.Tools;
using System;

namespace UnityEngine.UI.Extension
{
	public class UISmartScrollView : SmartScrollView<BaseParams>
	{
        private IList _datas;
        private GameObject _itemPrefab;
        private Type cellType;

        #region UISmartScrollView implementation

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

            if (!newOrRecycled.IsPopupAnimationActive && newOrRecycled.cellIndex == GetItemsCount() - 1) // only animating the last one
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
	}

 
}
