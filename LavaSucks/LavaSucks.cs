using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace LavaSucks
{
	[ApiVersion(2, 1)]
	public class LavaSucks : TerrariaPlugin
	{
		public static bool doesLavaSuck = true;

		#region Info
		public override string Name => "LavaSucks";
		
		public override string Author => "aMoka";
		
		public override string Description => "Autoremoves the blood of hellstone children during their murders.";
		
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
		#endregion
		
		#region Initialize
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.NetGetData.Register(this, OnGetData);
		}
        #endregion
		
		#region Dispose
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
			}
			base.Dispose(disposing);
		}
		#endregion
		
		public LavaSucks(Main game)
			: base(game)
		{
			Order = 10;
		}

		void OnInitialize(EventArgs args)
		{
			//Adding a command is as simple as adding a new ``Command`` object to the ``ChatCommands`` list.
			//The ``Commands` object is available after including TShock in the file (`using TShockAPI;`)
			Commands.ChatCommands.Add(new Command("lavasucks.admin.toggle", ToggleLava, "tlava")
				{
					HelpText = "Turns on/off lava drop from hellstone mining. Default setting is off."
				});
		}
		
		#region OnGetData
		/// <summary>
		/// Called when the server receives any sort of packet.
		/// </summary>
		void OnGetData(GetDataEventArgs args)
		{
			// If we don't want to remove lava, then this method is useless and we can return now.
			if (!doesLavaSuck)
				return;
			
			// If the packet hasn't been handled (i.e. canceled out) by another plugin, and it's a tile edit packet.
			if (!args.Handled && args.MsgID == PacketTypes.Tile)
			{
				// Then we want to read the data it exposes.
				using (BinaryReader reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
				{
					byte action = reader.ReadByte();
					short x = reader.ReadInt16();
					short y = reader.ReadInt16();
					ushort type = reader.ReadUInt16();
					
					//if the tile destroyed was not hellstone, we can ignore the packet and return
					if (Main.tile[x, y].type != Terraria.ID.TileID.Hellstone) 
						return;
						
					if (action == 0)  //0 = destroy
					{
						//remove the tile from play instead of sending to the graveyard...
						Main.tile[x, y].active(false);
						Main.tile[x, y].frameX = -1;
						Main.tile[x, y].frameY = -1;
						Main.tile[x, y].liquidType(0);
						Main.tile[x, y].liquid = 0;
						Main.tile[x, y].type = 0;
						//Tell clients that the hellstone tile is dead
						TSPlayer.All.SendTileSquare(x, y);
						TShock.Players[args.Msg.whoAmI].SendTileSquare(x, y);
						
						//and special summon hellstone to the field
						Item itm = new Item();
						//Create a new item with the properties of hellstone
						itm.SetDefaults(Terraria.ID.ItemID.Hellstone);
						//Spawn it on the server
						int itemid = Item.NewItem(x * 16, y * 16, itm.width, itm.height, itm.type, 1, true, 0, true);
						//Then send a packet to let clients know it exists
						NetMessage.SendData((int)PacketTypes.ItemDrop, -1, -1, NetworkText.Empty, itemid, 0f, 0f, 0f);
					}
				}
			}
		}
		#endregion

		void ToggleLava(CommandArgs args)
		{
			//invert the boolean value
			doesLavaSuck = !doesLavaSuck;

            args.Player.SendSuccessMessage($"Lava from hellstone mining is now {((doesLavaSuck) ? "OFF" : "ON")}.");
		}
	}
}
