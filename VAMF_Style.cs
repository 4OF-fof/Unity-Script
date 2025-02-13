using UnityEngine;
using UnityEditor;

public static class Style {
    public static GUIStyle title;
    public static GUIStyle subTitle;
    public static GUIStyle divLine;
    public static GUIStyle button;
    public static GUIStyle detailTitle;
    public static GUIStyle detailContentName;
    public static GUIStyle detailEditInfoButton;

    static Style() {
        title = new GUIStyle(EditorStyles.boldLabel);
        title.fontSize = 20;
        title.alignment = TextAnchor.UpperCenter;
        title.margin = new RectOffset(170, 0, 10, 10);

        subTitle = new GUIStyle(EditorStyles.boldLabel);
        subTitle.fontSize = 15;
        subTitle.alignment = TextAnchor.UpperCenter;
        subTitle.margin = new RectOffset(0, 0, 10, 10);

        divLine = new GUIStyle();
        divLine.normal.background = EditorGUIUtility.whiteTexture;
        divLine.margin = new RectOffset(20, 20, 0, 0);
        divLine.fixedHeight = 2;

        button = new GUIStyle(EditorStyles.miniButton);
        button.fontSize = 15;
        button.fixedWidth = 130;
        button.fixedHeight = 20;
        button.margin = new RectOffset(0, 40, 10, 10);

        detailTitle = new GUIStyle(EditorStyles.boldLabel);
        detailTitle.fontSize = 15;
        detailTitle.alignment = TextAnchor.UpperCenter;
        detailTitle.margin = new RectOffset(130, 0, 10, 10);

        detailContentName = new GUIStyle(EditorStyles.boldLabel);
        detailContentName.fontSize = 13;
        detailContentName.margin = new RectOffset(0, 0, 10, 10);

        detailEditInfoButton = new GUIStyle(EditorStyles.miniButton);
        detailEditInfoButton.fontSize = 15;
        detailEditInfoButton.fixedWidth = 130;
        detailEditInfoButton.fixedHeight = 20;
        detailEditInfoButton.alignment = TextAnchor.UpperCenter;
        detailEditInfoButton.margin = new RectOffset(0, 0, 10, 10);
    }
}