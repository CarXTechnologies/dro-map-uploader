using System;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public static class BuildOptimizationPanel
    {
        public static void Populate(VisualElement root, MapMetaConfig config, bool english, Action changed)
        {
            root.Unbind(); root.Clear();
            if (config == null) return;
            config.optimization ??= new BuildOptimizationSettings();
            string L(string ru, string en) => english ? en : ru;
            var serialized = new SerializedObject(config);
            var property = serialized.FindProperty("optimization");
            var options = new VisualElement();
            void Field(VisualElement target, string name, string ru, string en)
            {
                var field = new PropertyField(property.FindPropertyRelative(name), L(ru, en));
                field.AddToClassList("mb-field"); target.Add(field);
            }
            Field(root, "enabled", "Оптимизировать сборку", "Optimize build");
            root.Add(options);
            Field(options, "sectorSize", "Размер сектора (м)", "Sector size (m)");
            Field(options, "maxTriangles", "Треугольников на меш", "Max triangles per mesh");
            Field(options, "sectorColliders", "Разбивать статические коллайдеры", "Partition static colliders");
            Field(options, "sectorRenderMeshes", "Разбивать статические меши", "Partition static render meshes");
            Field(options, "mergeCompatibleMeshes", "Объединять совместимые в секторе", "Merge compatible meshes in each sector");
            Field(options, "simplifyColliders", "Упрощать коллайдеры", "Simplify colliders");
            var collision = new VisualElement(); options.Add(collision);
            Field(collision, "colliderTriangleRatio", "Доля треугольников коллизии", "Collider triangle ratio");
            Field(collision, "colliderErrorMeters", "Допуск коллизии (м)", "Collider error tolerance (m)");
            Field(options, "generateSectorLods", "Генерировать LOD секторов", "Generate sector LODs");
            var lods = new VisualElement(); options.Add(lods);
            Field(lods, "lodLevels", "Дополнительных уровней LOD", "Additional LOD levels");
            Field(lods, "lodTriangleRatio", "Доля треугольников на уровень", "Triangle ratio per LOD level");
            Field(lods, "lodErrorMeters", "Допуск LOD (м)", "LOD error tolerance (m)");
            options.Add(new HelpBox(L(
                "Исходная сцена не меняется. Границы секторов сохраняются. Анимации, Rigidbody и авторские LOD не объединяются. Материалы объединяются только при точном совпадении. Допуск упрощения — оценка алгоритма, а не гарантия расстояния до исходной поверхности; проверьте проезд после сборки.",
                "Source scene stays unchanged. Sector borders are preserved. Animations, Rigidbody and authored LODs are excluded. Materials must match exactly. Simplification tolerance is an algorithmic estimate, not a guaranteed surface distance; verify driving after building."), HelpBoxMessageType.Info));
            void Refresh()
            {
                var s = config.optimization;
                options.SetEnabled(s.enabled);
                collision.style.display = s.simplifyColliders && s.sectorColliders ? DisplayStyle.Flex : DisplayStyle.None;
                lods.style.display = s.generateSectorLods && s.sectorRenderMeshes ? DisplayStyle.Flex : DisplayStyle.None;
            }
            var previousKey = config.OptimizationKey;
            root.Bind(serialized); Refresh();
            // Register on fields so rebuilding this panel does not accumulate callbacks on the host element.
            foreach (var field in root.Query<PropertyField>().ToList()) field.RegisterValueChangeCallback(_ =>
            {
                Refresh();
                var currentKey = config.OptimizationKey;
                if (currentKey == previousKey) return;
                previousKey = currentKey;
                EditorUtility.SetDirty(config);
                var manager = MapManagerConfig.instance;
                for (int i = 0; i < manager.builds.Count; i++)
                    if (manager.builds[i].config == config)
                    {
                        var build = manager.builds[i]; build.buildSuccess &= ~(int)TempData.Map; manager.builds[i] = build;
                    }
                EditorUtility.SetDirty(manager);
                Refresh(); changed?.Invoke();
            });
        }
    }
}
