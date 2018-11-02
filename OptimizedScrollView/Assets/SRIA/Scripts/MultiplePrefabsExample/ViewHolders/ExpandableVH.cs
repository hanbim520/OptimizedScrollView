using frame8.Logic.Misc.Other.Extensions;
using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using frame8.ScrollRectItemsAdapter.MultiplePrefabsExample.Models;
using frame8.ScrollRectItemsAdapter.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace frame8.ScrollRectItemsAdapter.MultiplePrefabsExample.ViewsHolders
{
    /// <summary>The views holder that can preset an <see cref="ExpandableModel"/>. It demonstrates the flow of data both from the view to the model and vice-versa</summary>
    public class ExpandableVH : BaseVH
    {
        public RemoteImageBehaviour remoteImageBehaviour;
        public ExpandCollapseOnClick expandCollapseOnClickBehaviour;


		/// <inheritdoc/>
		public override void CollectViews()
        {
            base.CollectViews();

			root.GetComponentAtPath("SimpleAvatarPanel/TitleText", out titleText);
			root.GetComponentAtPath("SimpleAvatarPanel/Panel/MaskWithImage/IconRawImage", out remoteImageBehaviour);

			expandCollapseOnClickBehaviour = root.GetComponent<ExpandCollapseOnClick>();
        }

        /// <summary>Can only preset models of type <see cref="ExpandableModel"/></summary>
        public override bool CanPresentModelType(Type modelType) { return modelType == typeof(ExpandableModel); }

		/// <inheritdoc/>
		internal override void UpdateViews(BaseModel model)
        {
            base.UpdateViews(model);

            var modelAsExpandable = model as ExpandableModel;
            remoteImageBehaviour.Load(modelAsExpandable.imageURL);

            // Modify the recycled expand behavior script so it's up-to-date with the model. 
            if (expandCollapseOnClickBehaviour)
            {
                expandCollapseOnClickBehaviour.expanded = modelAsExpandable.expanded;
                expandCollapseOnClickBehaviour.nonExpandedSize = modelAsExpandable.nonExpandedSize;
            }
        }
    }
}
