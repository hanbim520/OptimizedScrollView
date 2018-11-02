using frame8.Logic.Misc.Visual.UI.ScrollRectItemsAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace frame8.ScrollRectItemsAdapter.Editor.CustomEditors
{
	[CustomEditor(typeof(SRIAStandaloneInputModule))]
	public class SRIAStandaloneInputModuleCustomEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			EditorGUILayout.HelpBox("SRIA: This component is mandatory if building for Universal Windows Platform, but recommended in all cases", MessageType.Info);
		}
	}
}
