using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using frame8.ScrollRectItemsAdapter;
using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.ScrollRectItemsAdapter.Util.Drawer;
using frame8.ScrollRectItemsAdapter.Util;
using System;

namespace frame8.ScrollRectItemsAdapter.MainExample
{
    /// <summary>Helper that manages 2 adapters at once and delegates commands from the drawer panel to both of them</summary>
    public class MainExampleHelper : MonoBehaviour
    {
		public static MainExampleHelper Instance { get; private set; }

		ButtonWithInputPanel _ScrollToAndResizeSetting;
		ScrollRectItemsAdapterExample[] _Adapters;
		int _InitializedAdapters;


		void Start()
        {
			Instance = this;

			var drawer = DrawerCommandPanel.Instance;
			_Adapters = FindObjectsOfType<ScrollRectItemsAdapterExample>();
			drawer.Init(
				_Adapters, 
				true, true, true,
				false
			);

			var updateMode = _Adapters[0].Parameters.updateMode;
			for (int i = 1; i < _Adapters.Length; i++)
			{
				if (_Adapters[i].Parameters.updateMode != updateMode)
				{
					Debug.Log("Different update mode for other adapters.. setting all of them to " + updateMode);
					_Adapters[i].Parameters.updateMode = updateMode;
				}
			}

			AddLoadNonOptimizedExampleButton();

			// The items are expandable. 
			//drawer.AddLabelWithTogglePanel("Use different sizes").toggle.onValueChanged.AddListener(_ => drawer.RequestChangeItemCountToSpecified());

			var scrollToAndResizeSetting = drawer.AddButtonWithInputPanel("ScrollTo & Resize");
			scrollToAndResizeSetting.button.onClick.AddListener(() =>
            {
				int location = scrollToAndResizeSetting.InputFieldValueAsInt;
				if (location < 0)
					return;

				drawer.RequestSmoothScrollTo(
					location, 
					() =>
					{
						foreach (var a in _Adapters) // using foreach because can't access GetItemViewsHolderIfVisible via IScrollRectItemsAdapter8
						{
							var vh = a.GetItemViewsHolderIfVisible(location);
							if (vh != null && vh.expandCollapseComponent != null)
								vh.expandCollapseComponent.OnClicked();
						}
					}
				);
			});
			scrollToAndResizeSetting.transform.SetSiblingIndex(4);

			var updateModePanel = drawer.AddLabelWithTogglesPanel("UpdateMode", "Default", "OnScroll", "Update");

			// Activate the corresponding toggle for the update mode set in params
			updateModePanel.subItems[(int)updateMode].toggle.isOn = true;

			updateModePanel.ToggleChanged += (idx, isOn) =>
			{
				if (!isOn)
					return;
				drawer.DoForAllAdapters(adapter => adapter.BaseParameters.updateMode = (BaseParams.UpdateMode)idx);
			};


			// Commented: not that important and it bulks the controls which are already too stuffy
			//var scrollToImmediate = DrawerCommandPanel.Instance.AddButtonWithInputPanel("ScrollToImmediate");
			//scrollToImmediate.button.onClick.AddListener(() =>
			//{
			//	if (scrollToImmediate.InputFieldValueAsInt >= 0 && scrollToImmediate.InputFieldValueAsInt < adapters[0].GetItemCount())
			//		drawer.DoForAllAdapters((adapter, _) => 
			//			adapter.ScrollTo(
			//				scrollToImmediate.InputFieldValueAsInt,
			//				drawer.scrollToPanel.gravityAdvPanel.InputFieldValueAsFloat,
			//				drawer.scrollToPanel.itemPivotAdvPanel.InputFieldValueAsFloat
			//			)
			//		);
			//});
			//scrollToImmediate.transform.SetSiblingIndex(DrawerCommandPanel.Instance.scrollToPanel.transform.GetSiblingIndex() + 1);
		}

		public void OnAdapterInitialized()
		{
			if (++_InitializedAdapters == _Adapters.Length)
				// This should be called here, since we have 2 instances of ScrollRectItemsAdapterExample
				DrawerCommandPanel.Instance.RequestChangeItemCountToSpecified();
		}

		void AddLoadNonOptimizedExampleButton()
		{
			var buttons = DrawerCommandPanel.Instance.AddButtonsPanel("Compare to classic ScrollView");
			buttons.button1.gameObject.AddComponent<LoadSceneOnClick>().sceneName = "non_optimized_example";
			buttons.button1.image.color = DrawerCommandPanel.Instance.backButtonBehavior.GetComponent<Image>().color;
			var backButtonText = DrawerCommandPanel.Instance.backButtonBehavior.GetComponentInChildren<Text>();
			var loadNonOptimizedButtonText = buttons.button1.GetComponentInChildren<Text>();
			loadNonOptimizedButtonText.font = backButtonText.font;
			loadNonOptimizedButtonText.resizeTextForBestFit = backButtonText.resizeTextForBestFit;
			loadNonOptimizedButtonText.fontStyle = backButtonText.fontStyle;
			loadNonOptimizedButtonText.fontSize = backButtonText.fontSize;
			buttons.transform.SetAsFirstSibling();
		}
	}
}
