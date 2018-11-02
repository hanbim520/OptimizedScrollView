using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.ScrollRectItemsAdapter.Util;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using frame8.ScrollRectItemsAdapter.Util.Drawer;
using frame8.Logic.Misc.Other.Extensions;
using frame8.Logic.Misc.Visual.UI;

namespace frame8.ScrollRectItemsAdapter.MainExample
{
    /// <summary>
    /// <para>The main example implementation demonstrating common (not all) functionalities: </para>
    /// <para>- using both a horizontal (also includes optional snapping) and a vertical ScrollRect with a complex prefab, </para>
    /// <para>- changing the item count, adding/removing to/from head/end of the list, </para>
    /// <para>- expanding/collapsing an item, thus demonstrating the possibility of multiple sizes, </para>
    /// <para>- smooth scrolling to an item &amp; optionally doing an action after the animation is done, </para>
    /// <para>- comparing the performance to the default implementation of a ScrollView,</para>
    /// <para>- the use of <see cref="ScrollbarFixer8"/></para>
    /// <para>At the core, everything's the same as in other example implementations, so if something's not clear, check them (SimpleTutorial is a good start)</para>
    /// </summary>
    public class ScrollRectItemsAdapterExample : SRIA<MyParams, ClientItemViewsHolder>, ExpandCollapseOnClick.ISizeChangesHandler
	{
		#region SRIA implementation
		/// <inheritdoc/>
		protected override void Start()
		{
			_Params.InitTextures();
			_Params.NewModelCreator = index => CreateNewModel(index);

			base.Start();

			DrawerCommandPanel.Instance.ItemCountChangeRequested += OnItemCountChangeRequested;
			DrawerCommandPanel.Instance.AddItemRequested += OnAddItemRequested;
			DrawerCommandPanel.Instance.RemoveItemRequested += OnRemoveItemRequested;

			// This will call ChangeItemsCount when both adapters are initialized
			MainExampleHelper.Instance.OnAdapterInitialized();
		}

		/// <inheritdoc/>
		protected override void OnDestroy() { _Params.ReleaseTextures(); }

		/// <inheritdoc/>
		protected override ClientItemViewsHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new ClientItemViewsHolder();
			instance.Init(_Params.itemPrefab, itemIndex);
			if (_Params.itemsAreExpandable)
				instance.expandCollapseComponent.sizeChangesHandler = this;

			return instance;
		}

		/// <inheritdoc/>
		protected override void UpdateViewsHolder(ClientItemViewsHolder newOrRecycled) { newOrRecycled.UpdateViews(_Params); }
		#endregion

		#region ExpandCollapseOnClick.ISizeChangesHandler implementation
		bool ExpandCollapseOnClick.ISizeChangesHandler.HandleSizeChangeRequest(RectTransform rt, float newSize)
		{
			var vh = GetItemViewsHolderIfVisible(rt);

			// If the vh is visible, we update our list of sizes
			if (vh != null)
			{
				RequestChangeItemSizeAndUpdateLayout(vh, newSize, DrawerCommandPanel.Instance.freezeItemEndEdgeToggle.isOn);
				return true;
			}

			return false;
		}

		public void OnExpandedStateChanged(RectTransform rt, bool expanded)
		{
			var vh = GetItemViewsHolderIfVisible(rt);

			// If the vh is visible and the request is accepted, we update the model's "expanded" field
			if (vh != null)
				_Params.Data[vh.ItemIndex].expanded = expanded;
		}
		#endregion

		#region events from DrawerCommandPanel
		void OnAddItemRequested(bool atEnd)
		{
			int index = atEnd ? _Params.Data.Count : 0;
			_Params.Data.Insert(index, 1);
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
			_Params.Data.InitWithNewCount(newCount);
			ResetItems(_Params.Data.Count, DrawerCommandPanel.Instance.freezeContentEndEdgeToggle.isOn);
		}
		#endregion

		ClientModel CreateNewModel(int index)
		{
			var model = new ClientModel()
			{
				avatarImageId = Rand(_Params.sampleAvatars.Count),
				//clientName = _Params.sampleFirstNames[Rand(_Params.sampleFirstNames.Length)] + _Params.sampleLastNames[Rand(_Params.sampleLastNames.Length)],
				clientName = _Params.sampleFirstNames[Rand(_Params.sampleFirstNames.Length)],
				//clientName = "Client #" + index,
				location = _Params.sampleLocations[Rand(_Params.sampleLocations.Length)],
				availability01 = RandF(),
				contractChance01 = RandF(),
				longTermClient01 = RandF(),
				isOnline = Rand(2) == 0,
				//visualSize = _Params.ItemPrefabSize,
				nonExpandedSize = _Params.ItemPrefabSize
			};

			int friendsCount = Rand(10);
			model.friendsAvatarIds = new int[friendsCount];
			for (int i = 0; i < friendsCount; i++)
				model.friendsAvatarIds[i] = Rand(_Params.sampleAvatarsDownsized.Count);

			return model;
		}

		// Utility randomness methods
		int Rand(int maxExcl) { return UnityEngine.Random.Range(0, maxExcl); }
		float RandF(float maxExcl = 1f) { return UnityEngine.Random.Range(0, maxExcl); }
    }


	public class ClientModel
	{
		/// <summary>In a real-world scenario, will be used to retrieve the actual image from an image cacher. Here, it's used as the avatar's index in params' sampleAvatars list</summary>
		public int avatarImageId;
		public int[] friendsAvatarIds; // actually avatar indices
		public string clientName;
		public string location;
		public float availability01, contractChance01, longTermClient01;
		public bool isOnline;

		public float AverageScore01 { get { return (availability01 + contractChance01 + longTermClient01) / 3; } }

		// View size related
		public bool expanded;
		public float nonExpandedSize;
	}


	public class ClientItemViewsHolder : BaseItemViewsHolder
	{
		public Image avatarImage, averageScoreFillImage;
		public Text nameText, locationText, averageScoreText, friendsText;
		public RectTransform availability01Slider, contractChance01Slider, longTermClient01Slider;
		public Text statusText;
		public Transform[] friendsPanels = new Transform[MAX_DISPLAYED_FRIENDS];
		public CanvasGroup[] friendsPanelsCanvasGroups = new CanvasGroup[MAX_DISPLAYED_FRIENDS];
		public Image[] friendsAvatarImages = new Image[MAX_DISPLAYED_FRIENDS];
		public ExpandCollapseOnClick expandCollapseComponent;

		const int MAX_DISPLAYED_FRIENDS = 5;


		/// <inheritdoc/>
		public override void CollectViews()
		{
			base.CollectViews();

			var mainPanel = root.GetChild(0);
			mainPanel.GetComponentAtPath("AvatarPanel", out avatarImage);
			mainPanel.GetComponentAtPath("AvatarPanel/StatusText", out statusText);
			mainPanel.GetComponentAtPath("NameAndLocationPanel/NameText", out nameText);
			mainPanel.GetComponentAtPath("NameAndLocationPanel/LocationText", out locationText);

			var friendsPanel = mainPanel.GetComponentAtPath<RectTransform>("FriendsPanel");
			for (int i = 0; i < MAX_DISPLAYED_FRIENDS; i++)
			{
				var ch = friendsPanels[i] = friendsPanel.GetChild(i);
				friendsPanelsCanvasGroups[i] = ch.GetComponent<CanvasGroup>();
				friendsAvatarImages[i] = ch.GetComponent<Image>();
			}
			friendsPanel.GetComponentAtPath("FriendsText", out friendsText);

			var ratingPanel = root.GetComponentAtPath<RectTransform>("RatingPanel/Panel");
			ratingPanel.GetComponentAtPath("Foreground", out averageScoreFillImage);
			ratingPanel.GetComponentAtPath("Text", out averageScoreText);

			var ratingBreakdownPanel = root.GetComponentAtPath<RectTransform>("RatingBreakdownPanel");
			ratingBreakdownPanel.GetComponentAtPath("AvailabilityPanel/Slider", out availability01Slider);
			ratingBreakdownPanel.GetComponentAtPath("ContractChancePanel/Slider", out contractChance01Slider);
			ratingBreakdownPanel.GetComponentAtPath("LongTermClientPanel/Slider", out longTermClient01Slider);

			expandCollapseComponent = root.GetComponent<ExpandCollapseOnClick>();
		}

		public void UpdateViews(MyParams p)
		{
			var dataModel = p.Data[ItemIndex];

			avatarImage.sprite = p.sampleAvatars[dataModel.avatarImageId];
			nameText.text = dataModel.clientName + "(#" + ItemIndex + ")";
			locationText.text = "  " + dataModel.location;
			UpdateScores(dataModel);
			friendsText.text = dataModel.friendsAvatarIds.Length + (dataModel.friendsAvatarIds.Length == 1 ? " friend" : " friends");
			if (dataModel.isOnline)
			{
				statusText.text = "Online";
				statusText.color = Color.green;
			}
			else
			{
				statusText.text = "Offline";
				statusText.color = Color.white * .8f;
			}

			UpdateFriendsAvatars(dataModel, p);

			if (expandCollapseComponent)
			{
				expandCollapseComponent.expanded = dataModel.expanded;
				expandCollapseComponent.nonExpandedSize = dataModel.nonExpandedSize;
			}
		}

		void UpdateScores(ClientModel dataModel)
		{
			var scale = availability01Slider.localScale;
			scale.x = dataModel.availability01;
			availability01Slider.localScale = scale;

			scale = contractChance01Slider.localScale;
			scale.x = dataModel.contractChance01;
			contractChance01Slider.localScale = scale;

			scale = longTermClient01Slider.localScale;
			scale.x = dataModel.longTermClient01;
			longTermClient01Slider.localScale = scale;

			float avgScore = dataModel.AverageScore01;
			averageScoreFillImage.fillAmount = avgScore;
			averageScoreText.text = (int)(avgScore * 100) + "%";
		}

		void UpdateFriendsAvatars(ClientModel dataModel, MyParams p)
		{
			// Set avatars for friends + set their alpha to 1;
			int i = 0;
			int friendsCount = dataModel.friendsAvatarIds.Length;
			int limit = Mathf.Min(MAX_DISPLAYED_FRIENDS, friendsCount);
			for (; i < limit; i++)
			{
				friendsAvatarImages[i].sprite = p.sampleAvatarsDownsized[dataModel.friendsAvatarIds[i]];
				friendsPanels[i].gameObject.SetActive(true);
			}

			// Hide the rest
			for (; i < MAX_DISPLAYED_FRIENDS; ++i)
				friendsPanels[i].gameObject.SetActive(false);

			// .. but fade the last 2, if the friends count is big enough
			if (friendsCount > MAX_DISPLAYED_FRIENDS - 1)
				friendsPanelsCanvasGroups[MAX_DISPLAYED_FRIENDS - 1].alpha = .1f;
			if (friendsCount > MAX_DISPLAYED_FRIENDS - 2)
				friendsPanelsCanvasGroups[MAX_DISPLAYED_FRIENDS - 2].alpha = .4f;
		}
	}


	[Serializable]
	public class MyParams : BaseParamsWithPrefabAndLazyData<ClientModel>
	{
		public List<Sprite> sampleAvatars;
		public string[] sampleFirstNames;//, sampleLastNames;
		public string[] sampleLocations;
		public bool itemsAreExpandable;

		[NonSerialized]
		public List<Sprite> sampleAvatarsDownsized;


		// Creating sprites with down-sized textures for friends' avatars and new sprites for bigger avatars with 
		// mipmaps turned off (so they'll always look sharp)
		internal void InitTextures()
		{
			sampleAvatarsDownsized = new List<Sprite>(sampleAvatars.Count);
			foreach (var avatar in sampleAvatars)
			{
				var mipPixels = avatar.texture.GetPixels32(Mathf.Min(2, avatar.texture.mipmapCount));
				int len = (int)Math.Sqrt(mipPixels.Length);
				var t = new Texture2D(len, len, TextureFormat.RGBA32, false);
				t.SetPixels32(mipPixels);
				t.Apply();
				var sprite = Sprite.Create(t, new Rect(0, 0, len, len), Vector2.one * .5f);
				sampleAvatarsDownsized.Add(sprite);

				var noMipMapTex = new Texture2D(avatar.texture.width, avatar.texture.height, TextureFormat.RGBA32, false);
				noMipMapTex.SetPixels32(avatar.texture.GetPixels32());
				noMipMapTex.Apply();
				sampleAvatars[sampleAvatars.IndexOf(avatar)] = Sprite.Create(noMipMapTex, avatar.textureRect, Vector2.one * .5f);
			}
		}

		internal void ReleaseTextures()
		{
			foreach (var av in sampleAvatars)
				if (av)
					GameObject.Destroy(av);

			foreach (var av in sampleAvatarsDownsized)
				if (av)
					GameObject.Destroy(av);
		}
	}
}
