using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extension;

public struct DataStruct
{
    public bool isMe;
    public int headIdx;
    public int bigImg;
}

public class Test : MonoBehaviour {

    public GameObject Datas;
    public UISmartScrollView uiSmartScrollView;
    public GameObject prefab;
    private List<DataStruct> datas = new List<DataStruct>();
    // Use this for initialization
    void Start() {
        Data _datas = Datas.GetComponent<Data>();
        for(int i=0;i< 100;++i)
        {
            DataStruct dt = new DataStruct();
            int isMe = Random.Range(0, 10);

            dt.isMe = (isMe % 2 == 0);
            if(dt.isMe )
            {
                dt.headIdx = 0;
            }else
            {
                dt.headIdx = 1;
            }

            int value = Random.Range(-2, _datas.BigSpts.Length - 1);
            if (value < 0)
            {
                dt.bigImg = 0;
            }
            else
            {
                dt.bigImg = value;
            }
            datas.Add(dt);
        }

        uiSmartScrollView.UpdateScrollView<UICelltItem>(datas, prefab);
    }
	
    public void ScrollToUp()
    {
        uiSmartScrollView.SmoothScrollTo(0,0.5f);
    }
    public void ScrollToDown()
    {
        uiSmartScrollView.SmoothScrollTo(datas.Count - 1, 0.5f);
    }

    public void OnAddItemRequested()
    {
        int preCount = datas.Count;
        Data _datas = Datas.GetComponent<Data>();
        for (int i = 0; i < 2; ++i)
        {
            DataStruct dt = new DataStruct();
            int isMe = Random.Range(0, 10);

            dt.isMe = (isMe % 2 == 0);
            if (dt.isMe)
            {
                dt.headIdx = 0;
            }
            else
            {
                dt.headIdx = 1;
            }

            int value = Random.Range(-2, _datas.BigSpts.Length - 1);
            if (value < 0)
            {
                dt.bigImg = 0;
            }
            else
            {
                dt.bigImg = value;
            }
            datas.Add(dt);
        }
        uiSmartScrollView.AddItemRequested(true, preCount,2);
        uiSmartScrollView.SmoothScrollTo(1,0);
    }
    public void OnRemoveItemRequested()
    {
        uiSmartScrollView.RemoveItemRequested(false, 2);
        int count = datas.Count;
        datas.RemoveRange(count - 3, 2);
        uiSmartScrollView.SmoothScrollTo(1, 0);
    }
}
