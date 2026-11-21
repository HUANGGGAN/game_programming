#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Migrate existing scene settings; a new field initializer does not replace a saved zero.
[InitializeOnLoad]
public static class BossHealthBarSceneRepair
{
    static BossHealthBarSceneRepair()
    {
        EditorApplication.delayCall += Repair;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) Repair();
        };
    }

    [MenuItem("Tools/Repair Boss Health Bar Settings")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        BossHealthBar[] bars = Resources.FindObjectsOfTypeAll<BossHealthBar>();
        foreach (BossHealthBar bar in bars)
        {
            if (!bar.gameObject.scene.IsValid() || EditorUtility.IsPersistent(bar)) continue;
            var serialized = new SerializedObject(bar);
            SerializedProperty alpha = serialized.FindProperty("visibleAlpha");
            if (alpha == null || alpha.floatValue > 0f) continue;
            Undo.RecordObject(bar, "Restore boss health bar visibility");
            alpha.floatValue = 1f;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(bar.gameObject.scene);
            Debug.Log("Restored Visible Alpha to 1 on " + bar.name, bar);
        }
        foreach (BossManager manager in Resources.FindObjectsOfTypeAll<BossManager>())
        {
            if (!manager.gameObject.scene.IsValid() || EditorUtility.IsPersistent(manager)) continue;
            var serialized = new SerializedObject(manager);
            Object tree = serialized.FindProperty("treeBoss")?.objectReferenceValue;
            SerializedProperty panel = serialized.FindProperty("bossPanel");
            if (tree == null || panel == null || panel.objectReferenceValue != null) continue;
            foreach (BossHealthBar bar in bars)
            {
                if (bar.gameObject.scene != manager.gameObject.scene) continue;
                if (new SerializedObject(bar).FindProperty("treeBoss")?.objectReferenceValue != tree) continue;
                Undo.RecordObject(manager, "Assign boss health bar panel");
                panel.objectReferenceValue = bar.gameObject;
                serialized.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                Debug.Log("Assigned " + bar.name + " as Boss Panel on " + manager.name, manager);
                break;
            }
        }
    }
}
#endif
