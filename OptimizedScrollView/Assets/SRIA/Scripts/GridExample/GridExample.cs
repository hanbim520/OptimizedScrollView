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
using frame8.ScrollRectItemsAdapter.MultiplePrefabsExample;
using frame8.Logic.Misc.Visual.UI;

namespace frame8.ScrollRectItemsAdapter.GridExample
{
    /// <summary>
    /// Implementation demonstrating the usage of a <see cref="GridAdapter{TParams, TCellVH}"/> for a simple gallery of remote images downloaded with a <see cref="SimpleImageDownloader"/>.
	/// It implements  <see cref="ILazyListSimpleDataManager{TItem}"/> to access the default interface implementations for common data manipulation functionality
    /// </summary>
    public class GridExample : GridAdapter<GridParams, MyCellViewsHolder>, ILazyListSimpleDataManager<BasicModel>
	{
		public LazyList<BasicModel> Data { get; private set; }


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
			DrawerCommandPanel.Instance.Init(this, true, false, true, false, false);

			base.Start();

			DrawerCommandPanel.Instance.galleryEffectSetting.slider.value = .3f;

			DrawerCommandPanel.Instance.ItemCountChangeRequested += this.LazySetNewItems;
			// Initially set the number of items to the number in the input field
			DrawerCommandPanel.Instance.RequestChangeItemCountToSpecified();
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

		/// <summary> Called when a cell becomes visible </summary>
		/// <param name="viewsHolder"> use viewsHolder.ItemIndexto find your corresponding model and feed data into its views</param>
		protected override void UpdateCellViewsHolder(MyCellViewsHolder viewsHolder)
		{
			var model = Data[viewsHolder.ItemIndex];

			viewsHolder.title.text = "Loading";
			viewsHolder.overlayImage.color = Color.white;
			int itemIndexAtRequest = viewsHolder.ItemIndex;
			var imageURLAtRequest = model.imageURL;
			viewsHolder.iconRemoteImageBehaviour.Load(imageURLAtRequest, true, (fromCache, success) => {
				if (!IsRequestStillValid(viewsHolder.ItemIndex, itemIndexAtRequest, imageURLAtRequest))
					return;

				viewsHolder.overlayImage.CrossFadeAlpha(0f, .5f, false);
				viewsHolder.title.text = model.title;
			});
		}

		bool IsRequestStillValid(int itemIndex, int itemIdexAtRequest, string imageURLAtRequest)
		{
			return
				_CellsCount > itemIndex // be sure the index still points to a valid model
				&& itemIdexAtRequest == itemIndex // be sure the view's associated model index is the same (i.e. the viewsHolder wasn't re-used)
				&& imageURLAtRequest == Data[itemIndex].imageURL; // be sure the model at that index is the same (could have changed if ChangeItemCountTo would've been called meanwhile)
		}
		#endregion

		BasicModel CreateNewModel(int index)
		{
			return new BasicModel()
			{
				title = "Item " + index,
				imageURL = C.GetRandomSmallImageURL()
			};
		}
	}


	public class BasicModel
	{
		public string title;
		public string imageURL;
	}


	/// <summary>All views holders used with GridAdapter should inherit from <see cref="CellViewsHolder"/></summary>
	public class MyCellViewsHolder : CellViewsHolder
	{
		public RemoteImageBehaviour iconRemoteImageBehaviour; // using a raw image because it works with less code when we already have a Texture2D (downloaded from www with SimpleImageDownloader)
		public Image loadingProgress, overlayImage;
		public Text title;


		public override void CollectViews()
		{
			base.CollectViews();

			views.GetComponentAtPath("IconRawImage", out iconRemoteImageBehaviour);
			views.GetComponentAtPath("OverlayImage", out overlayImage);
			views.GetComponentAtPath("LoadingProgressImage", out loadingProgress);
			views.GetComponentAtPath("TitleText", out title);
		}

		protected override RectTransform GetViews() { return root.Find("Views") as RectTransform; }
	}
}
