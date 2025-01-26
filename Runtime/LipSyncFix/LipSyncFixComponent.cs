using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Numeira
{
    [RequireComponent(typeof(VRCAvatarDescriptor))]
    public sealed class LipSyncFixComponent : MonoBehaviour
    {
        public SkinnedMeshRenderer? Face;
        public string MouthBlendShapeSelector = /* lang=regex */ "^mouth_.*$";

        public string[] LipSyncBlendShapeBlacklist = new string[0];

        public void Reset()
        {
            var descriptor = GetComponent<VRCAvatarDescriptor>();
            if (descriptor != null)
            {
                Face = descriptor.VisemeSkinnedMesh;
            }
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(LipSyncFixComponent))]
    public sealed class LipSyncFixComponentEditor : Editor
    {
        private SerializedProperty? Face;
        private SerializedProperty? MouthBlendShapeSelector;
        private SerializedProperty? LipSyncBlendShapeBlacklist;

        private VRCAvatarDescriptor? avatarDescriptor;

        private ReorderableList? Blacklist;

        public void OnEnable()
        {
            Face = serializedObject.FindProperty(nameof(LipSyncFixComponent.Face));
            MouthBlendShapeSelector = serializedObject.FindProperty(nameof(LipSyncFixComponent.MouthBlendShapeSelector));
            LipSyncBlendShapeBlacklist = serializedObject.FindProperty(nameof(LipSyncFixComponent.LipSyncBlendShapeBlacklist));

            avatarDescriptor = (target as LipSyncFixComponent)!.GetComponent<VRCAvatarDescriptor>();

            Blacklist = new(serializedObject, LipSyncBlendShapeBlacklist)
            {
                onAddCallback = list =>
                {
                    GenericMenu menu = new();

                    menu.AddItem(new GUIContent("New.."), false, () =>
                    {
                        var prop = list.serializedProperty;
                        int index = prop.arraySize;
                        prop.InsertArrayElementAtIndex(index);
                        prop.GetArrayElementAtIndex(index).stringValue = "";
                        prop.serializedObject.ApplyModifiedProperties();
                    });
                    menu.AddSeparator("");

                    foreach (var x in avatarDescriptor.VisemeBlendShapes)
                    {
                        menu.AddItem(new GUIContent(x), false, context =>
                        {
                            var name = context as string;
                            var prop = list.serializedProperty;
                            int index = prop.arraySize;
                            prop.InsertArrayElementAtIndex(index);
                            prop.GetArrayElementAtIndex(index).stringValue = name;
                            prop.serializedObject.ApplyModifiedProperties();
                        }, x);
                    }
                    menu.ShowAsContext();
                },
               drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
               {
                   EditorGUI.DelayedTextField(rect, Blacklist!.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);
               },
               drawHeaderCallback = rect =>
               {
                   EditorGUI.LabelField(rect, "Blacklist");
               },
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(Face);
            EditorGUILayout.PropertyField(MouthBlendShapeSelector);

            Blacklist?.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}

