using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

public class ActionLog : MonoBehaviour
{
    public static ActionLog instance;
    public static ActionLog GetInstance()
    {
        return instance;
    }

    public TextMeshProUGUI actionLogTxt;
    public ScrollRect scrollRect;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    public void AddText(string text)
    {
        actionLogTxt.text += "\n\n" + text;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
