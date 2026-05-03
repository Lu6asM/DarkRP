using System.Text.Json;
using Sandbox.UI;

public struct StorageEntry
{
	public string PrefabPath { get; set; }
	public string DisplayName { get; set; }
	public string IconPath { get; set; }
	public int AmmoCount { get; set; }
}

public sealed class PlayerStorage : Component, Local.IPlayerEvents
{
	[Property] public int MaxSlots { get; set; } = 27;
	[Property] public bool PersistsOnDeath { get; set; } = false;

	[RequireComponent] public Player Player { get; set; }

	const string EmptyJson = "[]";
	static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

	[Sync( SyncFlags.FromHost ), Change( nameof( OnStorageJsonChanged ) )]
	public string StorageJson { get; private set; } = EmptyJson;

	List<StorageEntry> CachedEntries;

	public IReadOnlyList<StorageEntry> Entries => GetCachedEntries();

	public bool IsFull => GetCachedEntries().Count >= MaxSlots;

	void OnStorageJsonChanged( string _, string __ ) => CachedEntries = null;

	List<StorageEntry> GetCachedEntries()
	{
		if ( CachedEntries is not null ) return CachedEntries;
		CachedEntries = ParseEntries( StorageJson );
		return CachedEntries;
	}

	static List<StorageEntry> ParseEntries( string json )
	{
		if ( string.IsNullOrWhiteSpace( json ) ) return [];
		try { return JsonSerializer.Deserialize<List<StorageEntry>>( json ) ?? []; }
		catch ( JsonException ) { return []; }
	}

	void SetEntries( List<StorageEntry> entries )
	{
		CachedEntries = entries;
		StorageJson = JsonSerializer.Serialize( entries, JsonOptions );
	}

	/// <summary>
	/// Moves a hotbar weapon into storage. Safe to call from any client — routes to host.
	/// </summary>
	public bool StoreWeapon( BaseCarryable weapon )
	{
		if ( !Networking.IsHost )
		{
			HostStoreWeapon( weapon );
			return true;
		}

		if ( !weapon.IsValid() || weapon.IsJobLocked || weapon is CameraWeapon ) return false;

		var prefabPath = weapon.GameObject.PrefabInstanceSource;
		if ( string.IsNullOrEmpty( prefabPath ) ) return false;

		if ( IsFull )
		{
			Notices.SendNotice( Player.Network.Owner, "backpack", Color.Red, "Sac à dos plein.", 3 );
			return false;
		}

		int ammo = 0;
		if ( weapon is BaseWeapon bw && bw.UsesAmmo )
			ammo = bw.ReserveAmmo;

		var entries = GetCachedEntries().ToList();
		entries.Add( new StorageEntry
		{
			PrefabPath = prefabPath,
			DisplayName = weapon.DisplayName,
			IconPath = weapon.InventoryIconOverride ?? weapon.DisplayIcon?.ResourcePath ?? string.Empty,
			AmmoCount = ammo
		} );

		Player.GetComponent<PlayerInventory>()?.Remove( weapon );
		SetEntries( entries );
		return true;
	}

	[Rpc.Host]
	private void HostStoreWeapon( BaseCarryable weapon ) => StoreWeapon( weapon );

	/// <summary>
	/// Moves a storage entry back into the hotbar. Safe to call from any client — routes to host.
	/// </summary>
	public void RetrieveToHotbar( int index )
	{
		if ( !Networking.IsHost )
		{
			HostRetrieveToHotbar( index );
			return;
		}

		var entries = GetCachedEntries();
		if ( index < 0 || index >= entries.Count ) return;

		var entry = entries[index];
		var inv = Player.GetComponent<PlayerInventory>();
		if ( inv is null ) return;

		if ( inv.FindEmptySlot() < 0 )
		{
			Notices.SendNotice( Player.Network.Owner, "block", Color.Red, "Inventaire plein.", 3 );
			return;
		}

		if ( !inv.Pickup( entry.PrefabPath, true ) ) return;

		var list = GetCachedEntries().ToList();
		list.RemoveAt( index );
		SetEntries( list );
	}

	[Rpc.Host]
	private void HostRetrieveToHotbar( int index ) => RetrieveToHotbar( index );

	/// <summary>
	/// Drops a storage entry into the world in front of the player. Safe to call from any client.
	/// </summary>
	public void DropFromStorage( int index )
	{
		if ( !Networking.IsHost )
		{
			HostDropFromStorage( index );
			return;
		}

		var entries = GetCachedEntries();
		if ( index < 0 || index >= entries.Count ) return;

		var entry = entries[index];
		var prefab = GameObject.GetPrefab( entry.PrefabPath );
		if ( prefab.IsValid() )
		{
			var dropPos = Player.EyeTransform.Position + Player.EyeTransform.Forward * 48f;
			var pickup = prefab.Clone( new CloneConfig
			{
				Transform = new Transform( dropPos ),
				StartEnabled = true
			} );

			Ownable.Set( pickup, Player.Network.Owner );
			pickup.Tags.Add( "removable" );
			pickup.NetworkSpawn();

			if ( pickup.GetComponent<Rigidbody>() is { } rb )
			{
				rb.Velocity = Player.Controller.Velocity + Player.EyeTransform.Forward * 200f + Vector3.Up * 100f;
				rb.AngularVelocity = Vector3.Random * 8.0f;
			}
		}

		var list = GetCachedEntries().ToList();
		list.RemoveAt( index );
		SetEntries( list );
	}

	[Rpc.Host]
	private void HostDropFromStorage( int index ) => DropFromStorage( index );

	/// <summary>
	/// Empties the storage. Host-only.
	/// </summary>
	public void Clear()
	{
		if ( !Networking.IsHost ) return;
		CachedEntries = null;
		StorageJson = EmptyJson;
	}

	void Local.IPlayerEvents.OnDied( PlayerDiedParams args )
	{
		if ( !Networking.IsHost || PersistsOnDeath ) return;
		Clear();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( Input.Pressed( "Backpack" ) )
			Scene.Get<InventoryMenu>()?.ToggleOpen();
	}
}
