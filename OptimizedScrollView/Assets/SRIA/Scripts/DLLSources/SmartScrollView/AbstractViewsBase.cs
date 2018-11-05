
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI.Extension.Collocation
{
    public abstract class AbstractViewsBase
    {
        public RectTransform root;

        public virtual int ItemIndex { get; set; }
		
        public virtual void Init(RectTransform rootPrefab, int itemIndex, bool activateRootGameObject = true, bool callCollectViews = true)
        { Init(rootPrefab.gameObject, itemIndex, activateRootGameObject, callCollectViews); }

        public virtual void Init(GameObject rootPrefabGO, int itemIndex, bool activateRootGameObject = true, bool callCollectViews = true)
        {
            root = (GameObject.Instantiate(rootPrefabGO) as GameObject).transform as RectTransform;
            if (activateRootGameObject)
                root.gameObject.SetActive(true);
            this.ItemIndex = itemIndex;
            root.name = "CellItem";
            if (callCollectViews)
                InitialChild();
        }

        public virtual void InitialChild()
        { }
		public virtual void MarkForRebuild() { if (root) LayoutRebuilder.MarkLayoutForRebuild(root); }
    }
}
