using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.Logic.Misc.Other.Extensions;
using frame8.ScrollRectItemsAdapter.Util.Drawer;
using frame8.ScrollRectItemsAdapter.Util;

namespace frame8.ScrollRectItemsAdapter.SimpleLoopingSpinnerExample
{
	/// <summary>
	/// Very basic example with a spinner that loops its items, similarly to a time picker in an alarm app.
	/// Minimal implementation of the adapter that initializes & updates the viewsholders. The size of each item is fixed in this case and it's the same as the prefab's
	/// </summary>
	public class SimpleLoopingSpinnerExample : SRIA<MyParams, MyItemViewsHolder>
	{
		#region SRIA implementation
		/// <inheritdoc/>
		protected override void Start()
        {
			base.Start();

			DrawerCommandPanel.Instance.Init(this, false, false, false, false);
			DrawerCommandPanel.Instance.galleryEffectSetting.slider.value = .2f;

			// No adding/removing at the head of the list
			DrawerCommandPanel.Instance.addRemoveOnePanel.button2.gameObject.SetActive(false);
			DrawerCommandPanel.Instance.addRemoveOnePanel.button4.gameObject.SetActive(false);

			//// Disable setCount while snapping animation is running
			//var snapper = GetComponent<Snapper8>();
			//if (snapper)
			//{
			//	snapper.SnappingStarted += () => DrawerCommandPanel.Instance.setCountPanel.button.interactable = false;
			//	snapper.SnappingEndedOrCancelled += () => DrawerCommandPanel.Instance.setCountPanel.button.interactable = true;
			//}

			DrawerCommandPanel.Instance.ItemCountChangeRequested += ChangeItemsCountWithChecks;
			DrawerCommandPanel.Instance.AddItemRequested += _ => ChangeItemsCountWithChecks(GetItemsCount() + 1);
			DrawerCommandPanel.Instance.RemoveItemRequested += _ => ChangeItemsCountWithChecks(GetItemsCount() - 1);
			// Initially set the number of items to the number in the input field
			DrawerCommandPanel.Instance.RequestChangeItemCountToSpecified();
		}

		/// <inheritdoc/>
		protected override void Update()
        {
			base.Update();

            _Params.currentSelectedIndicatorText.text = "Selected: ";
            if (_VisibleItemsCount == 0)
                return;

            int middleVHIndex = _VisibleItemsCount / 2;
            var middleVH = _VisibleItems[middleVHIndex];

            _Params.currentSelectedIndicatorText.text += _Params.GetItemValueAtIndex(middleVH.ItemIndex);
            middleVH.background.color = _Params.selectedColor;

            for (int i = 0; i < _VisibleItemsCount; ++i)
            {
                if (i != middleVHIndex)
					_VisibleItems[i].background.color = _Params.nonSelectedColor;
            }
		}

		/// <inheritdoc/>
		protected override MyItemViewsHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new MyItemViewsHolder();
			instance.Init(_Params.itemPrefab, itemIndex);

			return instance;
		}

		/// <inheritdoc/>
		protected override void UpdateViewsHolder(MyItemViewsHolder newOrRecycled) { newOrRecycled.titleText.text = _Params.GetItemValueAtIndex(newOrRecycled.ItemIndex) + ""; }
		#endregion

		void ChangeItemsCountWithChecks(int newCount)
		{
			int min = 4;
			if (newCount < min)
				newCount = min;

			DrawerCommandPanel.Instance.setCountPanel.inputField.text = newCount + "";
			// Don't allow removal if the remaining items would not be able to loop
			DrawerCommandPanel.Instance.addRemoveOnePanel.button3.interactable = newCount > min;

			ResetItems(newCount);
		}
	}


	[Serializable] // serializable, so it can be shown in inspector
	public class MyParams : BaseParamsWithPrefab
	{
		public int startItemNumber = 0;
		public int increment = 1;
		public Color selectedColor, nonSelectedColor;
		public Text currentSelectedIndicatorText;

		/// <summary>The value of each item is calculated dynamically using its <paramref name="index"/>, <see cref="startItemNumber"/> and the <see cref="increment"/><summary>
		/// <returns>The item's value (the displayed number)</returns>
		public int GetItemValueAtIndex(int index) { return startItemNumber + increment * index; }
	}


	public class MyItemViewsHolder : BaseItemViewsHolder
	{
		public Image background;
		public Text titleText;

		/// <inheritdoc/>
		public override void CollectViews()
		{
			base.CollectViews();

			background = root.GetComponent<Image>();
			titleText = root.GetComponentInChildren<Text>();
		}
	}
}
