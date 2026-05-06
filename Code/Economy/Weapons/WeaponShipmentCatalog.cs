public sealed class WeaponShipmentItemDefinition
{
	public WeaponShipmentItemDefinition( string weaponPrefabPath, string title, int price, string description, int weaponsPerShipment = 10, bool gunDealerOnly = true )
	{
		WeaponPrefabPath = weaponPrefabPath;
		Title = title;
		Price = price;
		Description = description;
		WeaponsPerShipment = weaponsPerShipment;
		GunDealerOnly = gunDealerOnly;
	}

	public string WeaponPrefabPath { get; }
	public string Title { get; }
	public int Price { get; }
	public string Description { get; }
	public int WeaponsPerShipment { get; }
	public bool GunDealerOnly { get; }
}

public static class WeaponShipmentCatalog
{
	static readonly WeaponShipmentItemDefinition[] Items =
	[
		new( "weapons/glock/glock.prefab", "Cargaison USP", 4800, "Une caisse de 10 pistolets USP à revendre.", 10, true ),
		new( "weapons/colt1911/colt1911.prefab", "Cargaison 1911", 6000, "Une caisse de 10 pistolets Colt 1911 à revendre.", 10, true ),
		new( "weapons/mp5/mp5.prefab", "Cargaison SMG", 12800, "Une caisse de 10 pistolets-mitrailleurs prêts à distribuer.", 10, true ),
		new( "weapons/shotgun/shotgun.prefab", "Cargaison Fusil à Pompe", 16800, "Une caisse de 10 fusils à pompe pour le combat rapproché.", 10, true ),
		new( "weapons/m4a1/m4a1.prefab", "Cargaison M4A1", 20800, "Une caisse de 10 fusils M4A1 pour des équipements lourds.", 10, true ),
		new( "weapons/sniper/sniper.prefab", "Cargaison Sniper", 25600, "Une caisse de 10 fusils de précision pour les longues distances.", 10, true ),
		new( "weapons/rpg/rpg.prefab", "Cargaison Lance-Roquettes", 80000, "Une caisse de 10 lance-roquettes pour le trafic d'armes haut de gamme.", 10, true )
	];

	public static IReadOnlyList<WeaponShipmentItemDefinition> GetAll()
	{
		return Items;
	}

	public static WeaponShipmentItemDefinition Get( string weaponPrefabPath )
	{
		if ( string.IsNullOrWhiteSpace( weaponPrefabPath ) )
			return null;

		return Items.FirstOrDefault( x => string.Equals( x.WeaponPrefabPath, weaponPrefabPath, StringComparison.OrdinalIgnoreCase ) );
	}

	public static bool ShouldShowInShop( Player player, WeaponShipmentItemDefinition item )
	{
		if ( item is null )
			return false;

		return !item.GunDealerOnly || WeaponShopCatalog.IsGunDealer( player );
	}

	public static bool CanPlayerBuy( Player player, string weaponPrefabPath, out string reason )
	{
		reason = null;

		var item = Get( weaponPrefabPath );
		if ( item is null )
		{
			reason = "Cargaison inconnue.";
			return false;
		}

		if ( !item.GunDealerOnly )
			return true;

		if ( player is null )
		{
			reason = "Joueur indisponible.";
			return false;
		}

		if ( !WeaponShopCatalog.IsGunDealer( player ) )
		{
			reason = "Réservé aux Marchands d'Armes.";
			return false;
		}

		return true;
	}
}
