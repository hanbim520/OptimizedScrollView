using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extension;
using UnityEngine.UI.Extension.Tools;


namespace UnityEngine.UI.Extension
{
    public class UILoopSmartItem : UILoopSmartItemBase
    {
        bool _IsAnimating;
  //      private ContentSizeFitter contentSizeFitter;
        public bool IsPopupAnimationActive
        {
            get { return _IsAnimating; }
            set
            {
                _IsAnimating = value;
            }
        }

       
        public override void MarkForRebuild()
        {
            base.MarkForRebuild();
        //    if (contentSizeFitter)
       //         contentSizeFitter.enabled = true;
        }

        public override void InitialChild()
        {
            base.InitialChild();
        }

        public virtual void UpdateFromModel(IList datas, int index)
        {
        }
    }
}
