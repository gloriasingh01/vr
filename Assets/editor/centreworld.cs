using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class CentreWorld
{
    [MenuItem("Tools/Center World Around XR Origin")]
    public static void Center()
    {
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");

        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin (XR Rig) not found in the active scene.");
            return;
        }

        Vector3 offset = xrOrigin.transform.position;

        if (offset == Vector3.zero)
        {
            Debug.Log("XR Origin is already at the world origin.");
            return;
        }

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Center World Around XR Origin");

        foreach (GameObject root in roots)
        {
            if (root == xrOrigin)
                continue;

            if (root.name == "XR Interaction Manager")
                continue;

            Undo.RecordObject(root.transform, "Center World");

            root.transform.position -= offset;
        }

        Undo.RecordObject(xrOrigin.transform, "Center XR Origin");

        xrOrigin.transform.position = Vector3.zero;

        Undo.CollapseUndoOperations(undoGroup);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene()
        );

        Debug.Log("World centered around XR Origin successfully!");
    }
}