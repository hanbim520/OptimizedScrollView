using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extension;
using UnityEngine.UI.Extension.Tools;

public class UILoopSmartItem : UILoopSmartItemBase
{
    const string LOREM_IPSUM = "百度百科是百度公司推出的一部内容开放、自由的网络百科全书平台。其测试版于2006年4月20日上线，正式版在2008年4月21日发布，截至2018年2月，百度百科已经收录了超过1520万词条，参与词条编辑的网友超过644万人，几乎涵盖了所有已知的知识领域";
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
    public override void MarkForRebuild()
    {
        base.MarkForRebuild();
        if (contentSizeFitter)
            contentSizeFitter.enabled = true;
    }

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

    public virtual void UpdateFromModel(IList datas, int index)
    {
        timeText.text = datas[index].ToString();
        text.text = GetRandomContent();

    }
    static string GetRandomContent() { return LOREM_IPSUM.Substring(0, UnityEngine.Random.Range(LOREM_IPSUM.Length / 50 + 1, LOREM_IPSUM.Length / 2)); }
}