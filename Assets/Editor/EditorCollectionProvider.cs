using Plugins.CarX.Modding.Creator.Editor;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEngine;

public class EditorCollectionProvider : ProviderCollection
{
	private static readonly IModFileProvider DefaultFileProvider = new DefaultFileProvider(Application.persistentDataPath);

	public EditorCollectionProvider() : base(GameVersion.GetDefaultFullVersionFormat())
	{

	}

	protected override VersionProvider[] providers { get; set; } =
	{
		new(GameVersion.GetFullVersionFormat(),
			new ModProvider(typeof(ModMeta), new MetaProvider(DefaultFileProvider, string.Empty)),
			new ModProvider(typeof(StaticHierarchyMeta), new HierarchiesMetaProvider(DefaultFileProvider)),
			new ModProvider(typeof(PrefabHierarchyMeta), new PrefabsMetaProvider(DefaultFileProvider)),
			new ModProvider(typeof(UnityPrefabInstance), new ObjMtlExporterProvider(DefaultFileProvider)),
			new ModProvider(typeof(Texture2D), new TexturePngProvider(DefaultFileProvider))
		),
	};
}