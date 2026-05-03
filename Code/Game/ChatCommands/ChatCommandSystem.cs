using Sandbox.UI;

public enum ChatCommandAccess
{
	Everyone,
	Admin,
	SuperAdmin
}

public sealed class ChatCommandPreview
{
	public ChatCommandPreview( string usage, string description, string accessText = null )
	{
		Usage = usage;
		Description = description;
		AccessText = accessText;
	}

	public string Usage { get; }
	public string Description { get; }
	public string AccessText { get; }
}

public sealed class ChatCommandContext
{
	public ChatCommandContext( Connection connection, Player player, Chat chat, string commandName, string argumentsText )
	{
		Connection = connection;
		Player = player;
		Chat = chat;
		CommandName = commandName;
		ArgumentsText = argumentsText?.Trim() ?? string.Empty;
		Arguments = ChatCommandSystem.TokenizeArguments( ArgumentsText );
	}

	public Connection Connection { get; }
	public Player Player { get; }
	public Chat Chat { get; }
	public string CommandName { get; }
	public string ArgumentsText { get; }
	public IReadOnlyList<string> Arguments { get; }

	public void Reply( string message, string icon = "/" )
	{
		Chat?.AddSystemTextTo( Connection, message, icon );
	}

	public void Broadcast( string message, string icon = "/" )
	{
		Chat?.AddSystemText( message, icon );
	}
}

public sealed class ChatCommandDefinition
{
	public ChatCommandDefinition(
		string name,
		string usage,
		string description,
		Action<ChatCommandContext> handler,
		ChatCommandAccess access = ChatCommandAccess.Everyone,
		string[] aliases = null,
		string accessText = null,
		Func<Player, bool> canUse = null )
	{
		Name = ChatCommandSystem.NormalizeCommandName( name );
		Usage = usage;
		Description = description;
		Handler = handler;
		Access = access;
		Aliases = (aliases ?? [])
			.Select( ChatCommandSystem.NormalizeCommandName )
			.Where( x => !string.IsNullOrWhiteSpace( x ) )
			.Distinct( StringComparer.OrdinalIgnoreCase )
			.ToArray();
		AccessText = accessText;
		CanUse = canUse;
	}

	public string Name { get; }
	public string Usage { get; }
	public string Description { get; }
	public Action<ChatCommandContext> Handler { get; }
	public ChatCommandAccess Access { get; }
	public IReadOnlyList<string> Aliases { get; }
	public string AccessText { get; }
	public Func<Player, bool> CanUse { get; }

	public IEnumerable<string> Names
	{
		get
		{
			yield return Name;

			foreach ( var alias in Aliases )
			{
				yield return alias;
			}
		}
	}

	public bool Matches( string commandName )
	{
		return Names.Any( x => string.Equals( x, commandName, StringComparison.OrdinalIgnoreCase ) );
	}
}

public static class ChatCommandSystem
{
	const int MaxSuggestions = 6;

	static readonly ChatCommandDefinition[] StaticCommands =
	[
		new( "advert", "/advert <message>", "Diffuser un message d'annonce.", AdvertCommand, aliases: ["ad"] ),
		new( "ooc", "/ooc <message>", "Parler hors personnage.", OocCommand, aliases: ["//"] ),
		new( "me", "/me <action>", "Décrire une action de jeu de rôle.", MeCommand ),
		new( "pm", "/pm <player> <message>", "Envoyer un message privé.", PrivateMessageCommand, aliases: ["msg", "tell", "w"] ),
		new( "dropmoney", "/dropmoney <amount>", "Laisser tomber de l'argent devant vous.", DropMoneyCommand, aliases: ["dropcash"] ),
		new( "name", "/name <rp name>", "Changer votre nom de jeu de rôle.", NameCommand, aliases: ["rpname", "nick"] ),
		new( "kick", "/kick <player> [reason]", "Expulser un joueur.", KickCommand, ChatCommandAccess.Admin, accessText: "admin" ),
		new( "ban", "/ban <player|steamid> [reason]", "Bannir un joueur.", BanCommand, ChatCommandAccess.SuperAdmin, accessText: "superadmin" ),
		new( "unban", "/unban <steamid>", "Supprimer un bannissement SteamID.", UnbanCommand, ChatCommandAccess.SuperAdmin, accessText: "superadmin" ),
		new( "setadmin", "/setadmin <player> <none|admin|superadmin>", "Changer le rôle d'un joueur.", SetAdminCommand, ChatCommandAccess.SuperAdmin, accessText: "superadmin" ),
		new( "givemoney", "/givemoney <player> <amount>", "Donner de l'argent à un joueur.", GiveMoneyCommand, ChatCommandAccess.Admin, accessText: "admin" ),
		new( "setmoney", "/setmoney <player> <amount>", "Définir l'argent d'un joueur.", SetMoneyCommand, ChatCommandAccess.Admin, accessText: "admin" )
	];

	public static IReadOnlyList<string> TokenizeArguments( string argumentsText )
	{
		if ( string.IsNullOrWhiteSpace( argumentsText ) )
			return [];

		return argumentsText.Split( ' ', StringSplitOptions.RemoveEmptyEntries );
	}

	public static string NormalizeCommandName( string commandName )
	{
		var value = (commandName ?? string.Empty).Trim();
		if ( value == "//" )
			return value;

		return value.TrimStart( '/' ).Trim().ToLowerInvariant();
	}

	public static bool IsCommandInput( string input )
	{
		return !string.IsNullOrWhiteSpace( input ) && input.TrimStart().StartsWith( "/" );
	}

	public static bool TryExecute( Connection caller, string input )
	{
		if ( !Networking.IsHost )
			return false;

		var chat = Game.ActiveScene?.Get<Chat>();
		if ( !TryParseCommand( input, out var commandName, out var argumentsText ) )
			return false;

		if ( string.IsNullOrWhiteSpace( commandName ) )
		{
			chat?.AddSystemTextTo( caller, "Commencez à taper une commande pour voir les suggestions.", "/" );
			return true;
		}

		var player = Player.FindForConnection( caller );
		if ( !player.IsValid() )
			return true;

		var context = new ChatCommandContext( caller, player, chat, commandName, argumentsText );
		var command = FindStaticCommand( commandName );
		if ( command is not null )
		{
			if ( !CanUseCommand( player, command ) )
			{
				context.Reply( "Vous n'avez pas accès à cette commande.", "!" );
				return true;
			}

			command.Handler( context );
			return true;
		}

		context.Reply( "Commande inconnue.", "!" );
		return true;
	}

	public static IReadOnlyList<ChatCommandPreview> GetPreviews( string input, Player player )
	{
		if ( !IsCommandInput( input ) )
			return [];

		if ( !HasCommandPreviewQuery( input ) )
			return [];

		if ( !TryParseCommand( input, out var commandName, out _ ) )
			return [];

		return BuildVisiblePreviews( player, commandName, MaxSuggestions );
	}

	static bool HasCommandPreviewQuery( string input )
	{
		var text = input?.TrimStart();
		if ( string.IsNullOrWhiteSpace( text ) || !text.StartsWith( "/" ) )
			return false;

		return text.Skip( 1 ).Any( char.IsLetter );
	}

	static IReadOnlyList<ChatCommandPreview> BuildVisiblePreviews( Player player, string commandName, int limit )
	{
		var query = NormalizeCommandName( commandName );
		var previews = new List<ChatCommandPreview>();

		foreach ( var command in StaticCommands )
		{
			if ( !CanUseCommand( player, command ) )
				continue;

			if ( !MatchesQuery( command.Names, query ) )
				continue;

			previews.Add( new ChatCommandPreview( command.Usage, command.Description, GetAccessText( command ) ) );
		}

		return previews
			.OrderBy( x => x.Usage )
			.Take( limit )
			.ToArray();
	}

	static bool TryParseCommand( string input, out string commandName, out string argumentsText )
	{
		commandName = null;
		argumentsText = string.Empty;

		var text = input?.TrimStart();
		if ( string.IsNullOrWhiteSpace( text ) || !text.StartsWith( "/" ) )
			return false;

		if ( text.StartsWith( "//" ) )
		{
			commandName = "//";
			argumentsText = text[2..].Trim();
			return true;
		}

		text = text[1..].TrimStart();
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			commandName = string.Empty;
			return true;
		}

		var separator = text.IndexOf( ' ' );
		if ( separator < 0 )
		{
			commandName = NormalizeCommandName( text );
			return true;
		}

		commandName = NormalizeCommandName( text[..separator] );
		argumentsText = text[(separator + 1)..].Trim();
		return true;
	}

	static ChatCommandDefinition FindStaticCommand( string commandName )
	{
		return StaticCommands.FirstOrDefault( x => x.Matches( commandName ) );
	}

	static bool CanUseCommand( Player player, ChatCommandDefinition command )
	{
		if ( command.Access == ChatCommandAccess.Admin && player?.HasAdminAccess != true )
			return false;

		if ( command.Access == ChatCommandAccess.SuperAdmin && player?.HasSuperAdminAccess != true )
			return false;

		return command.CanUse?.Invoke( player ) ?? true;
	}

	static string GetAccessText( ChatCommandDefinition command )
	{
		if ( !string.IsNullOrWhiteSpace( command.AccessText ) )
			return command.AccessText;

		return command.Access switch
		{
			ChatCommandAccess.Admin => "admin",
			ChatCommandAccess.SuperAdmin => "superadmin",
			_ => null
		};
	}

	static bool MatchesQuery( IEnumerable<string> names, string query )
	{
		if ( string.IsNullOrWhiteSpace( query ) )
			return true;

		return names.Any( name =>
		{
			name = NormalizeCommandName( name );
			return name.StartsWith( query, StringComparison.OrdinalIgnoreCase )
				|| name.Contains( query, StringComparison.OrdinalIgnoreCase );
		} );
	}

	static bool TryFindPlayer( string query, out Player player, out string error )
	{
		player = null;
		error = null;

		if ( string.IsNullOrWhiteSpace( query ) )
		{
			error = "Entrez un nom de joueur ou un SteamID.";
			return false;
		}

		var players = Game.ActiveScene.GetAll<Player>()
			.Where( x => x.IsValid() && x.Network.Owner is not null )
			.ToArray();

		if ( long.TryParse( query, out var steamId ) )
		{
			player = players.FirstOrDefault( x => x.SteamId == steamId );
			if ( player.IsValid() )
				return true;
		}

		player = players.FirstOrDefault( x => string.Equals( x.DisplayName, query, StringComparison.OrdinalIgnoreCase ) )
			?? players.FirstOrDefault( x => string.Equals( x.Network.Owner.DisplayName, query, StringComparison.OrdinalIgnoreCase ) );

		if ( player.IsValid() )
			return true;

		var matches = players
			.Where( x => x.DisplayName.Contains( query, StringComparison.OrdinalIgnoreCase )
				|| x.Network.Owner.DisplayName.Contains( query, StringComparison.OrdinalIgnoreCase ) )
			.Take( 5 )
			.ToArray();

		if ( matches.Length == 1 )
		{
			player = matches[0];
			return true;
		}

		error = matches.Length == 0
			? "Joueur non trouvé."
			: $"Plusieurs joueurs correspondent : {string.Join( ", ", matches.Select( x => x.DisplayName ) )}.";
		return false;
	}

	static bool TryReadPlayerAndRest( ChatCommandContext context, out Player target, out string rest )
	{
		if ( TryFindPlayerAndRest( context.Arguments, out target, out rest, out var error ) )
			return true;

		context.Reply( error, "!" );
		return false;
	}

	static bool TryFindPlayerAndRest( IReadOnlyList<string> arguments, out Player target, out string rest, out string error )
	{
		target = null;
		rest = string.Empty;
		error = "Joueur non trouvé.";

		if ( arguments.Count == 0 )
		{
			error = "Entrez un joueur cible.";
			return false;
		}

		for ( var length = arguments.Count; length >= 1; length-- )
		{
			var targetQuery = string.Join( " ", arguments.Take( length ) );
			if ( TryFindPlayer( targetQuery, out target, out var playerError ) )
			{
				rest = string.Join( " ", arguments.Skip( length ) );
				return true;
			}

			if ( length == 1 )
				error = playerError;
		}

		return false;
	}

	static bool SplitFirst( string text, out string first, out string rest )
	{
		first = null;
		rest = string.Empty;

		text = text?.Trim();
		if ( string.IsNullOrWhiteSpace( text ) )
			return false;

		var separator = text.IndexOf( ' ' );
		if ( separator < 0 )
		{
			first = text;
			return true;
		}

		first = text[..separator].Trim();
		rest = text[(separator + 1)..].Trim();
		return !string.IsNullOrWhiteSpace( first );
	}

	static bool TryParsePositiveInt( string value, out int amount )
	{
		return int.TryParse( value, out amount ) && amount > 0;
	}

	static void AdvertCommand( ChatCommandContext context )
	{
		if ( string.IsNullOrWhiteSpace( context.ArgumentsText ) )
		{
			context.Reply( "Utilisation : /advert <message>", "!" );
			return;
		}

		context.Broadcast( $"[Annonce] {context.Player.DisplayName} : {context.ArgumentsText}", "AD" );
	}

	static void OocCommand( ChatCommandContext context )
	{
		if ( string.IsNullOrWhiteSpace( context.ArgumentsText ) )
		{
			context.Reply( "Utilisation : /ooc <message>", "!" );
			return;
		}

		context.Broadcast( $"[HC] {context.Player.DisplayName} : {context.ArgumentsText}", "OOC" );
	}

	static void MeCommand( ChatCommandContext context )
	{
		if ( string.IsNullOrWhiteSpace( context.ArgumentsText ) )
		{
			context.Reply( "Utilisation : /me <action>", "!" );
			return;
		}

		context.Broadcast( $"* {context.Player.DisplayName} {context.ArgumentsText}", "*" );
	}

	static void PrivateMessageCommand( ChatCommandContext context )
	{
		if ( !TryReadPlayerAndRest( context, out var target, out var message ) )
			return;

		if ( string.IsNullOrWhiteSpace( message ) )
		{
			context.Reply( "Utilisation : /pm <joueur> <message>", "!" );
			return;
		}

		context.Chat?.AddSystemTextTo( context.Connection, $"MP pour {target.DisplayName} : {message}", "PM" );
		if ( target.Network.Owner != context.Connection )
		{
			context.Chat?.AddSystemTextTo( target.Network.Owner, $"MP de {context.Player.DisplayName} : {message}", "PM" );
		}
	}

	static void DropMoneyCommand( ChatCommandContext context )
	{
		if ( context.Arguments.Count < 1 || !TryParsePositiveInt( context.Arguments[0], out var amount ) )
		{
			context.Reply( "Utilisation : /dropmoney <montant>", "!" );
			return;
		}

		context.Player.TryDropMoney( amount );
	}

	static void NameCommand( ChatCommandContext context )
	{
		if ( string.IsNullOrWhiteSpace( context.ArgumentsText ) )
		{
			context.Reply( "Utilisation : /name <nom rp>", "!" );
			return;
		}

		context.Player.TryUpdateRoleplayName( context.ArgumentsText );
	}

	static void KickCommand( ChatCommandContext context )
	{
		if ( !TryReadPlayerAndRest( context, out var target, out var reason ) )
			return;

		var connection = target.Network.Owner;
		if ( connection is null || connection.IsHost || connection == context.Connection )
		{
			context.Reply( "Vous ne pouvez pas expulser ce joueur.", "!" );
			return;
		}

		GameManager.Current?.Kick( connection, string.IsNullOrWhiteSpace( reason ) ? "Expulsé" : reason );
		Notices.SendNotice( context.Connection, "person_remove", Color.Green, $"{target.DisplayName} a été expulsé.", 3 );
	}

	static void BanCommand( ChatCommandContext context )
	{
		if ( context.Arguments.Count == 0 )
		{
			context.Reply( "Utilisation : /ban <joueur|steamid> [raison]", "!" );
			return;
		}

		if ( TryFindPlayerAndRest( context.Arguments, out var target, out var reason, out _ ) )
		{
			var finalReason = string.IsNullOrWhiteSpace( reason ) ? "Banni" : reason;
			var connection = target.Network.Owner;
			if ( connection is null || connection.IsHost || connection == context.Connection )
			{
				context.Reply( "Vous ne pouvez pas bannir ce joueur.", "!" );
				return;
			}

			BanSystem.Current?.Ban( connection, finalReason );
			Notices.SendNotice( context.Connection, "gavel", Color.Green, $"{target.DisplayName} a été banni.", 3 );
			return;
		}

		SplitFirst( context.ArgumentsText, out var targetQuery, out reason );
		var finalOfflineReason = string.IsNullOrWhiteSpace( reason ) ? "Banni" : reason;
		if ( !ulong.TryParse( targetQuery, out var steamIdValue ) )
		{
			context.Reply( "Joueur non trouvé. Utilisez un SteamID pour bannir un joueur hors ligne.", "!" );
			return;
		}

		BanSystem.Current?.Ban( steamIdValue, finalOfflineReason );
		Notices.SendNotice( context.Connection, "gavel", Color.Green, $"{steamIdValue} a été banni.", 3 );
	}

	static void UnbanCommand( ChatCommandContext context )
	{
		if ( context.Arguments.Count < 1 || !ulong.TryParse( context.Arguments[0], out var steamIdValue ) )
		{
			context.Reply( "Utilisation : /unban <steamid>", "!" );
			return;
		}

		BanSystem.Current?.Unban( steamIdValue );
		Notices.SendNotice( context.Connection, "gavel", Color.Green, $"{steamIdValue} a été débanni.", 3 );
	}

	static void SetAdminCommand( ChatCommandContext context )
	{
		if ( !TryReadPlayerAndRest( context, out var target, out var rest ) )
			return;

		if ( !SplitFirst( rest, out var roleText, out _ ) || !TryParseAdminRole( roleText, out var role ) )
		{
			context.Reply( "Utilisation : /setadmin <joueur> <aucun|admin|superadmin>", "!" );
			return;
		}

		var connection = target.Network.Owner;
		if ( connection?.IsHost == true )
		{
			context.Reply( "Vous ne pouvez pas changer le rôle de l'hôte.", "!" );
			return;
		}

		AdminSystem.Current?.SetRole( connection.SteamId, role, connection.DisplayName );
		Notices.SendNotice( context.Connection, "security", Color.Green, $"Le rôle de {target.DisplayName} a été défini sur {role}.", 3 );
	}

	static bool TryParseAdminRole( string roleText, out AdminRole role )
	{
		role = AdminRole.None;
		switch ( roleText?.Trim().ToLowerInvariant() )
		{
			case "none":
			case "user":
			case "remove":
				role = AdminRole.None;
				return true;
			case "admin":
				role = AdminRole.Admin;
				return true;
			case "superadmin":
			case "super":
			case "owner":
				role = AdminRole.SuperAdmin;
				return true;
			default:
				return false;
		}
	}

	static void GiveMoneyCommand( ChatCommandContext context )
	{
		if ( !TryReadPlayerAndRest( context, out var target, out var rest ) )
			return;

		if ( !SplitFirst( rest, out var amountText, out _ ) || !TryParsePositiveInt( amountText, out var amount ) )
		{
			context.Reply( "Utilisation : /givemoney <joueur> <montant>", "!" );
			return;
		}

		target.GiveMoney( amount );
		Notices.SendNotice( context.Connection, "$", Color.Green, $"A donné ${amount:n0} à {target.DisplayName}.", 3 );
	}

	static void SetMoneyCommand( ChatCommandContext context )
	{
		if ( !TryReadPlayerAndRest( context, out var target, out var rest ) )
			return;

		if ( !SplitFirst( rest, out var amountText, out _ ) || !int.TryParse( amountText, out var amount ) || amount < 0 )
		{
			context.Reply( "Utilisation : /setmoney <joueur> <montant>", "!" );
			return;
		}

		target.SetMoney( amount );
		Notices.SendNotice( context.Connection, "$", Color.Green, $"{target.DisplayName} a maintenant ${amount:n0}.", 3 );
	}
}