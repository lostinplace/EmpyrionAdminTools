using System;
using Eleon.Modding;
using EmpyrionAPITools;
using EmpyrionAPIDefinitions;
using LiteDB;
using System.Linq;
using System.Collections.Generic;

namespace EmpyrionAdminTools
{
  public class MailBox
  {
    public string steamId { get; set; }
    public ItemStack[] contents { get; set; }
  }

  public partial class AdminToolsMod
  {
    private static string mailBoxFileName = @"AdminTools.db";
    private static LiteDatabase db = new LiteDatabase(mailBoxFileName);

    public static void InitializeMailDB() {
      var col = db.GetCollection<MailBox>("mailboxes");
      col.EnsureIndex(x => x.steamId, true);
    }

    public void HandleOtherMailboxCall(ChatInfo info, Dictionary<string, string> args)
    {
      
    }

    public void HandleOpenMailboxCall(ChatInfo info, Dictionary<string, string> args){
      var pi = playerInfoCache[info.playerId];
      HandleMailBoxRequest(pi);
    }

    public void HandleMailBoxRequest(PlayerInfo player){
      var contents = getMailBoxContents(player.steamId);
      var exchange = new ItemExchangeInfo()
      {
        buttonText = "Close Mailbox",
        desc = "",
        id = player.entityId,
        items = contents,
        title = $@"{player.playerName}'s Mailbox"
      };
      this.Request_Player_ItemExchange(exchange, (result) =>
      {
        setMailboxContents(player.steamId, result.items);
      });
    }

    public static ItemStack[] getMailBoxContents(string steamId){

      var col = db.GetCollection<MailBox>("mailboxes");

      var results = col.Find(x => x.steamId == steamId).ToList();
      if (results.Count == 0) return new ItemStack[] {};

      return results.First().contents;
    }

    public static void setMailboxContents(string steamId, ItemStack[] contents){
      var col = db.GetCollection<MailBox>("mailboxes");

      var mailbox = new MailBox()
      {
        steamId = steamId,
        contents = contents
      };

      col.Upsert(mailbox);
    }
  }
}
