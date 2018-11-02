using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.Logic.Misc.Other.Extensions;
using frame8.ScrollRectItemsAdapter.Util;
using frame8.ScrollRectItemsAdapter.Util.Drawer;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using UnityEngine.EventSystems;
using frame8.Logic.Misc.Visual.UI;

namespace frame8.ScrollRectItemsAdapter.ContentSizeFitterExample
{
	/// <summary>
	/// <para>The prefabhas a disabled ContentSizeFitter added, which will be enabled in <see cref="UpdateViewsHolder(MyItemViewsHolder)"/> </para> 
	/// <para>if the size was not already calculated (in a previous call), then <see cref="SRIA{TParams, TItemViewsHolder}.ScheduleComputeVisibilityTwinPass(bool)"/> should be called. </para> 
	/// <para>After that, as soon as <see cref="UpdateViewsHolder(MyItemViewsHolder)"/> was called for all visible items, you'll receive a callback to <see cref="SRIA{TParams, TItemViewsHolder}.OnItemHeightChangedPreTwinPass(TItemViewsHolder)"/> </para> 
	/// <para>(or <see cref="SRIA{TParams, TItemViewsHolder}.OnItemWidthChangedPreTwinPass(TItemViewsHolder)"/> for horizontal ScrollRects) for each of them,</para> 
	/// <para>where you can disable the content size fitter.</para> 
	/// <para>A "Twin" <see cref="SRIA{TParams, TItemViewsHolder}.ComputeVisibility(double)"/> pass is executed after the current one has finished (meaning <see cref="UpdateViewsHolder(MyItemViewsHolder)"/> was called for all visible items).</para>
	/// </summary>
	public class ContentSizeFitterExample : SRIA<MyParams, MyItemViewsHolder>
	{
		public RectTransformEdgeDragger edgeDragger;

		const string LOREM_IPSUM = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.";


		#region SRIA implementation
		/// <inheritdoc/>
		protected override void Start()
		{
			_Params.NewModelCreator = index => CreateNewModel(index);
			base.Start();

			DrawerCommandPanel.Instance.Init(this, true, false, true, false, true);
			DrawerCommandPanel.Instance.galleryEffectSetting.slider.value = 0f;
			DrawerCommandPanel.Instance.ItemCountChangeRequested += OnItemCountChangeRequested;
			DrawerCommandPanel.Instance.AddItemRequested += OnAddItemRequested;
			DrawerCommandPanel.Instance.RemoveItemRequested += OnRemoveItemRequested;

			//while (EventSystem.current.currentInputModule == null) { Debug.Log("waiting"); yield return null; };

			// Initially set the number of items to the number in the input field
			DrawerCommandPanel.Instance.RequestChangeItemCountToSpecified();
		}

		/// <inheritdoc/>
		protected override MyItemViewsHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new MyItemViewsHolder();
			instance.Init(_Params.itemPrefab, itemIndex);

			return instance;
		}

		/// <inheritdoc/>
		protected override void OnItemHeightChangedPreTwinPass(MyItemViewsHolder vh)
		{
			base.OnItemHeightChangedPreTwinPass(vh);

			_Params.Data[vh.ItemIndex].HasPendingSizeChange = false;
			vh.contentSizeFitter.enabled = false;
		}

		/// <inheritdoc/>
		protected override void UpdateViewsHolder(MyItemViewsHolder newOrRecycled)
		{
			// Initialize the views from the associated model
			ExampleItemModel model = _Params.Data[newOrRecycled.ItemIndex];

			newOrRecycled.UpdateFromModel(model, _Params.availableIcons);

			if (newOrRecycled.contentSizeFitter.enabled)
				newOrRecycled.contentSizeFitter.enabled = false;

			if (model.HasPendingSizeChange)
			{
				// Height will be available before the next 'twin' pass, inside OnItemHeightChangedPreTwinPass() callback (see above)
				newOrRecycled.MarkForRebuild(); // will enable the content size fitter
				ScheduleComputeVisibilityTwinPass(DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
			}
		}

		/// <inheritdoc/>
		protected override void RebuildLayoutDueToScrollViewSizeChange()
		{
			// Invalidate the last sizes so that they'll be re-calculated
			foreach (var model in _Params.Data.AsEnumerableForExistingItems)
				model.HasPendingSizeChange = true;

			base.RebuildLayoutDueToScrollViewSizeChange();
		}
		#endregion

		#region events from DrawerCommandPanel
		void OnAddItemRequested(bool atEnd)
		{
			int index = atEnd ? _Params.Data.Count : 0;
			_Params.Data.Insert(index, 1);
			//_Params.Data.Insert(index, CreateNewModel(index));
			InsertItems(index, 1, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
		}
		void OnRemoveItemRequested(bool fromEnd)
		{
			if (_Params.Data.Count == 0)
				return;

			int index = fromEnd ? _Params.Data.Count - 1 : 0;
			_Params.Data.RemoveAt(index);
			RemoveItems(index, 1, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
		}
		void OnItemCountChangeRequested(int newCount)
		{
			//// Generating some random models
			//var newModels = new ExampleItemModel[newCount];
			//for (int i = 0; i < newCount; ++i)
			//	newModels[i] = CreateNewModel(i);

			//_Params.Data.Clear();
			//_Params.Data.AddRange(newModels);
			_Params.Data.InitWithNewCount(newCount);
			ResetItems(_Params.Data.Count, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
		}
		#endregion

		public ExampleItemModel CreateNewModel(int itemIdex)
		{
			return new ExampleItemModel()
			{
				//Title = LOREM_IPSUM.Substring(400),
				Title = LOREM_IPSUM.Substring(0, UnityEngine.Random.Range(LOREM_IPSUM.Length / 50 + 1, LOREM_IPSUM.Length / 2)),
				IconIndex = UnityEngine.Random.Range(0, _Params.availableIcons.Length)
			};
		}
	}


	public class ExampleItemModel
	{
		/// <summary><see cref="HasPendingSizeChange"/> is set to true each time this property changes</summary>
		public string Title
		{
			get { return _Title; }
			set
			{
				if (_Title != value)
				{
					_Title = value;
					HasPendingSizeChange = true;
				}
			}
		}

		public int IconIndex { get; set; }

		/// <summary>This will be true when the item size may have changed and the ContentSizeFitter component needs to be updated</summary>
		public bool HasPendingSizeChange { get; set; }

		string _Title;

		public ExampleItemModel()
		{
			// By default, the model's size is unknown, so mark it for size re-calculation
			HasPendingSizeChange = true;
		}
	}


	// This in almost all cases will contain the prefab and the list of models
	[Serializable] // serializable, so it can be shown in inspector
	public class MyParams : BaseParamsWithPrefabAndLazyData<ExampleItemModel>
	{
		public Texture2D[] availableIcons; // used to randomly generate models;
	}


	/// <summary>The ContentSizeFitter should be attached to the item itself</summary>
	public class MyItemViewsHolder : BaseItemViewsHolder
	{
		public Text titleText;
		public RawImage icon1Image;

		public ContentSizeFitter contentSizeFitter { get; private set; }


		public override void CollectViews()
		{
			base.CollectViews();

			contentSizeFitter = root.GetComponent<ContentSizeFitter>();
			contentSizeFitter.enabled = false; // the content size fitter should not be enabled during normal lifecycle, only in the "Twin" pass frame
			root.GetComponentAtPath("TitlePanel/TitleText", out titleText);
			root.GetComponentAtPath("Icon1Image", out icon1Image);
		}

		public override void MarkForRebuild()
		{
			base.MarkForRebuild();
			if (contentSizeFitter)
				contentSizeFitter.enabled = true;
		}

		/// <summary>Utility getting rid of the need of manually writing assignments</summary>
		public void UpdateFromModel(ExampleItemModel model, Texture2D[] availableIcons)
		{
			string title = "[#" + ItemIndex + "] " + model.Title;
			if (titleText.text != title)
				titleText.text = title;
			var tex = availableIcons[model.IconIndex];
			if (icon1Image.texture != tex)
				icon1Image.texture = tex;
		}
	}
}
