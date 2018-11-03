using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine.UI.Extension.Tools;

namespace UnityEngine.UI.Extension.ScrollRectItemsAdapter.Editor.CustomEditors
{
	[CustomEditor(typeof(SRIATouchInputModule))]
	public class SmartScrollViewTouchInputModuleCustomEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			EditorGUILayout.HelpBox("SRIA: In case a TouchInputModule is needed (older unity versions require both Standard- and Touch InputModules) and building for Universal Windows Platform, this component is mandatory (and just 'recommended' for other platforms)", MessageType.Info);
		}
	}
}
