using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.ScrollRectItemsAdapter.Util.Drawer;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using frame8.ScrollRectItemsAdapter.Util;
using frame8.Logic.Misc.Visual.UI;

namespace frame8.ScrollRectItemsAdapter.IncrementalItemFetchExample
{
	/// <summary>
	/// This class demonstrates how items can be appended at the bottom as needed (i.e. when the user acually scrolls there), as opposed to directly downloading all of them.
	/// This is useful if the number of items can't be known beforehand (because of reasons). 
	/// Also, here it's demonstrated how items can be set custom sizes by overriding <see cref="CollectItemsSizes(ItemCountChangeMode, int, int, ItemsDescriptor)"/>
	/// Use this approach if it's impossible to know the total number of items in advance or there's simply too much overhead.
	///	If the number of items IS known, consider using placeholder prefabs with a "Loading..." text on them (which may also make for a nicer UX) while they're being downloaded/processed. 
	///	The placeholder approach is implemented in <see cref="GridExample.GridExample"/> - be sure to check it out  if interested.
	/// </summary>
	public class IncrementalItemFetch : SRIA<MyParams, MyItemViewsHolder>
	{
		// Instance of your ScrollRectItemsAdapter8 implementation
		bool _Fetching;
		LabelWithInputPanel _FetchCountSetting;
		LabelWithToggle _RandomSizesForNewItemsSetting;


		#region SRIA implementation
		/// <inheritdoc/>
		protected override void Start()
		{
			base.Start();

			DrawerCommandPanel.Instance.Init(this, false, false, true, true, false);
			DrawerCommandPanel.Instance.galleryEffectSetting.slider.value = 0f;
			DrawerCommandPanel.Instance.ItemCountChangeRequested += UpdateCapacity;

			_FetchCountSetting = DrawerCommandPanel.Instance.AddLabelWithInputPanel("Max items to fetch:");
			_FetchCountSetting.inputField.text = _Params.preFetchedItemsCount + "";
			_FetchCountSetting.inputField.keyboardType = TouchScreenKeyboardType.NumberPad;
			_FetchCountSetting.inputField.characterLimit = 2;
			_FetchCountSetting.inputField.onEndEdit.AddListener(_ => _Params.preFetchedItemsCount = _FetchCountSetting.InputFieldValueAsInt);

			_RandomSizesForNewItemsSetting = DrawerCommandPanel.Instance.AddLabelWithTogglePanel("Random sizes for new items");
		}

		/// <inheritdoc/>
		protected override void Update()
		{
			base.Update();

			if (_Fetching)
				return;

			int lastVisibleItemitemIndex = -1;
			if (_VisibleItemsCount > 0)
				lastVisibleItemitemIndex = _VisibleItems[_VisibleItemsCount - 1].ItemIndex;
			int numberOfItemsBelowLastVisible = _Params.Data.Count - (lastVisibleItemitemIndex + 1);

			// If the number of items available below the last visible (i.e. the bottom-most one, in our case) is less than <adapterParams.preFetchedItemsCount>, get more
			if (numberOfItemsBelowLastVisible < _Params.preFetchedItemsCount)
			{
				int newPotentialNumberOfItems = _Params.Data.Count + _Params.preFetchedItemsCount;
				if (_Params.totalCapacity > -1) // i.e. the capacity isn't unlimited
					newPotentialNumberOfItems = Mathf.Min(newPotentialNumberOfItems, _Params.totalCapacity);

				if (newPotentialNumberOfItems > _Params.Data.Count) // i.e. if we there's enough room for at least 1 more item
					StartPreFetching(newPotentialNumberOfItems - _Params.Data.Count);
			}
		}

		protected override void CollectItemsSizes(ItemCountChangeMode changeMode, int count, int indexIfInsertingOrRemoving, ItemsDescriptor itemsDesc)
		{
			base.CollectItemsSizes(changeMode, count, indexIfInsertingOrRemoving, itemsDesc);

			if (changeMode == ItemCountChangeMode.REMOVE || count == 0)
				return;

			if (!_RandomSizesForNewItemsSetting.toggle.isOn)
				return;

			// Randomize sizes
			int indexOfFirstItemThatWillChangeSize;
			if (changeMode == ItemCountChangeMode.RESET)
				indexOfFirstItemThatWillChangeSize = 0;
			else
				indexOfFirstItemThatWillChangeSize = indexIfInsertingOrRemoving;

			int end = indexOfFirstItemThatWillChangeSize + count;

			itemsDesc.BeginChangingItemsSizes(indexOfFirstItemThatWillChangeSize);
			for (int i = indexOfFirstItemThatWillChangeSize; i < end; ++i)
				itemsDesc[i] = UnityEngine.Random.Range(_Params.DefaultItemSize / 3, _Params.DefaultItemSize * 3);
			itemsDesc.EndChangingItemsSizes();
		}

		/// <inheritdoc/>
		protected override MyItemViewsHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new MyItemViewsHolder();
			instance.Init(_Params.itemPrefab, itemIndex);

			return instance;
		}

		/// <inheritdoc/>
		protected override void UpdateViewsHolder(MyItemViewsHolder newOrRecycled)
		{
			// Initialize the views from the associated model
			ExampleItemModel model = _Params.Data[newOrRecycled.ItemIndex];

			newOrRecycled.titleText.text = "[" + newOrRecycled.ItemIndex + "] " + model.title;
		}
		#endregion

		public void UpdateCapacity(int newCapacity)
		{
			_Params.totalCapacity = newCapacity;
			// Also reduce the current count, if the new capacity is smaller than it
			if (newCapacity < _Params.Data.Count)
			{
				int indexOfFirstRemovedItem = newCapacity;
				int cutInCount = _Params.Data.Count - newCapacity;
				//DrawerCommandPanel.Instance.setCountPanel.inputField.text = adapterParams.data.Count + "";
				//ResetItemsCount(newCapacity, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
				RemoveItems(indexOfFirstRemovedItem, cutInCount, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
				_Params.statusText.text = _Params.Data.Count + " items";
			}
		}

		// Setting _Fetching to true & starting to fetch 
		void StartPreFetching(int additionalItems)
		{
			_Fetching = true;
			DrawerCommandPanel.Instance.setCountPanel.button.interactable = false;
			StartCoroutine(FetchItemModelsFromServer(additionalItems, OnPreFetchingFinished));
		}

		// Updating the models list and notify the adapter that it changed; 
		// it'll call GetItemHeight() for each item and UpdateViewsHolder for the visible ones.
		// Setting _Fetching to false
		void OnPreFetchingFinished(ExampleItemModel[] models)
		{
			int index = _Params.Data.Count;
			_Params.Data.AddRange(models);
			InsertItems(index, models.Length, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn, true /*keep the current velocity*/);
			_Params.statusText.text = _Params.Data.Count + " items";
			_Fetching = false;
			DrawerCommandPanel.Instance.setCountPanel.button.interactable = true;
		}

		IEnumerator FetchItemModelsFromServer(int count, Action<ExampleItemModel[]> onDone)
		{
			_Params.statusText.text = "Fetching "+ count + " from server...";

			// Simulating server delay
			yield return new WaitForSeconds(DrawerCommandPanel.Instance.serverDelaySetting.InputFieldValueAsInt);

			// Generating some random models
			var results = new ExampleItemModel[count];
			for (int i = 0; i < count; ++i)
			{
				results[i] = new ExampleItemModel();
				results[i].title = "Item got at " + DateTime.Now.ToString("hh:mm:ss");
			}

			onDone(results);
		}
	}


	[Serializable]
	public class ExampleItemModel { public string title; }


	// This in almost all cases will contain the prefab and your list of models
	[Serializable] // serializable, so it can be shown in inspector
	public class MyParams : BaseParamsWithPrefabAndData<ExampleItemModel>
	{
		public Text statusText;
		public int preFetchedItemsCount;
		[Tooltip("Set to -1 if while fetching <preFetchedItemsCount> items, the adapter shouldn't check for a capacity limit")]
		public int totalCapacity;
	}


	public class MyItemViewsHolder : BaseItemViewsHolder
	{
		public Text titleText;


		public override void CollectViews()
		{
			base.CollectViews();

			titleText = root.Find("TitlePanel/TitleText").GetComponent<Text>();
		}
	}
}
