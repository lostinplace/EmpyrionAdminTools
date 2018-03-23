using System;
using Eleon.Modding;
using EmpyrionAPITools;
using System.Collections.Generic;
using EmpyrionAPIDefinitions;
using System.Linq;


namespace EmpyrionAdminTools
{
  public partial class AdminToolsMod : SimpleMod
  {
    private Dictionary<int, PlayerInfo> playerInfoCache = new Dictionary<int, PlayerInfo>();

    public override void Initialize(ModGameAPI dediAPI)
    {
      this.Event_Player_Connected += PlayerConnnectedHandler;
      this.Event_Player_Disconnected += RemovePlayerInfoFromCache;
      this.Event_Player_Info += CachePlayerInfo;


      var ownMailboxCommand = new ChatCommand(
        @"/mailbox", HandleOpenMailboxCall, "open your mailbox"
      );

      var otherPlayerMailboxCommand = new ChatCommand(
        @"/mailbox (?<playerName>\S+)", HandleOtherMailboxCall, "open the mailbox for {playerName}"
      );

      var teleportCommand = new ChatCommand(
        @"/teleport (?<targetName>\S+) to (?<destinationName>\S+)",
        HandleTeleportCommand,
        @"teleports player with {targetName} to the location of player with {destinationName}." +
          @"Use ""me"" to indicate yourself."
      );
      
      this.ChatCommands.Add(teleportCommand);
      this.ChatCommands.Add(ownMailboxCommand);
    }

    public void PlayerConnnectedHandler(Id playerId){
      this.Request_Player_Info(playerId);
    }

    public void RemovePlayerInfoFromCache(Id playerId){
      playerInfoCache.Remove(playerId.id);
    }

    public void CachePlayerInfo(PlayerInfo info){
      playerInfoCache[info.entityId] = info;
    }

    public void MessagePlayer(int id, string message){
      var outMsg = new IdMsgPrio()
      {
        id = id,
        msg = message
      };
      this.Request_InGameMessage_SinglePlayer(outMsg);
    }

    public PlayerInfo VocallyGetCachedPlayerInfoFromName(string name, int requesterId){
      var candidates = playerInfoCache.Values.Where(
        x => x.playerName.ToLower().Contains(name.ToLower())).ToList();

      if (candidates.Count == 0)
      {
        var message = $@"there are no currently active players found with the name" +
          $@"containing, ""{name}""";
        MessagePlayer(requesterId, message);
        return null;
      }
      else if (candidates.Count > 1)
      {
        var caseSensitive = candidates.Where(x => x.playerName.Contains(name)).ToList();
        if (caseSensitive.Count == 1) return caseSensitive.First();
        var exactMatch = candidates.FirstOrDefault(x => x.playerName == name);
        if (exactMatch != null) return exactMatch;

        var message = $@"{caseSensitive.Count} active players found with a name containing," +
        $@"""{name}"". Please be more specific";
        MessagePlayer(requesterId, message);
        return null;  
      }
      return candidates.First();
    }

  }
}
