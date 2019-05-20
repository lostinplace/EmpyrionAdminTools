using System;
using System.Collections.Generic;
using Eleon.Modding;
using EmpyrionAPIDefinitions;


namespace EmpyrionAdminTools
{
  public partial class AdminToolsMod
  {
    public void HandleTeleportCommand(ChatMsgData data, Dictionary<string, string> args)
    {
      this.Request_Player_Info(new Id(data.SenderEntityId), (info) =>
      {
        if (info.permission < (int)PermissionType.GameMaster)
        {
          MessagePlayer(data.SenderEntityId, "You have insufficient permissions to teleport");
          return;
        }

        var requesterName = playerInfoCache[data.SenderEntityId].playerName;

        var targetName = args["targetName"];
        var destinationName = args["destinationName"];

        var targetPlayerInfo = targetName == "me" ?
          info :
          VocallyGetCachedPlayerInfoFromName(targetName, data.SenderEntityId);

        var destinationPlayerInfo = destinationName == "me" ?
          info :
          VocallyGetCachedPlayerInfoFromName(destinationName, data.SenderEntityId);

        Action execute = () =>
        {
          var msg = $@"{requesterName} is teleporting you to the location of {destinationPlayerInfo.playerName}";
          MessagePlayer(targetPlayerInfo.entityId, msg);

          if(targetPlayerInfo.playfield != destinationPlayerInfo.playfield)
          {
            var teleportArg = new IdPlayfieldPositionRotation(
              targetPlayerInfo.entityId,
              destinationPlayerInfo.playfield,
              destinationPlayerInfo.pos,
              destinationPlayerInfo.rot
            );

            this.Request_Player_ChangePlayerfield(teleportArg);
          } else
          {
            var teleportArg = new IdPositionRotation(
              targetPlayerInfo.entityId,
              destinationPlayerInfo.pos,
              destinationPlayerInfo.rot
            );

            this.Request_Entity_Teleport(teleportArg);
          }
        };

        this.Request_Player_Info(new Id(targetPlayerInfo.entityId), (result) =>
        {
          targetPlayerInfo = result;
          this.Request_Player_Info(new Id(destinationPlayerInfo.entityId), result2 =>
          {
            destinationPlayerInfo = result2;
            execute();
          });
        });
      });
    }
  }
}
