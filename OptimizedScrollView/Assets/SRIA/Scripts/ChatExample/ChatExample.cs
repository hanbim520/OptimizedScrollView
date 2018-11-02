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

namespace frame8.ScrollRectItemsAdapter.ChatExample
{
	/// <summary>This class demonstrates a basic chat implementation. A message can contain a text, image, or both</summary>
	public class ChatExample : SRIA<MyParams, ChatMessageViewsHolder>
	{
		const string LOREM_IPSUM = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.";
		

		#region SRIA implementation
		/// <inheritdoc/>
		protected override void Start()
		{
			base.Start();

			DrawerCommandPanel.Instance.Init(this, false, false, false, false, true);
			DrawerCommandPanel.Instance.galleryEffectSetting.slider.value = .04f;

			// No adding/removing at the head of the list
			DrawerCommandPanel.Instance.addRemoveOnePanel.button2.gameObject.SetActive(false);
			DrawerCommandPanel.Instance.addRemoveOnePanel.button4.gameObject.SetActive(false);

			// No removing whatsoever. Only adding
			DrawerCommandPanel.Instance.addRemoveOnePanel.button3.gameObject.SetActive(false);

			//DrawerCommandPanel.Instance.ItemCountChangeRequested += OnItemCountChangeRequested;
			DrawerCommandPanel.Instance.AddItemRequested += OnAddItemRequested;
			//DrawerCommandPanel.Instance.RemoveItemRequested += OnRemoveItemRequested;

			OnItemCountChangeRequested(3);
		}

		/// <inheritdoc/>
		protected override void Update()
		{
			base.Update();

			foreach (var visibleVH in _VisibleItems)
				if (visibleVH.IsPopupAnimationActive)
					visibleVH.UpdatePopupAnimation();
		}

		/// <inheritdoc/>
		protected override ChatMessageViewsHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new ChatMessageViewsHolder();
			instance.Init(_Params.itemPrefab, itemIndex);

			return instance;
		}

		/// <inheritdoc/>
		protected override void OnItemHeightChangedPreTwinPass(ChatMessageViewsHolder vh)
		{
			base.OnItemHeightChangedPreTwinPass(vh);

			_Params.Data[vh.ItemIndex].HasPendingVisualSizeChange = false;
			//vh.ContentFitPending = false;
			vh.contentSizeFitter.enabled = false;
		}

		/// <inheritdoc/>
		protected override void UpdateViewsHolder(ChatMessageViewsHolder newOrRecycled)
		{
			// Initialize the views from the associated model
			ChatMessageModel model = _Params.Data[newOrRecycled.ItemIndex];

			newOrRecycled.UpdateFromModel(model, _Params);

			if (newOrRecycled.contentSizeFitter.enabled)
				newOrRecycled.contentSizeFitter.enabled = false;

			if (model.HasPendingVisualSizeChange)
			{
				// Height will be available before the next 'twin' pass, inside OnItemHeightChangedPreTwinPass() callback (see above)
				newOrRecycled.MarkForRebuild(); // will enable the content size fitter
												//newOrRecycled.contentSizeFitter.enabled = true;
				ScheduleComputeVisibilityTwinPass(true);
			}
			if (!newOrRecycled.IsPopupAnimationActive && newOrRecycled.itemIndexInView == GetItemsCount() - 1) // only animating the last one
				newOrRecycled.IsPopupAnimationActive = true;
		}

		/// <inheritdoc/>
		protected override void OnBeforeRecycleOrDisableViewsHolder(ChatMessageViewsHolder inRecycleBinOrVisible, int newItemIndex)
		{
			inRecycleBinOrVisible.IsPopupAnimationActive = false;

			base.OnBeforeRecycleOrDisableViewsHolder(inRecycleBinOrVisible, newItemIndex);
		}

		/// <inheritdoc/>
		protected override void RebuildLayoutDueToScrollViewSizeChange()
		{
			// Invalidate the last sizes so that they'll be re-calculated
			foreach (var model in _Params.Data)
				model.HasPendingVisualSizeChange = true;

			base.RebuildLayoutDueToScrollViewSizeChange();
		}
		#endregion

		#region events from DrawerCommandPanel
		void OnAddItemRequested(bool atEnd)
		{
			int index = atEnd ? _Params.Data.Count : 0;
			_Params.Data.Insert(index, CreateNewModel(index));
			InsertItems(index, 1, true);
		}
		void OnItemCountChangeRequested(int newCount)
		{
			// Generating some random models
			var newModels = new ChatMessageModel[newCount];
			for (int i = 0; i < newCount; ++i)
				newModels[i] = CreateNewModel(i, i != 1); // the second model will always have an image, for demo purposes

			_Params.Data.Clear();
			_Params.Data.AddRange(newModels);
			ResetItems(_Params.Data.Count, true);
		}
		#endregion

		ChatMessageModel CreateNewModel(int itemIdex, bool addImageOnlyRandomly = true)
		{
			return new ChatMessageModel()
			{
				timestampSec = (Int32)(DateTime.UtcNow.Subtract(ChatMessageModel.EPOCH_START_TIME)).TotalSeconds,
				//Title = LOREM_IPSUM.Substring(400),
				Text = GetRandomContent(),
				IsMine = UnityEngine.Random.Range(0, 2) == 0,
				ImageIndex = addImageOnlyRandomly ?
								UnityEngine.Random.Range(-2 * _Params.availableChatImages.Length, _Params.availableChatImages.Length) // twice as many messages without photo as with photo
								: 0
			};
		}

		static string GetRandomContent() { return LOREM_IPSUM.Substring(0, UnityEngine.Random.Range(LOREM_IPSUM.Length / 50 + 1, LOREM_IPSUM.Length / 2)); }
	}


	/// <summary><see cref="HasPendingVisualSizeChange"/> is set to true each time a property that can affect the height changes</summary>
	public class ChatMessageModel
	{
		public static readonly DateTime EPOCH_START_TIME = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

		public int timestampSec;

		public DateTime TimestampAsDateTime
		{
			get
			{
				// Unix timestamp is seconds past epoch
				System.DateTime dtDateTime = EPOCH_START_TIME.AddSeconds(timestampSec).ToLocalTime();
				return dtDateTime;
			}
		}
		public string Text
		{
			get { return _Text; }
			set
			{
				if (_Text == value)
					return;

				_Text = value;
				HasPendingVisualSizeChange = true;
			}
		}
		public int ImageIndex
		{
			get { return _ImageIndex; }
			set
			{
				if (_ImageIndex == value)
					return;

				_ImageIndex = value;
				HasPendingVisualSizeChange = true;
			}
		}
		public bool IsMine { get; set; }

		/// <summary>This will be true when the item size may have changed and the ContentSizeFitter component needs to be updated</summary>
		public bool HasPendingVisualSizeChange { get; set; }

		string _Text;
		int _ImageIndex;
	}


	// This in almost all cases will contain the prefab and your list of models
	[Serializable] // serializable, so it can be shown in inspector
	public class MyParams : BaseParamsWithPrefabAndData<ChatMessageModel>
	{
		public Sprite[] availableChatImages; // used to randomly generate models;
	}


	/// <summary>The ContentSizeFitter should be attached to the item itself</summary>
	public class ChatMessageViewsHolder : BaseItemViewsHolder
	{
		public Text timeText, text;
		public Image leftIcon, rightIcon;
		public Image image;
		public Image messageContentPanelImage;

		public ContentSizeFitter contentSizeFitter { get; private set; }
		public float PopupAnimationStartTime { get; private set; }
		public bool IsPopupAnimationActive
		{
			get { return _IsAnimating; }
			set
			{
				if (value)
				{
					var s = messageContentPanelImage.transform.localScale;
					s.x = 0;
					messageContentPanelImage.transform.localScale = s;
					PopupAnimationStartTime = Time.time;
				}
				else
				{
					messageContentPanelImage.transform.localScale = Vector3.one;
				}

				_IsAnimating = value;
			}
		}

		const float POPUP_ANIMATION_TIME = .2f;

		bool _IsAnimating;
		VerticalLayoutGroup _RootLayoutGroup, _MessageContentLayoutGroup;
		int paddingAtIconSide, paddingAtOtherSide;
		Color colorAtInit;


		public override void CollectViews()
		{
			base.CollectViews();

			_RootLayoutGroup = root.GetComponent<VerticalLayoutGroup>();
			paddingAtIconSide = _RootLayoutGroup.padding.right;
			paddingAtOtherSide = _RootLayoutGroup.padding.left;

			contentSizeFitter = root.GetComponent<ContentSizeFitter>();
			contentSizeFitter.enabled = false; // the content size fitter should not be enabled during normal lifecycle, only in the "Twin" pass frame
			root.GetComponentAtPath("MessageContentPanel", out _MessageContentLayoutGroup);
			messageContentPanelImage = _MessageContentLayoutGroup.GetComponent<Image>();
			messageContentPanelImage.transform.GetComponentAtPath("Image", out image);
			messageContentPanelImage.transform.GetComponentAtPath("TimeText", out timeText);
			messageContentPanelImage.transform.GetComponentAtPath("Text", out text);
			root.GetComponentAtPath("LeftIconImage", out leftIcon);
			root.GetComponentAtPath("RightIconImage", out rightIcon);
			colorAtInit = messageContentPanelImage.color;
		}

		public override void MarkForRebuild()
		{
			base.MarkForRebuild();
			if (contentSizeFitter)
				contentSizeFitter.enabled = true;
		}

		/// <summary>Utility getting rid of the need of manually writing assignments</summary>
		public void UpdateFromModel(ChatMessageModel model, MyParams parameters)
		{
			timeText.text = model.TimestampAsDateTime.ToString("HH:mm");

			string messageText = "[#" + ItemIndex + "] " + model.Text;
			if (text.text != messageText)
				text.text = messageText;

			leftIcon.gameObject.SetActive(!model.IsMine);
			rightIcon.gameObject.SetActive(model.IsMine);
			if (model.ImageIndex < 0)
				image.gameObject.SetActive(false);
			else
			{
				image.gameObject.SetActive(true);
				image.sprite = parameters.availableChatImages[model.ImageIndex];
			}

			if (model.IsMine)
			{
				messageContentPanelImage.rectTransform.pivot = new Vector2(1.4f, .5f);
				messageContentPanelImage.color = new Color(.75f, 1f, 1f, colorAtInit.a);
				_RootLayoutGroup.childAlignment = _MessageContentLayoutGroup.childAlignment = text.alignment = TextAnchor.MiddleRight;
				_RootLayoutGroup.padding.right = paddingAtIconSide;
				_RootLayoutGroup.padding.left = paddingAtOtherSide;
			}
			else
			{
				messageContentPanelImage.rectTransform.pivot = new Vector2(-.4f, .5f);
				messageContentPanelImage.color = colorAtInit;
				_RootLayoutGroup.childAlignment = _MessageContentLayoutGroup.childAlignment = text.alignment = TextAnchor.MiddleLeft;
				_RootLayoutGroup.padding.right = paddingAtOtherSide;
				_RootLayoutGroup.padding.left = paddingAtIconSide;
			}
		}

		internal void UpdatePopupAnimation()
		{
			float elapsed = Time.time - PopupAnimationStartTime;
			float t01;
			if (elapsed > POPUP_ANIMATION_TIME)
				t01 = 1f;
			else
				// Normal in, sin slow out
				t01 = Mathf.Sin((elapsed / POPUP_ANIMATION_TIME) * Mathf.PI / 2);

			var s = messageContentPanelImage.transform.localScale;
			s.x = t01;
			messageContentPanelImage.transform.localScale = s;

			if (t01 == 1f)
				IsPopupAnimationActive = false;

			//Debug.Log("Updating: " + itemIndexInView + ", t01=" + t01 + ", elapsed=" + elapsed);
		}
	}
}
