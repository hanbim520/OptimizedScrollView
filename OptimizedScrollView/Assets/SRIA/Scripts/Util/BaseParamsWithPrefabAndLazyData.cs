using System;

namespace UnityEngine.UI.Extension.Tools.Util
{
	/// <summary><see cref="LazyList{T}"/></summary>
	/// <typeparam name="TData">The model type to be used</typeparam>
	public abstract class BaseParamsWithPrefabAndLazyData<TData> : BaseParamsWithPrefab
	{
		public LazyList<TData> Data { get; set; }
		public Func<int, TData> NewModelCreator { get; set; }


		/// <inheritdoc/>
		public override void InitIfNeeded(ISmartScrollView sria)
		{
			base.InitIfNeeded(sria);

			if (Data == null) // this will only be null at init. When scrollview's size changes, the data should remain the same
				Data = new LazyList<TData>(NewModelCreator, 0);
		}
	}
}
