using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.ScrollRectItemsAdapter.MultiplePrefabsExample.Models;
using frame8.ScrollRectItemsAdapter.MultiplePrefabsExample.ViewsHolders;
using frame8.ScrollRectItemsAdapter.Util;
using frame8.ScrollRectItemsAdapter.Util.Drawer;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using frame8.Logic.Misc.Visual.UI;

namespace frame8.ScrollRectItemsAdapter.MultiplePrefabsExample
{
    /// <summary>
    /// <para>Example implementation demonstrating the use of 2 different views holders, representing 2 different models into their own prefab, with a common Title property, displayed in a Text found on both prefabs. </para>
    /// <para>The only constrain is for the models to have a common ancestor class and for the views holders to also have a common ancestor class</para>
    /// <para>Also, the <see cref="BidirectionalModel"/> is used to demonstrate how the data can flow from the model to the views, but also from the views to the model (i.e. this model updates when the user changes the value of its corresponding slider)</para>
    /// <para>At the core, everything's the same as in other example implementations, so if something's not clear, check them (SimpleTutorial is a good start)</para>
    /// </summary>
    public class MultiplePrefabsExample : SRIA<MyParams, BaseVH>, ExpandCollapseOnClick.ISizeChangesHandler	
	{
        /// <summary>Shows the average of all <see cref="BidirectionalModel.value"/>s in all models of type <see cref="BidirectionalModel"/></summary>
        public Text averageValuesInModelsText;

		/// <summary> Used to only allow one item to be expanded at once </summary>
		int _IndexOfCurrentlyExpandedItem;


		#region SRIA implementation
		/// <inheritdoc/>
		protected override void Start()
		{
			base.Start();

			DrawerCommandPanel.Instance.Init(this, true, true, true,
				false // no server delay command
			);
			DrawerCommandPanel.Instance.galleryEffectSetting.slider.value = 0f;
			DrawerCommandPanel.Instance.ItemCountChangeRequested += OnItemCountChangeRequested;
			DrawerCommandPanel.Instance.AddItemRequested += OnAddItemRequested;
			DrawerCommandPanel.Instance.RemoveItemRequested += OnRemoveItemRequested;
			// Initially set the number of items to the number in the input field
			DrawerCommandPanel.Instance.RequestChangeItemCountToSpecified();
		}

		/// <inheritdoc/>
		protected override void Update()
		{
			base.Update();

			if (_Params == null || _Params.data == null)
			{
				averageValuesInModelsText.text = "0";
				return;
			}

			if (_Params.data.Count > 10000)
			{
				averageValuesInModelsText.text = "too much data";
				averageValuesInModelsText.color = Color.red * .5f;
				return;
			}
			averageValuesInModelsText.color = Color.white;

			// Keeping the update rate the smaller the bigger the data set. Then, clamped between 1 and 60. 
			// This way, the performance of computing the average stays relatively constant, regardless of the data set
			float frameStep = (_Params.data.Count / 100000f);
			int frameStepInt = Mathf.Min(60, Mathf.Max(1, (int)(frameStep * 60)));

			if (Time.frameCount % frameStepInt == 0)
			{
				float avg = 0f;
				int bidiNum = 0;
				foreach (var model in _Params.data)
				{
					var asBidi = model as BidirectionalModel;
					if (asBidi != null)
					{
						++bidiNum;
						avg += asBidi.value;
					}
				}
				averageValuesInModelsText.text = (avg / bidiNum).ToString("0.000");
			}
		}

		/// <inheritdoc/>
		public override void ChangeItemsCount(ItemCountChangeMode changeMode, int itemsCount, int indexIfInsertingOrRemoving = -1, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
		{
			_IndexOfCurrentlyExpandedItem = -1; // at initialization, and each time the item count changes, this should be invalidated

			base.ChangeItemsCount(changeMode, itemsCount, indexIfInsertingOrRemoving, contentPanelEndEdgeStationary, keepVelocity);
		}

		/// <summary>
		/// Creates either a <see cref="BidirectionalVH"/> or a <see cref="ExpandableVH"/>, depending on the type of the model at index <paramref name="itemIndex"/>. Calls <see cref="AbstractViewsHolder.Init(RectTransform, int, bool, bool)"/> on it, which instantiates the prefab etc.
		/// </summary>
		/// <seealso cref="SRIA{TParams, TItemViewsHolder}.CreateViewsHolder(int)"/>
		protected override BaseVH CreateViewsHolder(int itemIndex)
		{
			var modelType = _Params.data[itemIndex].CachedType;// _ModelTypes[itemIndex];
			if (modelType == typeof(BidirectionalModel)) // very efficient type comparison, since typeof() is evaluated at compile-time
			{
				var instance = new BidirectionalVH();
				instance.Init(_Params.bidirectionalPrefab, itemIndex);

				return instance;
			}
			else if (modelType == typeof(ExpandableModel))
			{
				var instance = new ExpandableVH();
				instance.Init(_Params.expandablePrefab, itemIndex);

				instance.expandCollapseOnClickBehaviour.sizeChangesHandler = this;
				instance.expandCollapseOnClickBehaviour.expandFactor = _Params.expandableItemExpandFactor;

				return instance;
			}
			else
				throw new InvalidOperationException("Unrecognized model type: " + modelType.Name);
		}

		/// <inheritdoc/>
		protected override void UpdateViewsHolder(BaseVH newOrRecycled)
		{
			// Initialize/update the views from the associated model
			BaseModel model = _Params.data[newOrRecycled.ItemIndex];
			newOrRecycled.UpdateViews(model);
		}

		/// <summary>Overriding the base implementation, which always returns true. In this case, a views holder is recyclable only if its <see cref="BaseVH.CanPresentModelType(Type)"/> returns true for the model at index <paramref name="indexOfItemThatWillBecomeVisible"/></summary>
		/// <seealso cref="SRIA{TParams, TItemViewsHolder}.IsRecyclable(TItemViewsHolder, int, float)"/>
		protected override bool IsRecyclable(BaseVH potentiallyRecyclable, int indexOfItemThatWillBecomeVisible, float heightOfItemThatWillBecomeVisible)
		{ return potentiallyRecyclable.CanPresentModelType(_Params.data[indexOfItemThatWillBecomeVisible].CachedType); }

		#region ExpandCollapseOnClick.ISizeChangesHandler implementation
		bool ExpandCollapseOnClick.ISizeChangesHandler.HandleSizeChangeRequest(RectTransform rt, float newRequestedSize)
		{
			var vh = GetItemViewsHolderIfVisible(rt);

			// If the vh is visible and the request is accepted, we update our list of sizes
			if (vh != null)
			{
				var modelOfExpandingItem = _Params.data[vh.ItemIndex] as ExpandableModel;
				RequestChangeItemSizeAndUpdateLayout(vh, newRequestedSize, DrawerCommandPanel.Instance.freezeItemEndEdgeToggle.isOn);

				if (_IndexOfCurrentlyExpandedItem != -1)
				{
					var vhAsExpandable = vh as ExpandableVH;
					if (!vhAsExpandable.expandCollapseOnClickBehaviour.expanded) // the item is currently expanding => simultaneously collapse the previously expanded one with the same percentage 
					{
						float expandingItem_ExpandedAmount01 = newRequestedSize / (modelOfExpandingItem.nonExpandedSize * _Params.expandableItemExpandFactor);
						var modelOfExpandedItem = _Params.data[_IndexOfCurrentlyExpandedItem] as ExpandableModel;
						var expandedItemNewRequestedSize =
							Mathf.Lerp(
								modelOfExpandedItem.nonExpandedSize,
								modelOfExpandedItem.nonExpandedSize * _Params.expandableItemExpandFactor,
								1f - expandingItem_ExpandedAmount01 // the previously expanded item grows inversely than the newly expanding one
							);
						RequestChangeItemSizeAndUpdateLayout(_IndexOfCurrentlyExpandedItem, expandedItemNewRequestedSize, DrawerCommandPanel.Instance.freezeItemEndEdgeToggle.isOn);
					}
				}

				return true;
			}

			return false;
		}

		public void OnExpandedStateChanged(RectTransform rt, bool expanded)
		{
			var vh = GetItemViewsHolderIfVisible(rt);

			// If the vh is visible and the request is accepted, we update the model's "expanded" field
			if (vh != null)
			{
				var asExpandableModel = _Params.data[vh.ItemIndex] as ExpandableModel;
				if (asExpandableModel == null)
					throw new UnityException(
						"MultiplePrefabsExample.MyScrollRectAdapter.OnExpandedStateChanged: item model at index " + vh.ItemIndex
						+ " is not of type " + typeof(ExpandableModel).Name + ", as expected by the views holder having this itemIndex. Happy debugging :)");
				asExpandableModel.expanded = expanded;

				if (expanded)
				{
					// Mark previous as non-expanded, if any
					if (_IndexOfCurrentlyExpandedItem != -1)
					{
						var lastExpandedModel = _Params.data[_IndexOfCurrentlyExpandedItem] as ExpandableModel;
						lastExpandedModel.expanded = false;

						// If it's also visible, update its views too
						var collapsedVHIfVisible = GetItemViewsHolderIfVisible(_IndexOfCurrentlyExpandedItem);
						if (collapsedVHIfVisible != null)
							collapsedVHIfVisible.UpdateViews(lastExpandedModel);
					}

					_IndexOfCurrentlyExpandedItem = vh.ItemIndex;
				}
				else if (vh.ItemIndex == _IndexOfCurrentlyExpandedItem) // the currently expanded item was collapsed => invalidate indexOfCurrentlyExpandedItem 
					_IndexOfCurrentlyExpandedItem = -1;
			}
		}
		#endregion

		#endregion
		
		#region events from DrawerCommandPanel
		void OnAddItemRequested(bool atEnd)
		{
			int curCount = _Params.data.Count;
			int index = atEnd ? curCount : 0;
			int id = 0;
			if (curCount > 0)
			{
				if (atEnd)
					id = _Params.data[curCount - 1].id + 1;
				else
					id = _Params.data[0].id - 1;
			}
			_Params.data.Insert(index, CreateNewModel(id));
			InsertItems(index, 1, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
		}
		void OnRemoveItemRequested(bool fromEnd)
		{
			if (_Params.data.Count == 0)
				return;

			int index = fromEnd ? _Params.data.Count - 1 : 0;

			_Params.data.RemoveAt(index);
			RemoveItems(index, 1, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
		}
		void OnItemCountChangeRequested(int newCount)
		{
            // Generating some random models
            var newModels = new BaseModel[newCount];
            for (int i = 0; i < newCount; ++i)
				newModels[i] = CreateNewModel(i);

            _Params.data.Clear();
            _Params.data.AddRange(newModels);
            ResetItems(newModels.Length, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
		}
		#endregion

		BaseModel CreateNewModel(int id)
		{
			BaseModel model;
			Rect prefabRect;
			float initialSize;
			if (UnityEngine.Random.Range(0, 2) == 0)
			{
				prefabRect = _Params.expandablePrefab.rect;
				initialSize = _Params.scrollRect.horizontal ? prefabRect.width : prefabRect.height;
				model = new ExpandableModel() { imageURL = C.GetRandomSmallImageURL(), nonExpandedSize = initialSize };
			}
			else
			{
				prefabRect = _Params.bidirectionalPrefab.rect;
				initialSize = _Params.scrollRect.horizontal ? prefabRect.width : prefabRect.height;
				model = new BidirectionalModel() { value = UnityEngine.Random.Range(-5f, 5f) };
			}
			model.id = id;

			return model;
		}



	}

	/// <summary>
	/// Contains the 2 prefabs associated with the 2 views holders & the data list containing models of the 2 type, stored as <see cref="BaseModel"/>
	/// </summary>
	[Serializable] // serializable, so it can be shown in inspector
	public class MyParams : BaseParams
	{
		public RectTransform bidirectionalPrefab, expandablePrefab;
		public float expandableItemExpandFactor = 2f;

		[NonSerialized]
		public List<BaseModel> data = new List<BaseModel>();
	}
}
