﻿
namespace UnityEngine.UI.Extension.Tools
{
    /// <summary>The minimal implementation of a Views Holder that can be used with <see cref="SmartScrollView{TParams, UILoopSmartItem}"/></summary>
    public class UILoopSmartItemBase : AbstractViewsBase
    {
        /// <summary> Only used if the scroll rect is looping, otherwise it's the same as <see cref="AbstractViewsBase.ItemIndex"/>; See <see cref="BaseParams.loopItems"/></summary>
        public int cellIndex;
    }
}