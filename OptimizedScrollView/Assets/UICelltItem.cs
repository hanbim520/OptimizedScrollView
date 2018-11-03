using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extension;

public class UICelltItem : UILoopSmartItem
{
    GameObject Datas;
    Data _datas;
    const string LOREM_IPSUM = "百度百科是百度公司推出的一部内容开放、自由的网络百科全书平台。其测试版于2006年4月20日上线，正式版在2008年4月21日发布，截至2018年2月，百度百科已经收录了超过1520万词条，参与词条编辑的网友超过644万人，几乎涵盖了所有已知的知识领域";
    static string GetRandomContent() { return LOREM_IPSUM.Substring(0, UnityEngine.Random.Range(LOREM_IPSUM.Length / 50 + 1, LOREM_IPSUM.Length / 2)); }
    Color colorAtInit;
    Text timeText, text;
    Image leftIcon, rightIcon;
    Image image;
    Image messageContentPanelImage;
    VerticalLayoutGroup _RootLayoutGroup, _MessageContentLayoutGroup;
    int isMe = 1;

    

    public override void InitialChild()
    {
        base.InitialChild();
        _RootLayoutGroup = root.GetComponent<VerticalLayoutGroup>();

        root.GetComponentAtPath("MessageContentPanel", out _MessageContentLayoutGroup);
        messageContentPanelImage = _MessageContentLayoutGroup.GetComponent<Image>();
        messageContentPanelImage.transform.GetComponentAtPath("Image", out image);
        messageContentPanelImage.transform.GetComponentAtPath("TimeText", out timeText);
        messageContentPanelImage.transform.GetComponentAtPath("Text", out text);
        root.GetComponentAtPath("LeftIconImage", out leftIcon);
        root.GetComponentAtPath("RightIconImage", out rightIcon);
        colorAtInit = messageContentPanelImage.color;

        Datas = GameObject.Find("Main Camera");
        _datas = Datas.GetComponent<Data>();
    }

    public override void UpdateFromModel(IList datas, int index)
    {
        base.UpdateFromModel(datas, index);
        List<DataStruct> _data = (List<DataStruct>)datas;
        timeText.text = index.ToString();
        text.text = GetRandomContent();
        int value = _data[index].bigImg;
        if (value < 0)
        {
            image.gameObject.SetActive(false);
        }
        else
        {
            image.gameObject.SetActive(true);
            image.sprite = _datas.BigSpts[value];
            image.SetNativeSize();
        }
       
       
        if(_data[index].isMe)
        {
            leftIcon.gameObject.SetActive(false);
            rightIcon.gameObject.SetActive(true);
            rightIcon.sprite = _datas.FaceSpts[_data[index].headIdx];
        }
        else
        {
            leftIcon.gameObject.SetActive(true);
            rightIcon.gameObject.SetActive(false);
            leftIcon.sprite = _datas.FaceSpts[_data[index].headIdx];
        }
    }
}
