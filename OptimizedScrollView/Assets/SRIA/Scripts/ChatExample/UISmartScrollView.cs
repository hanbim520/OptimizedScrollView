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
       

        public void UpdateScrollView<T>(IList datas,GameObject prefab,bool isScrollBottom = true)where T: UILoopSmartItem
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
            if(isScrollBottom)
            {
                StopCoroutine("ScrollToBottom");
                StartCoroutine("ScrollToBottom", datas.Count);
            } else
            {
                StopCoroutine("ScrollToUp");
                StartCoroutine("ScrollToUp");
            }
          
        }

        IEnumerator ScrollToUp()
        {
            yield return new WaitForEndOfFrame();
            SmoothScrollTo(0, 0f);
        }
        IEnumerator ScrollToBottom(int count)
        {
            yield return new WaitForEndOfFrame();
            SmoothScrollTo(count - 1, 0f);               
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
	    public void AddItemRequested(bool atEnd,int preCount,int count)
		{
			InsertItems(preCount, count, true);
		}
        public void RemoveItemRequested(bool atEnd, int count)
        {
            int index = atEnd ? (_datas.Count - count): 0;
            RemoveItems(index, count, true);
        }
        public void OnItemCountChangeRequested(int newCount)
		{
			ResetItems(_datas.Count, true);
		}
        #endregion
	}

 
}
