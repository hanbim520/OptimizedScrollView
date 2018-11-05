using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEngine.UI.Extension
{
	public interface IScrollRectProxy
	{
		event Action<float> ScrollPositionChanged;

		void SetNormalizedPosition(float normalizedPosition);

		float GetNormalizedPosition();

		float GetContentSize();
	}
}
