public sealed class AmmoShopItemDefinition
{
	public AmmoShopItemDefinition( string prefabPath, string title, int price, string description, bool gunDealerOnly = false )
	{
		PrefabPath = prefabPath;
		Title = title;
		Price = price;
		Description = description;
		GunDealerOnly = gunDealerOnly;
	}

	public string PrefabPath { get; }
	public string Title { get; }
	public int Price { get; }
	public string Description { get; }
	public bool GunDealerOnly { get; }
}

public static class AmmoShopCatalog
{
	static readonly AmmoShopItemDefinition[] Items =
	[
		new( "entities/pickup/ammo_9mm.prefab", "Munitions Pistolet", 250, "Un pack de 30 munitions pour pistolet." ),
		new( "entities/pickup/ammo_rifle.prefab", "Munitions Fusil", 450, "Un pack de 60 munitions pour fusil." ),
		new( "entities/pickup/ammo_shotgun.prefab", "Munitions Fusil à Pompe", 400, "Un pack de 18 cartouches pour fusil à pompe." ),
		new( "entities/pickup/ammo_rocket.prefab", "Roquettes", 1800, "Deux roquettes pour le lance-roquettes.", true )
	];

	public static IReadOnlyList<AmmoShopItemDefinition> GetAll()
	{
		return Items;
	}

	public static AmmoShopItemDefinition Get( string prefabPath )
	{
		if ( string.IsNullOrWhiteSpace( prefabPath ) )
			return null;

		return Items.FirstOrDefault( x => string.Equals( x.PrefabPath, prefabPath, StringComparison.OrdinalIgnoreCase ) );
	}

	public static bool ShouldShowInShop( Player player, AmmoShopItemDefinition item )
	{
		if ( item is null )
			return false;

		return !item.GunDealerOnly || WeaponShopCatalog.IsGunDealer( player );
	}

	public static bool CanPlayerBuy( Player player, string prefabPath, out string reason )
	{
		reason = null;

		var item = Get( prefabPath );
		if ( item is null )
		{
			reason = "Munition inconnue.";
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

	public static bool TryGetPickupAmmo( string prefabPath, out AmmoResource ammoType, out int ammoAmount )
	{
		ammoType = null;
		ammoAmount = 0;

		var prefab = GameObject.GetPrefab( prefabPath );
		var pickup = prefab?.GetComponent<AmmoPickup>( true );
		if ( !pickup.IsValid() || pickup.AmmoType is null || pickup.AmmoAmount <= 0 )
			return false;

		ammoType = pickup.AmmoType;
		ammoAmount = pickup.AmmoAmount;
		return true;
	}
}
