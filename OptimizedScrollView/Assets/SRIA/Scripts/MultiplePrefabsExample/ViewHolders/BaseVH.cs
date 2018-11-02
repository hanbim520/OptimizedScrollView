using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.ScrollRectItemsAdapter.MultiplePrefabsExample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace frame8.ScrollRectItemsAdapter.MultiplePrefabsExample.ViewsHolders
{
    /// <summary>Includes common functionalities for the 2 viewsholders. <see cref="CanPresentModelType(Type)"/> is implemented in both of them to return wether the views holder can present a model of specific type (that's why we cache the model's type into <see cref="BaseModel.CachedType"/> inside its constructor)</summary>
    public abstract class BaseVH : BaseItemViewsHolder
    {
        public Text titleText;

		/// <inheritdoc/>
        public override void CollectViews()
        {
            base.CollectViews();

            //titleText = root.Find("TitleText").GetComponent<Text>();
        }

        public abstract bool CanPresentModelType(Type modelType);

        /// <summary>Called to update the views from the specified model. Overriden by inheritors to update their own views after casting the model to its known type</summary>
        internal virtual void UpdateViews(BaseModel model)
        {
            //titleText.text = model.title;
            titleText.text = "#"+ItemIndex+" [id:"+model.id + "]";
		}
    }
}
