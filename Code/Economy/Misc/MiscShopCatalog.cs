public sealed class MiscShopItemDefinition
{
	public MiscShopItemDefinition( string prefabPath, string title, int price, string description, string requiredJobDefinitionPath = null, string requiredJobTitle = null )
	{
		PrefabPath = prefabPath;
		Title = title;
		Price = price;
		Description = description;
		RequiredJobDefinitionPath = requiredJobDefinitionPath;
		RequiredJobTitle = requiredJobTitle;
	}

	public string PrefabPath { get; }
	public string Title { get; }
	public int Price { get; }
	public string Description { get; }
	public string RequiredJobDefinitionPath { get; }
	public string RequiredJobTitle { get; }
}

public static class MiscShopCatalog
{
	public const string HoboJobDefinitionPath = Player.HoboJobDefinitionPath;
	public const string MayorJobDefinitionPath = Player.MayorJobDefinitionPath;

	static readonly MiscShopItemDefinition[] Items =
	[
		new( TipJar.PrefabPath, "Pot à Pourboires", 150, "Posez un pot pour que les autres joueurs puissent vous donner de l'argent.", HoboJobDefinitionPath, "Hobo" ),
		new( Lawboard.PrefabPath, "Tableau des Lois", 250, "Posez un panneau public affichant les lois de la ville décrétées par le maire.", MayorJobDefinitionPath, "Mayor" )
	];

	public static IReadOnlyList<MiscShopItemDefinition> GetAll()
	{
		return Items;
	}

	public static MiscShopItemDefinition Get( string prefabPath )
	{
		if ( string.IsNullOrWhiteSpace( prefabPath ) )
			return null;

		return Items.FirstOrDefault( x => string.Equals( x.PrefabPath, prefabPath, StringComparison.OrdinalIgnoreCase ) );
	}

	public static bool ShouldShowInShop( Player player, MiscShopItemDefinition item )
	{
		if ( item is null )
			return false;

		return MeetsJobRequirement( player, item );
	}

	public static bool CanPlayerBuy( Player player, string prefabPath, out string reason )
	{
		reason = null;

		var item = Get( prefabPath );
		if ( item is null )
		{
			reason = "Objet inconnu.";
			return false;
		}

		if ( MeetsJobRequirement( player, item ) )
			return true;

		if ( string.IsNullOrWhiteSpace( item.RequiredJobTitle ) )
		{
			reason = "Joueur indisponible.";
			return false;
		}

		reason = $"Réservé aux {item.RequiredJobTitle}.";
		return false;
	}

	static bool MeetsJobRequirement( Player player, MiscShopItemDefinition item )
	{
		if ( item is null )
			return false;

		if ( string.IsNullOrWhiteSpace( item.RequiredJobDefinitionPath ) )
			return true;

		var job = player?.CurrentJobDefinition;
		if ( job is null )
			return false;

		if ( string.Equals( job.ResourcePath, item.RequiredJobDefinitionPath, StringComparison.OrdinalIgnoreCase ) )
			return true;

		return !string.IsNullOrWhiteSpace( item.RequiredJobTitle )
			&& string.Equals( job.Title, item.RequiredJobTitle, StringComparison.OrdinalIgnoreCase );
	}
}
