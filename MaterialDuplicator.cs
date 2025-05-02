using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class MaterialDuplicator : EditorWindow
{
    /// <summary> _serializedObject </summary>
    private SerializedObject _serializedObject;
    
    /// <summary> 複製するマテリアルのList </summary>
    [SerializeField]
    private List<Material> _materials;
    
    /// <summary> 複製するマテリアルのListのSerializedProperty </summary>
    private SerializedProperty _materialsProperty;

    /// <summary>
    /// 複製したマテリアルを保存する場所
    /// </summary>
    private string _duplicateMaterialSaveFolder;
    
    /// <summary>
    /// 複製したテクスチャを保存する場所
    /// </summary>
    private string _duplicateTextureSaveFolder;
    
    /// <summary>
    /// ウィンドウを出す
    /// </summary>
    [MenuItem("AyahaGraphicDevelopTools/MaterialDuplicator")]
    public static void ShowWindow()
    {
        var window = GetWindow<MaterialDuplicator>("MaterialDuplicator");
        window.titleContent = new GUIContent("MaterialDuplicator");
    }

    private void OnEnable()
    {
        _serializedObject = new SerializedObject(this);
        _materialsProperty = _serializedObject.FindProperty("_materials");
    }

    private void OnGUI()
    {
        _serializedObject.Update();
        
        ViewTargetMaterialList();

        ViewDuplicateButton();
        
        _serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 対象となるマテリアルのList
    /// </summary>
    private void ViewTargetMaterialList()
    {
        EditorGUILayout.PropertyField(_materialsProperty, new GUIContent("対象マテリアルリスト"), true);
    }

    /// <summary>
    /// 複製ボタン
    /// </summary>
    private void ViewDuplicateButton()
    {
        if (GUILayout.Button("複製"))
        {
            if (_materialsProperty.arraySize == 0 || _materialsProperty == null)
            {
                return;
            }

            CreateDirectory();

            for (int i = 0; i < _materialsProperty.arraySize; i++)
            {
                var targetMat = _materialsProperty.GetArrayElementAtIndex(i);
                DuplicateMaterialAndTexture(targetMat.objectReferenceValue as Material);
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"Duplicated Material and its textures to {_duplicateMaterialSaveFolder}");
        }
    }

    /// <summary>
    /// 複製したデータを保存するディレクトリを生成する
    /// </summary>
    private void CreateDirectory()
    {
        // フォルダの準備
        _duplicateMaterialSaveFolder = "Assets/Material/Copied";
        _duplicateTextureSaveFolder = _duplicateMaterialSaveFolder + "/Textures";
        if (!AssetDatabase.IsValidFolder(_duplicateMaterialSaveFolder))
            Directory.CreateDirectory(_duplicateMaterialSaveFolder);
        if (!AssetDatabase.IsValidFolder(_duplicateTextureSaveFolder))
            Directory.CreateDirectory(_duplicateTextureSaveFolder);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// マテリアルと参照しているテクスチャを複製する
    /// </summary>
    /// <param name="targetMaterial">対象マテリアル</param>
    private void DuplicateMaterialAndTexture(Material targetMaterial)
    {
        // マテリアルのパスと複製先パス
        string originalMatPath = AssetDatabase.GetAssetPath(targetMaterial);
        string matName = Path.GetFileNameWithoutExtension(originalMatPath);
        string duplicatedMatPath = AssetDatabase.GenerateUniqueAssetPath($"{_duplicateMaterialSaveFolder}/{matName}_Copy.mat");

        // マテリアルを複製
        AssetDatabase.CopyAsset(originalMatPath, duplicatedMatPath);
        AssetDatabase.Refresh();
        var duplicatedMat = AssetDatabase.LoadAssetAtPath<Material>(duplicatedMatPath);

        // プロパティ一覧を取得
        Shader shader = duplicatedMat.shader;
        int propertyCount = ShaderUtil.GetPropertyCount(shader);

        for (int i = 0; i < propertyCount; i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
            {
                continue;
            }

            string propertyName = ShaderUtil.GetPropertyName(shader, i);
            Texture tex = targetMaterial.GetTexture(propertyName);

            if (tex == null)
            {
                continue;
            }

            string texturePath = AssetDatabase.GetAssetPath(tex);
            string textureExt = Path.GetExtension(texturePath);
            string textureName = Path.GetFileNameWithoutExtension(texturePath);
            string newTexturePath = AssetDatabase.GenerateUniqueAssetPath($"{_duplicateTextureSaveFolder}/{textureName}_Copy{textureExt}");

            if (AssetDatabase.CopyAsset(texturePath, newTexturePath))
            {
                AssetDatabase.ImportAsset(newTexturePath);
                Texture newTexture = AssetDatabase.LoadAssetAtPath<Texture>(newTexturePath);
                duplicatedMat.SetTexture(propertyName, newTexture);
            }
        }
    }
}
