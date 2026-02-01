#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class HandWindow : EditorWindow
{
    private PlayerHandController hand;
    private GameObject cardPrefab;
    private bool removeFirstChecker;

    [MenuItem("Tools/Hand Window")]
    public static void Open() => GetWindow<HandWindow>("Hand Window");

    private void OnGUI()
    {
        hand = (PlayerHandController)EditorGUILayout.ObjectField(
            "Hand Controller", hand, typeof(PlayerHandController), true);

        using (new EditorGUI.DisabledScope(hand == null))
        {
            EditorGUILayout.LabelField("Hand Count", hand.HandCount.ToString());

            cardPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Card Prefab", cardPrefab, typeof(GameObject), false);

            if (GUILayout.Button("Add Prefab To Hand"))
            {
                Undo.RecordObject(hand, "Add Card To Hand");
                //hand.AddCardFromPrefab(cardPrefab);
                EditorUtility.SetDirty(hand);
            }

            EditorGUILayout.Space(6);
            removeFirstChecker = EditorGUILayout.ToggleLeft("Checker: Remove First Card", removeFirstChecker);
            if (removeFirstChecker)
            {
                removeFirstChecker = false;
                Undo.RecordObject(hand, "Remove First Card");
                hand.RemoveFirstCard();
                EditorUtility.SetDirty(hand);
            }
        }
    }
}
#endif
