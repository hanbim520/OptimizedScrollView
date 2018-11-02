using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.Logic.Misc.Other.Extensions;
using frame8.ScrollRectItemsAdapter.Util.GridView;
using frame8.ScrollRectItemsAdapter.Util;
using frame8.ScrollRectItemsAdapter.Util.Drawer;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using frame8.Logic.Misc.Visual.UI;

namespace frame8.ScrollRectItemsAdapter.SelectionExample
{
    /// <summary>
    /// Implementation demonstrating the usage of a <see cref="GridAdapter{TParams, TCellVH}"/> with support for selecting items on long click & deleting them with a nice collapse animation
    /// </summary>
    public class SelectionExample : GridAdapter<MyGridParams, MyCellViewsHolder>, LongClickableItem.IItemLongClickListener, ExpandCollapseOnClick.ISizeChangesHandler, ILazyListSimpleDataManager<BasicModel>
	{
		#region ILazyListSimpleDataManager implementation
		public LazyList<BasicModel> Data { get; private set; }
		#endregion

		bool SelectionMode
		{
			get { return _SelectionMode; }
			set
			{
				if (_SelectionMode != value)
				{
					SetSelectionMode(value);
					RefreshSelectionStateForVisibleCells();
					UpdateSelectionActionButtons();
				}
			}
		}

		const float SELECTED_SCALE_FACTOR = .8f;
		readonly Vector3 SELECTED_SCALE = new Vector3(SELECTED_SCALE_FACTOR, SELECTED_SCALE_FACTOR, 1f);

		bool _SelectionMode;
		bool waitingForItemsToBeDeleted;
		int _CurrentFreeID = 0;


		#region GridAdapter implementation
		/// <inheritdoc/>
		protected override void Awake()
		{
			Data = new LazyList<BasicModel>(CreateNewModel, 0);

			base.Awake();
		}

		/// <inheritdoc/>
		protected override void Start()
		{
			DrawerCommandPanel.Instance.Init(this, true, false, true, false);

			base.Start();

			DrawerCommandPanel.Instance.galleryEffectSetting.slider.value = 0f;
			DrawerCommandPanel.Instance.ItemCountChangeRequested += OnItemCountChangeRequested;
			DrawerCommandPanel.Instance.AddItemRequested += OnAddItemRequested;
			DrawerCommandPanel.Instance.RemoveItemRequested += OnRemoveItemRequested;

			// Initially set the number of items to the number in the input field
			DrawerCommandPanel.Instance.RequestChangeItemCountToSpecified();

			_Params.deleteButton.onClick.AddListener(() => { if (!PlayPreDeleteAnimation()) DeleteSelectedItems(); }); // delete items directly, if selected item is visible to wait for the delete animation
			_Params.cancelButton.onClick.AddListener(() => SelectionMode = false);
		}

		/// <inheritdoc/>
		protected override void Update()
		{
			base.Update();

			if (Input.GetKeyUp(KeyCode.Escape))
				SelectionMode = false;
		}

		/// <inheritdoc/>
		public override void ChangeItemsCount(ItemCountChangeMode changeMode, int cellsCount, int indexIfAppendingOrRemoving = -1, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
		{
			// Assure nothing is selected before changing the count
			// Update: not calling RefreshSelectionStateForVisibleCells(), since UpdateCellViewsHolder() will be called for all cells anyway
			if (_SelectionMode)
				SetSelectionMode(false);
			UpdateSelectionActionButtons();

			base.ChangeItemsCount(changeMode, cellsCount, indexIfAppendingOrRemoving, contentPanelEndEdgeStationary, keepVelocity);
		}

		/// <summary>
		/// <para><paramref name="contentPanelEndEdgeStationary"/> is overridden by the corresponding setting in the drawer. This is because the <see cref="ILazyListSimpleDataManager{TItem}"/> </para>
		/// <para>calls refresh after any data modification, but it can't know about the drawer panel settings, since it calls the parameterless version of Refresh(), which calls this version</para>
		/// </summary>
		/// <param name="contentPanelEndEdgeStationary">ignored for this demo</param>
		/// <seealso cref="GridAdapter{TParams, TCellVH}.Refresh(bool, bool)"/>
		public override void Refresh(bool contentPanelEndEdgeStationary /*ignored*/, bool keepVelocity = false)
		{
			_CellsCount = Data.Count;
			base.Refresh(DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn, keepVelocity);
		}

		/// <inheritdoc/>
		protected override CellGroupViewsHolder<MyCellViewsHolder> CreateViewsHolder(int itemIndex)
		{
			var cellsGroupVHInstance = base.CreateViewsHolder(itemIndex);

			// Set listeners for the Toggle in each cell. Will call OnCellToggled() when the toggled state changes
			// Set this adapter as listener for the OnItemLongClicked event
			for (int i = 0; i < cellsGroupVHInstance.ContainingCellViewsHolders.Length; ++i)
			{
				var cellVH = cellsGroupVHInstance.ContainingCellViewsHolders[i];
				cellVH.toggle.onValueChanged.AddListener(_ => OnCellToggled(cellVH));
				cellVH.longClickableComponent.longClickListener = this;
			}

			return cellsGroupVHInstance;
		}

		/// <summary> Called when a cell becomes visible </summary>
		/// <param name="viewsHolder"> use viewsHolder.ItemIndexto find your corresponding model and feed data into its views</param>
		/// <see cref="GridAdapter{TParams, TCellVH}.UpdateCellViewsHolder(TCellVH)"/>
		protected override void UpdateCellViewsHolder(MyCellViewsHolder viewsHolder)
		{
			var model = Data[viewsHolder.ItemIndex];
			viewsHolder.UpdateViews(model);

			UpdateSelectionState(viewsHolder, model);
		}

		/// <inheritdoc/>
		protected override void OnBeforeRecycleOrDisableCellViewsHolder(MyCellViewsHolder viewsHolder, int newItemIndex)
		{
			viewsHolder.views.localScale = Vector3.one;
			viewsHolder.expandCollapseComponent.expanded = true;
		}

		void UpdateSelectionState(MyCellViewsHolder viewsHolder, BasicModel model)
		{
			viewsHolder.longClickableComponent.gameObject.SetActive(!_SelectionMode); // can be long-clicked only if selection mode is off
			viewsHolder.toggle.gameObject.SetActive(_SelectionMode); // can be selected only if selection mode is on
			viewsHolder.toggle.isOn = model.isSelected;
		}

		/// <summary>Assumes the current state of SelectionMode is different than <paramref name="active"/></summary>
		void SetSelectionMode(bool active)
		{
			_SelectionMode = active;

			// Don't add/remove items while in selection mode
			DrawerCommandPanel.Instance.addRemoveOnePanel.Interactable = !_SelectionMode;

			// De-select selected items
			for (int i = 0; i < _CellsCount; ++i)
				Data[i].isSelected = false;

			waitingForItemsToBeDeleted = false;
		}

		void UpdateSelectionActionButtons()
		{
			if (!_SelectionMode)
				_Params.deleteButton.interactable = false;

			_Params.cancelButton.interactable = _SelectionMode;
		}

		bool PlayPreDeleteAnimation()
		{
			var numVisibleCells = GetNumVisibleCells();
			for (int i = 0; i < numVisibleCells; ++i)
			{
				var cellVH = GetCellViewsHolder(i);
				var m = Data[cellVH.ItemIndex];
				if (!m.isSelected)
					continue;

				if (cellVH != null)
				{
					waitingForItemsToBeDeleted = true;
					cellVH.expandCollapseComponent.sizeChangesHandler = this;
					cellVH.expandCollapseComponent.OnClicked();
				}
			}

			return waitingForItemsToBeDeleted;
		}

		void RefreshSelectionStateForVisibleCells()
		{
			// Rather than calling Refresh, we retrieve the already-visible ones and update them manually (less lag)
			int visibleCellCount = GetNumVisibleCells();
			for (int i = 0; i < visibleCellCount; ++i)
			{
				var cellVH = GetCellViewsHolder(i);
				UpdateSelectionState(cellVH, Data[cellVH.ItemIndex]);
			}
		}

		void OnCellToggled(MyCellViewsHolder cellVH)
		{
			// Update the model this cell is representing
			var model = Data[cellVH.ItemIndex];
			model.isSelected = cellVH.toggle.isOn;

			cellVH.views.localScale = model.isSelected ? SELECTED_SCALE : Vector3.one;

			// Activate the delete button if at least one item was selected
			if (cellVH.toggle.isOn) // selected
			{
				_Params.deleteButton.interactable = true;
			}
			else // de-selected
			{
				foreach (var m in Data.AsEnumerableForExistingItems)
					if (m.isSelected)
						return;

				// No item selected => disable the delete button
				_Params.deleteButton.interactable = false;
			}
		}

		#region LongClickableItem.IItemLongClickListener implementation
		public void OnItemLongClicked(LongClickableItem longClickedItem)
		{
			// Enter selection mode
			SetSelectionMode(true);
			RefreshSelectionStateForVisibleCells();
			UpdateSelectionActionButtons();

			if (_Params.autoSelectFirstOnSelectionMode)
			{
				// Find the cell views holder that corresponds to the LongClickableItem parameter & mark it as toggled
				int numVisibleCells = base.GetNumVisibleCells();
				for (int i = 0; i < numVisibleCells; ++i)
				{
					var cellVH = base.GetCellViewsHolder(i);

					if (cellVH.longClickableComponent == longClickedItem)
					{
						var model = Data[cellVH.ItemIndex];
						model.isSelected = true;
						UpdateSelectionState(cellVH, model);

						break;
					}
				}
			}
		}
		#endregion

		#region ExpandCollapseOnClick.ISizeChangesHandler implementation
		public bool HandleSizeChangeRequest(RectTransform rt, float newSize) { rt.localScale = SELECTED_SCALE * newSize; return true; }
		public void OnExpandedStateChanged(RectTransform rt, bool expanded)
		{
			if (expanded)
				return;

			if (waitingForItemsToBeDeleted)
			{
				waitingForItemsToBeDeleted = false;

				// Prevent resizing of other items after the CurrentDeleteAnimationDone() was called 
				// (since we only call it for the first OnExpandedStateChanged callback and ignore the following ones)
				var numVisibleCells = GetNumVisibleCells();
				for (int i = 0; i < numVisibleCells; ++i)
				{
					var cellVH = GetCellViewsHolder(i);
					if (cellVH != null)
						cellVH.expandCollapseComponent.sizeChangesHandler = null;
				}

				DeleteSelectedItems();
			}
		}
		#endregion

		#endregion
		
		#region events from DrawerCommandPanel
		void OnAddItemRequested(bool atEnd)
		{
			//if (CellsCount > 0)
			//{
			//	if (atEnd)
			//		_CurrentItemID = this.LazyGetItem(CellsCount - 1).id + 1;
			//	else
			//		_CurrentItemID = this.LazyGetItem(0).id - 1;
			//}

			int index = atEnd ? CellsCount : 0;
			this.LazyInsertItems(index, 1);
		}
		void OnRemoveItemRequested(bool fromEnd)
		{
			if (CellsCount == 0)
				return;

			this.LazyRemoveItemAt(fromEnd ? CellsCount - 1 : 0);
		}
		void OnItemCountChangeRequested(int newCount)
		{
			_CurrentFreeID = 0;
			//// Generating some random models
			//var models = new BasicModel[newCount];
			//for (int i = 0; i < newCount; ++i)
			//	models[i] = CreateNewModel(i);
			this.LazySetNewItems(newCount);
		}
		#endregion

        /// <summary>Deletes the selected items immediately</summary>
		void DeleteSelectedItems()
		{
			var toBeDeleted = new List<BasicModel>();
			foreach (var model in Data.AsEnumerableForExistingItems)
				if (model.isSelected)
					toBeDeleted.Add(model);

			if (toBeDeleted.Count > 0)
			{
				// Remove models from adapter & update views
				this.LazyRemoveItems(toBeDeleted.ToArray());

				// Re-enable selection mode
				if (_Params.keepSelectionModeAfterDeletion)
					SelectionMode = true;

				// "Remove from disk" or similar
				foreach (var item in toBeDeleted)
					HandleItemDeletion(item);
			}
		}

		BasicModel CreateNewModel(int index)
		{
			return new BasicModel()
			{
				id = _CurrentFreeID++,
				//title = "Item ID: " + id,
			};
		}

		void HandleItemDeletion(BasicModel model)
        { Debug.Log("Deleted with id: " + model.id); }
	}


	[Serializable]
	public class MyGridParams : GridParams
	{
		/// <summary>Will be enabled when in selection mode and there are items selsted. Disabled otherwise</summary>
		public Button deleteButton;

		/// <summary>Will be enabled when in selection mode. Pressing it will exit selection mode. Useful for devices with no back/escape (iOS)</summary>
		public Button cancelButton;

		/// <summary>Select the first item when entering selection mode</summary>
		public bool autoSelectFirstOnSelectionMode = true;

		/// <summary>Wether to remain in selection mode after deletion or not</summary>
		public bool keepSelectionModeAfterDeletion = true;
	}


	public class BasicModel
	{
		// Data state
		public int id;
		public readonly Color color = Utils.GetRandomColor(true);

		// View state
		public bool isSelected;
	}


	/// <summary>All views holders used with GridAdapter should inherit from <see cref="CellViewsHolder"/></summary>
	public class MyCellViewsHolder : CellViewsHolder
	{
		public Text title;
		public Toggle toggle;
		public LongClickableItem longClickableComponent;
		public Image background;
		public ExpandCollapseOnClick expandCollapseComponent;


		/// <inheritdoc/>
		public override void CollectViews()
		{
			base.CollectViews();

			toggle = views.Find("Toggle").GetComponent<Toggle>();
			title = views.Find("TitleText").GetComponent<Text>();
			longClickableComponent = views.Find("LongClickableArea").GetComponent<LongClickableItem>();
			background = views.GetComponent<Image>();
			expandCollapseComponent = views.GetComponent<ExpandCollapseOnClick>();
			expandCollapseComponent.nonExpandedSize = .001f;
			expandCollapseComponent.expandFactor = 1 / expandCollapseComponent.nonExpandedSize;
			expandCollapseComponent.expanded = true;
		}

		public void UpdateViews(BasicModel model)
		{
			title.text = "#" + ItemIndex + " [id:" + model.id + "]";
			background.color = model.color;
		}

		/// <inheritdoc/>
		protected override RectTransform GetViews() { return root.Find("Views") as RectTransform; }
	}
}
