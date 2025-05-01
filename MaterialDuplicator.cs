using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MaterialDuplicator : EditorWindow
{
    private Material _selectedMaterial;
    
    /// <summary>
    /// ウィンドウを出す
    /// </summary>
    [MenuItem("AyahaGraphicDevelopTools/MaterialDuplicator")]
    public static void ShowWindow()
    {
        var window = GetWindow<MaterialDuplicator>("MaterialDuplicator");
        window.titleContent = new GUIContent("MaterialDuplicator");
    }
    
    private void OnGUI()
    {
        _selectedMaterial = (Material)EditorGUILayout.ObjectField("Material", _selectedMaterial, typeof(Material), false);

        if (GUILayout.Button("複製"))
        {
            if (_selectedMaterial == null)
            {
                return;
            }
            
            // フォルダの準備
            string materialTargetFolder = "Assets/Material/Copied";
            string textureTargetFolder = materialTargetFolder + "/Textures";
            if (!AssetDatabase.IsValidFolder(materialTargetFolder))
                Directory.CreateDirectory(materialTargetFolder);
            if (!AssetDatabase.IsValidFolder(textureTargetFolder))
                Directory.CreateDirectory(textureTargetFolder);
            AssetDatabase.Refresh();

            // マテリアルのパスと複製先パス
            string originalMatPath = AssetDatabase.GetAssetPath(_selectedMaterial);
            string matName = Path.GetFileNameWithoutExtension(originalMatPath);
            string copiedMatPath = AssetDatabase.GenerateUniqueAssetPath($"{materialTargetFolder}/{matName}_Copy.mat");

            // マテリアルを複製
            AssetDatabase.CopyAsset(originalMatPath, copiedMatPath);
            AssetDatabase.Refresh();
            var copiedMat = AssetDatabase.LoadAssetAtPath<Material>(copiedMatPath);

            // プロパティ一覧を取得
            Shader shader = copiedMat.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                Texture tex = _selectedMaterial.GetTexture(propertyName);

                if (tex == null) continue;

                string texturePath = AssetDatabase.GetAssetPath(tex);
                string textureExt = Path.GetExtension(texturePath);
                string textureName = Path.GetFileNameWithoutExtension(texturePath);
                string newTexturePath = AssetDatabase.GenerateUniqueAssetPath($"{textureTargetFolder}/{textureName}_Copy{textureExt}");

                if (AssetDatabase.CopyAsset(texturePath, newTexturePath))
                {
                    AssetDatabase.ImportAsset(newTexturePath);
                    Texture newTexture = AssetDatabase.LoadAssetAtPath<Texture>(newTexturePath);
                    copiedMat.SetTexture(propertyName, newTexture);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Duplicated Material and its textures to {materialTargetFolder}");
        }
    }
}
