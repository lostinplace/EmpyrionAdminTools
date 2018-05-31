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

      this.verbose = true;

      this.Event_Player_Connected += PlayerConnnectedHandler;
      this.Event_Player_Disconnected += RemovePlayerInfoFromCache;
      this.Event_Player_Info += CachePlayerInfo;


      var ownMailboxCommand = new ChatCommand(
        @"/at mailbox", HandleOpenMailboxCall, "open your mailbox"
      );

      var otherPlayerMailboxCommand = new ChatCommand(
        @"/at mailbox (?<playerName>.+)", HandleOtherMailboxCall, "open the mailbox for {playerName}", PermissionType.Moderator
      );

      var teleportCommand = new ChatCommand(
        @"/at teleport (?<targetName>.+) to (?<destinationName>.+)",
        HandleTeleportCommand,
        @"teleports player with {targetName} to the location of player with {destinationName}." +
          @"Use ""me"" to indicate yourself.",
        PermissionType.Moderator
      );
      
      this.ChatCommands.Add(teleportCommand);
      this.ChatCommands.Add(ownMailboxCommand);
      this.ChatCommands.Add(otherPlayerMailboxCommand);

      this.ChatCommands.Add(new ChatCommand(@"/at help", (data, __) => {
        this.Request_Player_Info(data.playerId.ToId(), (info) =>
        {
          var playerPermissionLevel = (PermissionType)info.permission;
          var header = $"Commands available to {info.playerName}; permission level {playerPermissionLevel}\n";

          var lines = this.GetChatCommandsForPermissionLevel(playerPermissionLevel)
            .Select(x => x.ToString())
            .OrderBy(x => x.Length).ToList();

          lines.Insert(0, header);

          var msg = new DialogBoxData()
          {
            Id = data.playerId,
            MsgText = String.Join("\n", lines.ToArray())
          };
          Request_ShowDialog_SinglePlayer(msg);
        });
      }));
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
        var message = $@"there are no currently active players found with the name containing, ""{name}""";
        MessagePlayer(requesterId, message);
        return null;
      }
      else if (candidates.Count > 1)
      {
        var caseSensitive = candidates.Where(x => x.playerName.Contains(name)).ToList();
        if (caseSensitive.Count == 1) return caseSensitive.First();
        var exactMatch = candidates.FirstOrDefault(x => x.playerName == name);
        if (exactMatch != null) return exactMatch;

        var message = $@"{caseSensitive.Count} active players found with a name containing, ""{name}"". Please be more specific";
        MessagePlayer(requesterId, message);
        return null;  
      }
      return candidates.First();
    }
  }
}
