using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extension;

public class Test : MonoBehaviour {

    public UISmartScrollView uiSmartScrollView;
    public GameObject prefab;
    private List<int> datas = new List<int>();
    // Use this for initialization
    void Start () {
		
        for(int i=0;i< 100;++i)
        {
            datas.Add(i);
        }

        uiSmartScrollView.UpdateScrollView<UILoopSmartItem>(datas, prefab);

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
