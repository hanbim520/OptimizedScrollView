
namespace frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter
{
    /// <summary>The minimal implementation of a Views Holder that can be used with <see cref="SRIA{TParams, TItemViewsHolder}"/></summary>
    public class BaseItemViewsHolder : AbstractViewsHolder
    {
        /// <summary> Only used if the scroll rect is looping, otherwise it's the same as <see cref="AbstractViewsHolder.ItemIndex"/>; See <see cref="BaseParams.loopItems"/></summary>
        public int itemIndexInView;
    }
}
