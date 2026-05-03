using Sandbox.UI;

public sealed partial class Player
{
	[Rpc.Host]
	public void RequestBuyPrinter( string definitionPath )
	{
		var definition = MoneyPrinterDefinition.Get( definitionPath );
		if ( Rpc.Caller != Network.Owner || definition is null || definition.Prefab is null )
			return;

		if ( MoneyPrinter.CountOwned( Network.Owner ) >= MoneyPrinterDefinition.MaxOwnedPerPlayer )
		{
			Notices.SendNotice( Network.Owner, "block", Color.Red, $"Vous possédez déjà {MoneyPrinterDefinition.MaxOwnedPerPlayer} imprimantes.", 3 );
			return;
		}

		if ( !TryTakeMoney( definition.Price ) )
		{
			Notices.SendNotice( Network.Owner, "block", Color.Red, "Vous n'avez pas assez d'argent.", 3 );
			return;
		}

		if ( MoneyPrinter.TrySpawn( this, definition ) )
		{
			Notices.SendNotice( Network.Owner, "$", Color.Green, $"{definition.Title} achetée.", 3 );
			return;
		}

		GiveMoney( definition.Price );
		Notices.SendNotice( Network.Owner, "block", Color.Red, "Impossible de placer l'imprimante maintenant.", 3 );
	}
}
