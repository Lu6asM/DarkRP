using Sandbox.UI;

public sealed partial class Player
{
	[Property, Sync( SyncFlags.FromHost )]
	public AdminRole AdminRole { get; private set; } = AdminRole.None;

	public bool HasAdminAccess => (Network.Owner?.IsHost ?? false) || AdminRole >= AdminRole.Admin;
	public bool HasSuperAdminAccess => (Network.Owner?.IsHost ?? false) || AdminRole >= AdminRole.SuperAdmin;

	public void SetAdminRole( AdminRole role )
	{
		if ( !Networking.IsHost )
			return;

		AdminRole = role;
	}

	[Rpc.Host]
	public void RequestKickPlayer( long steamId, string reason )
	{
		if ( !AdminSystem.Current.HasAdminAccess( Rpc.Caller ) || steamId <= 0 )
			return;

		var connection = Connection.All.FirstOrDefault( x => x.SteamId.Value == steamId );
		if ( connection is null || connection.IsHost || connection == Rpc.Caller )
			return;

		var finalReason = string.IsNullOrWhiteSpace( reason ) ? "Expulsé(e)" : reason.Trim();
		GameManager.Current?.Kick( connection, finalReason );
		Notices.SendNotice( Rpc.Caller, "person_remove", Color.Green, $"{connection.DisplayName} a été expulsé(e).", 3 );
	}

	[Rpc.Host]
	public void RequestBanPlayer( long steamId, string reason )
	{
		if ( !AdminSystem.Current.HasSuperAdminAccess( Rpc.Caller ) || steamId <= 0 )
			return;

		var connection = Connection.All.FirstOrDefault( x => x.SteamId.Value == steamId );
		if ( connection is null || connection.IsHost || connection == Rpc.Caller )
			return;

		var finalReason = string.IsNullOrWhiteSpace( reason ) ? "Banni(e)" : reason.Trim();
		BanSystem.Current?.Ban( connection, finalReason );
		Notices.SendNotice( Rpc.Caller, "gavel", Color.Green, $"{connection.DisplayName} a été banni(e).", 3 );
	}

	[Rpc.Host]
	public void RequestSetAdminRole( long steamId, AdminRole role )
	{
		if ( !AdminSystem.Current.HasSuperAdminAccess( Rpc.Caller ) || steamId <= 0 )
			return;

		var targetSteamId = (SteamId)steamId;
		var connection = Connection.All.FirstOrDefault( x => x.SteamId == targetSteamId );
		if ( connection?.IsHost == true )
			return;

		var displayName = connection?.DisplayName ?? targetSteamId.ToString();
		AdminSystem.Current.SetRole( targetSteamId, role, displayName );

		var roleText = role switch
		{
			AdminRole.Admin      => "défini(e) comme admin",
			AdminRole.SuperAdmin => "défini(e) comme superadmin",
			_                    => "retiré(e) du staff"
		};

		Notices.SendNotice( Rpc.Caller, role == AdminRole.SuperAdmin ? "stars" : "security", Color.Green, $"{displayName} {roleText}.", 3 );
	}

	// ── Set Health ───────────────────────────────────────────────────────

	[Rpc.Host]
	public void RequestAdminSetHealth( Guid targetId, int hp )
	{
		if ( !AdminSystem.Current.HasAdminAccess( Rpc.Caller ) ) return;

		var target = Player.For( targetId );
		if ( target is null ) return;

		target.Health = Math.Clamp( hp, 1f, 100f );
		Notices.SendNotice( Rpc.Caller, "favorite", Color.Green, $"Santé de {target.DisplayName} → {hp}.", 3 );
	}

	// ── Set Armour ───────────────────────────────────────────────────────

	[Rpc.Host]
	public void RequestAdminSetArmour( Guid targetId, int armour )
	{
		if ( !AdminSystem.Current.HasAdminAccess( Rpc.Caller ) ) return;

		var target = Player.For( targetId );
		if ( target is null ) return;

		target.Armour = Math.Clamp( armour, 0f, 100f );
		Notices.SendNotice( Rpc.Caller, "shield", Color.Green, $"Armure de {target.DisplayName} → {armour}.", 3 );
	}

	// ── Give / Take Money ─────────────────────────────────────────────────

	[Rpc.Host]
	public void RequestAdminGiveMoney( Guid targetId, int amount )
	{
		if ( !AdminSystem.Current.HasAdminAccess( Rpc.Caller ) ) return;

		var target = Player.For( targetId );
		if ( target is null ) return;

		target.Money = Math.Max( 0, target.Money + amount );
		var label = amount >= 0 ? $"+${amount:n0}" : $"-${Math.Abs( amount ):n0}";
		Notices.SendNotice( Rpc.Caller, "attach_money", Color.Green, $"{label} pour {target.DisplayName}.", 3 );
	}

	// ── TP → moi ─────────────────────────────────────────────────────────

	[Rpc.Host]
	public void RequestAdminTeleportToMe( Guid targetId )
	{
		if ( !AdminSystem.Current.HasAdminAccess( Rpc.Caller ) ) return;

		var target = Player.For( targetId );
		if ( target is null ) return;

		var caller = Scene.GetAll<Player>().FirstOrDefault( p => p.Network.OwnerId == Rpc.CallerId );
		if ( caller is null ) return;

		target.WorldPosition = caller.WorldPosition + Vector3.Up * 10f;
		Notices.SendNotice( Rpc.Caller, "near_me", Color.Green, $"{target.DisplayName} téléporté vers vous.", 3 );
	}

	// ── TP → lui ─────────────────────────────────────────────────────────

	[Rpc.Host]
	public void RequestAdminTeleportToTarget( Guid targetId )
	{
		if ( !AdminSystem.Current.HasAdminAccess( Rpc.Caller ) ) return;

		var target = Player.For( targetId );
		if ( target is null ) return;

		var caller = Scene.GetAll<Player>().FirstOrDefault( p => p.Network.OwnerId == Rpc.CallerId );
		if ( caller is null ) return;

		caller.WorldPosition = target.WorldPosition + Vector3.Up * 10f;
		Notices.SendNotice( Rpc.Caller, "location_on", Color.Green, $"Téléporté vers {target.DisplayName}.", 3 );
	}

	// ── Slay ─────────────────────────────────────────────────────────────

[Rpc.Host]
public void RequestAdminSlay( Guid targetId )
{
    if ( !AdminSystem.Current.HasAdminAccess( Rpc.Caller ) ) return;

    var target = Player.For( targetId );
    if ( target is null ) return;

    // On appelle Kill() directement via la méthode interne
    // sans passer par OnDamage qui tente de sérialiser DamageInfo
    target.Health = 0;
    var dmg = new DamageInfo();
    target.Kill( dmg );
    Notices.SendNotice( Rpc.Caller, "bolt", Color.Green, $"{target.DisplayName} a été slayé(e).", 3 );
}

	// ── God Mode ─────────────────────────────────────────────────────────

	[Rpc.Host]
	public void RequestAdminToggleGodMode( Guid targetId )
	{
		if ( !AdminSystem.Current.HasAdminAccess( Rpc.Caller ) ) return;

		var target = Player.For( targetId );
		if ( target is null || !target.PlayerData.IsValid() ) return;

		target.PlayerData.IsGodMode = !target.PlayerData.IsGodMode;
		var state = target.PlayerData.IsGodMode ? "activé" : "désactivé";
		Notices.SendNotice( Rpc.Caller, "auto_fix", Color.Green, $"God Mode {state} pour {target.DisplayName}.", 3 );
	}

	// ── Invisible ────────────────────────────────────────────────────────
	// Cache le Body du joueur pour tous les autres clients.
	// Le joueur lui-même voit toujours son propre body (géré dans ApplyInvisibility).

	[Rpc.Host]
	public void RequestAdminToggleInvisible( Guid targetId )
	{
		if ( !AdminSystem.Current.HasAdminAccess( Rpc.Caller ) ) return;

		var target = Player.For( targetId );
		if ( target is null || !target.PlayerData.IsValid() ) return;

		target.PlayerData.IsInvisible = !target.PlayerData.IsInvisible;
		target.ApplyInvisibility( target.PlayerData.IsInvisible );

		var state = target.PlayerData.IsInvisible ? "activé" : "désactivé";
		Notices.SendNotice( Rpc.Caller, "visibility_off", Color.Green, $"Invisible {state} pour {target.DisplayName}.", 3 );
	}

	[Rpc.Broadcast( NetFlags.HostOnly | NetFlags.Reliable )]
	public void ApplyInvisibility( bool invisible )
	{
		if ( !Body.IsValid() ) return;

		// Le joueur lui-même voit toujours son propre body
		if ( IsLocalPlayer )
		{
			Body.Enabled = true;
			return;
		}

		Body.Enabled = !invisible;
	}
}
