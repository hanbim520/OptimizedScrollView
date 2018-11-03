using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extension.Other.Extensions;
using UnityEngine.UI.Extension.Tools;

public class UILoopSmartItem : BaseItemViewsHolder
{
    bool _IsAnimating;
    private ContentSizeFitter contentSizeFitter;
    public bool IsPopupAnimationActive
    {
        get { return _IsAnimating; }
        set
        {
            _IsAnimating = value;
        }
    }

    VerticalLayoutGroup _RootLayoutGroup, _MessageContentLayoutGroup;
    int paddingAtIconSide, paddingAtOtherSide;
    Color colorAtInit;
    Text timeText, text;
    Image leftIcon, rightIcon;
    Image image;
    Image messageContentPanelImage;


    public override void InitialChild()
    {
        base.InitialChild();
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

    public void UpdateFromModel(IList datas, int index)
    {

        timeText.text = datas[index].ToString();


    }
}