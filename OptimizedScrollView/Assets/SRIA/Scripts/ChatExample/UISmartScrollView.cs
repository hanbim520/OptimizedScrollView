using System.Collections;
using UnityEngine.UI.Extension.Collocation;
using System;

namespace UnityEngine.UI.Extension
{
	public class UISmartScrollView : SmartScrollView<BaseCollocation>
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
		protected override void Update()
		{
			base.Update();

		}

		protected override UILoopSmartItem CreateCellView(int itemIndex)
		{
            var instance = Activator.CreateInstance(cellType) as UILoopSmartItem;
			instance.Init(_itemPrefab, itemIndex);

			return instance;
		}

		protected override void OnItemHeightChangedPreTwinPass(UILoopSmartItem vh)
		{
			base.OnItemHeightChangedPreTwinPass(vh);
		}

		protected override void UpdateCellView(UILoopSmartItem newOrRecycled)
		{
			newOrRecycled.UpdateFromModel(_datas, newOrRecycled.ItemIndex);

            newOrRecycled.MarkForRebuild();
            ScheduleComputeVisibilityTwinPass(true);


            if (!newOrRecycled.IsPopupAnimationActive && newOrRecycled.cellIndex == GetItemsCount() - 1) 
				newOrRecycled.IsPopupAnimationActive = true;
		}
		protected override void OnBeforeRecycleOrDisableViewsHolder(UILoopSmartItem inRecycleBinOrVisible, int newItemIndex)
		{
			inRecycleBinOrVisible.IsPopupAnimationActive = false;

			base.OnBeforeRecycleOrDisableViewsHolder(inRecycleBinOrVisible, newItemIndex);
		}

		protected override void RebuildLayoutDueToScrollViewSizeChange()
		{
			base.RebuildLayoutDueToScrollViewSizeChange();
		}
		#endregion

		#region events
		void OnAddItemRequested(bool atEnd)
		{
			int index = atEnd ? _datas.Count : 0;
			InsertItems(index, 1, true);
		}
		void OnItemCountChangeRequested(int newCount)
		{
			ResetItems(_datas.Count, true);
		}
        #endregion
	}

 
}
