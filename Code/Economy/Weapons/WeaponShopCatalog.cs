public sealed class WeaponShopItemDefinition
{
	public WeaponShopItemDefinition( string prefabPath, string title, int price, string description, bool gunDealerOnly = false )
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

public static class WeaponShopCatalog
{
	public const string GunDealerJobDefinitionPath = "jobs/gun_dealer.jobdef";

	static readonly WeaponShopItemDefinition[] Items =
	[
		new( "weapons/crowbar/crowbar.prefab", "Pied-de-biche", 250, "Une arme de mêlée bon marché qui fait mal à bout portant." ),
		new( "weapons/glock/glock.prefab", "USP", 600, "Une arme de poing fiable, précise et efficace à courte portée." ),
		new( "weapons/colt1911/colt1911.prefab", "1911", 750, "Un pistolet plus lourd avec des tirs plus puissants et un chargeur réduit." ),
		new( "weapons/grenade/grenade.prefab", "Grenade", 900, "Un explosif lancé à la main pour déloger les ennemis en zone fermée.", true ),
		new( "weapons/mp5/mp5.prefab", "SMG", 1600, "Un pistolet-mitrailleur à cadence élevée, idéal pour le combat rapproché agressif.", true ),
		new( "weapons/shotgun/shotgun.prefab", "Fusil à Pompe", 2100, "Une arme dévastatrice à courte portée infligeant d'énormes dégâts.", true ),
		new( "weapons/m4a1/m4a1.prefab", "M4A1", 2600, "Un fusil d'assaut polyvalent, efficace dans la plupart des situations.", true ),
		new( "weapons/sniper/sniper.prefab", "Sniper", 3200, "Un fusil haute précision conçu pour les éliminations à longue distance.", true ),
		new( "weapons/rpg/rpg.prefab", "Lance-Roquettes", 10000, "Un lanceur lourd pour une pression explosive massive et dévastatrice.", true )
	];

	public static IReadOnlyList<WeaponShopItemDefinition> GetAll()
	{
		return Items;
	}

	public static WeaponShopItemDefinition Get( string prefabPath )
	{
		if ( string.IsNullOrWhiteSpace( prefabPath ) )
			return null;

		return Items.FirstOrDefault( x => string.Equals( x.PrefabPath, prefabPath, StringComparison.OrdinalIgnoreCase ) );
	}

	public static bool ShouldShowInShop( Player player, WeaponShopItemDefinition item )
	{
		if ( item is null )
			return false;

		return !item.GunDealerOnly || IsGunDealer( player );
	}

	public static bool CanPlayerBuy( Player player, string prefabPath, out string reason )
	{
		reason = null;

		var item = Get( prefabPath );
		if ( item is null )
		{
			reason = "Arme inconnue.";
			return false;
		}

		if ( !item.GunDealerOnly )
			return true;

		if ( player is null )
		{
			reason = "Joueur indisponible.";
			return false;
		}

		if ( !IsGunDealer( player ) )
		{
			reason = "Réservé aux Marchands d'Armes.";
			return false;
		}

		return true;
	}

	public static bool IsGunDealer( Player player )
	{
		var job = player?.CurrentJobDefinition;
		if ( job is null )
			return false;

		if ( string.Equals( job.ResourcePath, GunDealerJobDefinitionPath, StringComparison.OrdinalIgnoreCase ) )
			return true;

		if ( string.Equals( job.Command, "/gundealer", StringComparison.OrdinalIgnoreCase ) )
			return true;

		return string.Equals( job.Title, "Gun Dealer", StringComparison.OrdinalIgnoreCase );
	}
}
