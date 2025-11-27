using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using StarForce;

public class JellyUIBuilder
{
    [MenuItem("StarForce/Build Jelly UI")]
    public static void BuildUI()
    {
        // 1. Ensure Directory Exists
        string prefabPath = "Assets/GameMain/UI/JellyUIForm.prefab";
        string dir = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        // 2. Create Canvas Root (if needed for context, but we just need the Form root)
        // We'll create a temporary root object to build the structure
        GameObject root = new GameObject("JellyUIForm");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        // Set basic anchor to stretch
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // 3. Add Script
        JellyUIForm uiForm = root.AddComponent<JellyUIForm>();

        // 4. Create Children
        
        // Level Text
        GameObject levelTextObj = new GameObject("LevelText");
        levelTextObj.transform.SetParent(root.transform, false);
        Text levelText = levelTextObj.AddComponent<Text>();
        levelText.text = "Level 1";
        levelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        levelText.fontSize = 40;
        levelText.alignment = TextAnchor.UpperLeft;
        levelText.color = Color.white;
        RectTransform levelRect = levelTextObj.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0, 1);
        levelRect.anchorMax = new Vector2(0, 1);
        levelRect.pivot = new Vector2(0, 1);
        levelRect.anchoredPosition = new Vector2(20, -20);
        levelRect.sizeDelta = new Vector2(300, 50);

        // Reset Button
        GameObject resetBtnObj = CreateButton("ResetButton", "Reset", root.transform);
        RectTransform resetRect = resetBtnObj.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(1, 1);
        resetRect.anchorMax = new Vector2(1, 1);
        resetRect.pivot = new Vector2(1, 1);
        resetRect.anchoredPosition = new Vector2(-20, -20);

        // Next Level Button
        GameObject nextBtnObj = CreateButton("NextLevelButton", "Next Level >", root.transform);
        RectTransform nextRect = nextBtnObj.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.5f, 0.5f);
        nextRect.anchorMax = new Vector2(0.5f, 0.5f);
        nextRect.pivot = new Vector2(0.5f, 0.5f);
        nextRect.anchoredPosition = new Vector2(0, 0);
        nextRect.sizeDelta = new Vector2(200, 60);
        // Make it stand out
        nextBtnObj.GetComponent<Image>().color = Color.green;
        nextBtnObj.SetActive(false); // Hidden by default

        // Damage Numbers Root
        GameObject damageRoot = new GameObject("DamageNumbers");
        damageRoot.transform.SetParent(root.transform, false);
        RectTransform damageRect = damageRoot.AddComponent<RectTransform>();
        damageRect.anchorMin = Vector2.zero;
        damageRect.anchorMax = Vector2.one;
        damageRect.offsetMin = Vector2.zero;
        damageRect.offsetMax = Vector2.zero;

        // Damage Number Template
        GameObject templateObj = new GameObject("DamageNumberTemplate");
        templateObj.transform.SetParent(root.transform, false);
        Text templateText = templateObj.AddComponent<Text>();
        templateText.text = "999";
        templateText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        templateText.fontSize = 30;
        templateText.alignment = TextAnchor.MiddleCenter;
        templateText.color = Color.white;
        templateObj.AddComponent<Outline>().effectColor = Color.black; // Add outline for visibility
        templateObj.SetActive(false);

        // 5. Save as Prefab
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        
        // 6. Cleanup
        Object.DestroyImmediate(root);

        Debug.Log($"[JellyUIBuilder] Created UI Prefab at: {prefabPath}");
        AssetDatabase.Refresh();
    }

    private static GameObject CreateButton(string name, string label, Transform parent)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 40);

        return btnObj;
    }
}
