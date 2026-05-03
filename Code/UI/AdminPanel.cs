using Sandbox;
using Sandbox.UI;
using System;
using System.Linq;

public class AdminPanel : Component
{
    protected override void OnStart()
    {
        // Correction : On utilise Game.RootPanel directement s'il existe, ou on crée un nouveau RootPanel
        var mainFrame = new RootPanel(); 
        
        mainFrame.Style.FlexDirection = FlexDirection.Column;
        mainFrame.Style.BackgroundColor = new Color( 0.15f, 0.15f, 0.15f, 0.95f );
        mainFrame.Style.BorderColor = Color.Cyan;
        mainFrame.Style.BorderWidth = 2;
        mainFrame.Style.Padding = 15;
        mainFrame.Style.Width = 350;
        mainFrame.Style.Position = PositionMode.Absolute;
        mainFrame.Style.Left = Length.Percent( 40 );
        mainFrame.Style.Top = Length.Percent( 20 );

        var header = new Label();
        header.Parent = mainFrame;
        header.Text = "DARKRP ADMIN PANEL";
        header.Style.FontColor = Color.Cyan;
        header.Style.FontSize = 20;
        header.Style.FontWeight = 800;
        header.Style.MarginBottom = 15;

        foreach ( var connection in Connection.All )
        {
            var row = new Panel();
            row.Parent = mainFrame;
            row.Style.FlexDirection = FlexDirection.Row;
            row.Style.MarginBottom = 8;
            row.Style.BackgroundColor = Color.White.WithAlpha( 0.05f );
            row.Style.Padding = 5;

            var name = new Label();
            name.Parent = row;
            name.Text = connection.DisplayName;
            name.Style.FlexGrow = 1;

            var btnHeal = new Label();
            btnHeal.Parent = row;
            btnHeal.Text = " HEAL ";
            btnHeal.Style.BackgroundColor = Color.Green.WithAlpha( 0.6f );
            // Correction BorderRadius -> BorderRadiusTopLeft (et autres) ou BorderCornerRadius
            btnHeal.Style.BorderTopLeftRadius = 3;
            btnHeal.Style.BorderTopRightRadius = 3;
            btnHeal.Style.BorderBottomLeftRadius = 3;
            btnHeal.Style.BorderBottomRightRadius = 3;
            btnHeal.AddEventListener( "onmousedown", () => SetHealth( connection.Id, 100 ) );

            var btnKick = new Label();
            btnKick.Parent = row;
            btnKick.Text = " KICK ";
            btnKick.Style.BackgroundColor = Color.Red.WithAlpha( 0.6f );
            btnKick.Style.BorderTopLeftRadius = 3;
            btnKick.Style.BorderTopRightRadius = 3;
            btnKick.Style.BorderBottomLeftRadius = 3;
            btnKick.Style.BorderBottomRightRadius = 3;
            btnKick.Style.MarginLeft = 5;
            btnKick.AddEventListener( "onmousedown", () => KickPlayer( connection.Id ) );
        }
    }

    [ConCmd( "admin_set_health" )]
    public static void SetHealth( Guid id, int val ) => Log.Info( $"Heal: {id}" );

    [ConCmd( "admin_kick" )]
    public static void KickPlayer( Guid id ) => Log.Info( $"Kick: {id}" );
}