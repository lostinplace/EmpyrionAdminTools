using System;
using Eleon.Modding;
using EmpyrionAPITools;
using EmpyrionAPIDefinitions;
using LiteDB;
using System.Linq;
using System.Collections.Generic;

namespace EmpyrionAdminTools
{
  public class ItemStackWrapper
  {

    private int id;

    private int count;

    private byte slotIdx;

    private int ammo;

    private int decay;

    public int Id { get => id; set => id = value; }
    public int Count { get => count; set => count = value; }
    public byte SlotIdx { get => slotIdx; set => slotIdx = value; }
    public int Ammo { get => ammo; set => ammo = value; }
    public int Decay { get => decay; set => decay = value; }

    public ItemStackWrapper() { }

    public static ItemStack ToItemStack(ItemStackWrapper wrapper)
    {
      return new ItemStack()
      {
        id = wrapper.Id,
        ammo = wrapper.Ammo,
        count = wrapper.Count,
        slotIdx = wrapper.SlotIdx,
        decay = wrapper.decay
      };
    }

    public static ItemStackWrapper ToWrapper(ItemStack stack)
    {
      return new ItemStackWrapper()
      {
        Id = stack.id,
        Ammo = stack.ammo,
        Count = stack.count,
        SlotIdx = stack.slotIdx,
        Decay = stack.decay
      };
    }    
  }

  public class MailBox
  {
    [BsonId]
    public string steamId { get; set; }
    public List<ItemStackWrapper> contents { get; set; }
  }

  public partial class AdminToolsMod
  {
    private static string DatabasFileName = @"AdminTools.db";
    private static LiteDatabase db = new LiteDatabase(DatabasFileName);

    public static void InitializeMailDB()
    {
      var col = db.GetCollection<MailBox>("mailboxes");
      col.EnsureIndex(x => x.steamId, true);
    }

    public void HandleOtherMailboxCall(ChatMsgData info, Dictionary<string, string> args)
    {
      var other = VocallyGetCachedPlayerInfoFromName(args["playerName"], info.SenderEntityId);
      HandleMailBoxRequest(other, info.SenderEntityId);
    }

    public void HandleOpenMailboxCall(ChatMsgData info, Dictionary<string, string> args)
    {
      var pi = playerInfoCache[info.SenderEntityId];
      HandleMailBoxRequest(pi, info.SenderEntityId);
    }

    public void HandleMailBoxRequest(PlayerInfo player, int viewer)
    {
      var contents = getMailBoxContents(player.steamId);
      var exchange = new ItemExchangeInfo()
      {
        buttonText = "Close Mailbox",
        desc = "",
        id = viewer,
        items = contents,
        title = $@"{player.playerName}'s Mailbox"
      };
      this.Request_Player_ItemExchange(exchange, (result) =>
      {
        setMailboxContents(player.steamId, result.items);
      });
    }

    public ItemStack[] getMailBoxContents(string steamId)
    {
      this.log($"***********getting for {steamId}");
      var col = db.GetCollection<MailBox>("mailboxes");

      var results = col.Find(x => x.steamId == steamId).ToList();
      this.log($"***********results has {results.Count}");
      if (results.Count == 0) return new ItemStack[] { };
      this.log($"***********first result has {results.First().contents.Count}");
      return results.First().contents.Select(x=>ItemStackWrapper.ToItemStack(x)).ToArray();
    }

    public void setMailboxContents(string steamId, ItemStack[] contents)
    {
      var col = db.GetCollection<MailBox>("mailboxes");
      this.log($"***********saving for {steamId}");
      var mailbox = new MailBox()
      {
        steamId = steamId,
        contents = contents.Select(x=>ItemStackWrapper.ToWrapper(x)).ToList()
      };

      this.log($"***********saving {contents.Count()}");

      col.Upsert(mailbox);
      
    }
  }
}
