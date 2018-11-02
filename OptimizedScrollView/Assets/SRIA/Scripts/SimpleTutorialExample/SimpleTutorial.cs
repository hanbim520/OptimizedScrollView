using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.Logic.Misc.Other.Extensions;
using frame8.ScrollRectItemsAdapter.Util;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using frame8.ScrollRectItemsAdapter.Util.Drawer;
using frame8.ScrollRectItemsAdapter.Util.PullToRefresh;
using frame8.Logic.Misc.Visual.UI;

namespace frame8.ScrollRectItemsAdapter.SimpleTutorialExample
{
    /// <summary>Class (initially) implemented during this YouTube tutorial: https://youtu.be/aoqq_j-aV8I (which is now too old to relate). It demonstrates a simple use case with items that expand/collapse on click</summary>
    public class SimpleTutorial : SRIA<MyParams, MyItemViewsHolder>, ExpandCollapseOnClick.ISizeChangesHandler
	{
        /// <summary>Fired when the number of items changes or refreshes</summary>
        public UnityEngine.Events.UnityEvent OnItemsUpdated;


		#region SRIA implementation
		/// <inheritdoc/>
		protected override void Start()
		{
			_Params.NewModelCreator = index => CreateNewModel(index);

			base.Start();

			DrawerCommandPanel.Instance.Init(this);
			DrawerCommandPanel.Instance.serverDelaySetting.inputField.text = Mathf.Max(0, _Params.initialSimulatedServerDelay) + "";
			DrawerCommandPanel.Instance.ItemCountChangeRequested += OnItemCountChangeRequested;
			DrawerCommandPanel.Instance.AddItemRequested += OnAddItemRequested;
			DrawerCommandPanel.Instance.RemoveItemRequested += OnRemoveItemRequested;

			// Initially set the number of items to the number in the input field
			DrawerCommandPanel.Instance.RequestChangeItemCountToSpecified();
		}

		/// <inheritdoc/>
		protected override MyItemViewsHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new MyItemViewsHolder();
			instance.Init(_Params.itemPrefab, itemIndex);
			instance.expandOnCollapseComponent.sizeChangesHandler = this;

			return instance;
		}

		/// <inheritdoc/>
		protected override void UpdateViewsHolder(MyItemViewsHolder newOrRecycled)
		{
			// Initialize the views from the associated model
			ExampleItemModel model = _Params.Data[newOrRecycled.ItemIndex];

			newOrRecycled.backgroundImage.color = model.color;
			newOrRecycled.titleText.text = model.title + " #" + newOrRecycled.ItemIndex;
			newOrRecycled.icon1Image.texture = _Params.availableIcons[model.icon1Index];
			newOrRecycled.icon2Image.texture = _Params.availableIcons[model.icon2Index];

			if (newOrRecycled.expandOnCollapseComponent)
			{
				newOrRecycled.expandOnCollapseComponent.expanded = model.expanded;
				newOrRecycled.expandOnCollapseComponent.nonExpandedSize = model.nonExpandedSize;
			}
		}


		#region ExpandCollapseOnClick.ISizeChangesHandler implementation
		bool ExpandCollapseOnClick.ISizeChangesHandler.HandleSizeChangeRequest(RectTransform rt, float newSize)
		{
			var vh = GetItemViewsHolderIfVisible(rt);

			// If the vh is visible, we update our list of sizes
			if (vh == null)
				return false;

			RequestChangeItemSizeAndUpdateLayout(vh, newSize, DrawerCommandPanel.Instance.freezeItemEndEdgeToggle.isOn);

			return true;
		}

		public void OnExpandedStateChanged(RectTransform rt, bool expanded)
		{
			var vh = GetItemViewsHolderIfVisible(rt);

			// If the vh is visible and the request is accepted, we update the model's "expanded" field
			if (vh != null)
				_Params.Data[vh.ItemIndex].expanded = expanded;
		}
		#endregion

		#endregion

		#region events from DrawerCommandPanel
		void OnAddItemRequested(bool atEnd)
		{
			int index = atEnd ? _Params.Data.Count : 0;
			_Params.Data.Insert(index, 1);
			InsertItems(index, 1, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
			if (OnItemsUpdated != null)
				OnItemsUpdated.Invoke();
		}
		void OnRemoveItemRequested(bool fromEnd)
		{
			if (_Params.Data.Count == 0)
				return;

			int index = fromEnd ? _Params.Data.Count - 1 : 0;

			_Params.Data.RemoveAt(index);
			RemoveItems(index, 1, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
			if (OnItemsUpdated != null)
				OnItemsUpdated.Invoke();
		}
		void OnItemCountChangeRequested(int newCount)
		{ StartCoroutine(FetchItemModelsFromServer(newCount, () => OnReceivedNewModels(newCount))); }
		#endregion

		void OnReceivedNewModels(int newCount)
        {
			_Params.Data.Clear();
			_Params.Data.InitWithNewCount(newCount);

			ResetItems(_Params.Data.Count, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
			if (OnItemsUpdated != null)
				OnItemsUpdated.Invoke();
		}

        IEnumerator FetchItemModelsFromServer(int count, Action onDone)
        {
			// Simulating server delay
			yield return new WaitForSeconds(DrawerCommandPanel.Instance.serverDelaySetting.InputFieldValueAsInt);

            onDone();
		}

		ExampleItemModel CreateNewModel(int itemIdex)
		{
			return new ExampleItemModel()
			{
				title = "Item ",
				icon1Index = UnityEngine.Random.Range(0, _Params.availableIcons.Length),
				icon2Index = UnityEngine.Random.Range(0, _Params.availableIcons.Length),
				nonExpandedSize = _Params.ItemPrefabSize
			};
		}
	}


	public class ExampleItemModel
	{
		public string title;
		public int icon1Index, icon2Index;
		public bool expanded;
		public float nonExpandedSize;
		public readonly Color color = Utils.GetRandomColor();
	}

	
	// This in almost all cases will contain the prefab and your list of models
	[Serializable] // serializable, so it can be shown in inspector
	public class MyParams : BaseParamsWithPrefabAndLazyData<ExampleItemModel>
	{
		public Texture2D[] availableIcons; // used to randomly generate models;
		public int initialSimulatedServerDelay = 0;
	}

	
	public class MyItemViewsHolder : BaseItemViewsHolder
	{
		public Text titleText;
		public Image backgroundImage;
		public RawImage icon1Image, icon2Image;
		internal ExpandCollapseOnClick expandOnCollapseComponent;

		/// <inheritdoc/>
		public override void CollectViews()
		{
			base.CollectViews();

			root.GetComponentAtPath("TitlePanel/TitleText", out titleText);
			root.GetComponentAtPath("Background", out backgroundImage);
			root.GetComponentAtPath("Icon1Image", out icon1Image);
			root.GetComponentAtPath("Icon2Image", out icon2Image);
			expandOnCollapseComponent = root.GetComponent<ExpandCollapseOnClick>();
		}
	}
}
