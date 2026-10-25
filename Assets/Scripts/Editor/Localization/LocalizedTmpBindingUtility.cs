namespace Astrodiver.Editor.Localization
{
    using System;
    using TMPro;
    using UnityEditor;
    using UnityEditor.Events;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Localization.Components;

    public static class LocalizedTmpBindingUtility
    {
        /// <summary>Native LocalizeStringEvent owns ordinary labels. Do not bind a typewriter target.</summary>
        public static LocalizeStringEvent Bind(TMP_Text target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var binding = target.GetComponent<LocalizeStringEvent>();
            if (binding == null) binding = Undo.AddComponent<LocalizeStringEvent>(target.gameObject);
            for (int i = 0; i < binding.OnUpdateString.GetPersistentEventCount(); i++)
                if (binding.OnUpdateString.GetPersistentTarget(i) == target &&
                    binding.OnUpdateString.GetPersistentMethodName(i) == "set_text") return binding;

            Undo.RecordObject(binding, "Bind localized TMP text");
            var setter = typeof(TMP_Text).GetProperty(nameof(TMP_Text.text)).GetSetMethod();
            var callback = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), target, setter);
            UnityEventTools.AddPersistentListener(binding.OnUpdateString, callback);
            binding.OnUpdateString.SetPersistentListenerState(binding.OnUpdateString.GetPersistentEventCount() - 1,
                UnityEventCallState.EditorAndRuntime);
            EditorUtility.SetDirty(binding);
            PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
            return binding;
        }

        [MenuItem("Astrodiver/Localization/Bind Selected TMP")]
        private static void BindSelected()
        {
            var text = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<TMP_Text>();
            if (text == null) { Debug.LogError("Select a GameObject with a TMP_Text component."); return; }
            var binding = Bind(text);
            Selection.activeObject = binding;
            if (binding.StringReference.IsEmpty)
                Debug.Log("Select a String Reference on LocalizeStringEvent. Its On Update String is already bound to TMP.", binding);
            else binding.RefreshString();
        }
    }
}
