using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tamawatari.Bake
{
    /// <summary>焼き直しツール共通の小物。</summary>
    public static class BakeUtil
    {
        /// <summary>"Assets/..." をディスク上の絶対パスに直す。</summary>
        public static string ToDiskPath(string assetPath)
            => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", assetPath);

        /// <summary>"Assets/A/B/C" を、途中の階層ごと作る。</summary>
        public static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>組み立てたヒエラルキーをプレハブとして保存し、置いた実体は片付ける。</summary>
        public static GameObject SavePrefab(GameObject root, string assetPath)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath, out bool ok);
            if (!ok || prefab == null) Debug.LogError($"[Bake] {assetPath} を保存できませんでした。");
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
