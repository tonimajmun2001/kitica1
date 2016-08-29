﻿using FFXIVClassic_Map_Server;
using FFXIVClassic.Common;

using FFXIVClassic_Map_Server.Actors;
using FFXIVClassic_Map_Server.lua;
using FFXIVClassic_Map_Server.packets.send.actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVClassic_Map_Server.dataobjects
{
    class ConnectedPlayer
    {
        public uint actorID = 0;
        Player playerActor;
        public List<Actor> actorInstanceList = new List<Actor>();

        public uint languageCode = 1;

        private ZoneConnection zoneConnection;

        private uint lastPingPacket = Utils.UnixTimeStampUTC();

        public string errorMessage = "";

        public ConnectedPlayer(ZoneConnection zc, uint actorId)
        {
            zoneConnection = zc;
            this.actorID = actorId;
            playerActor = new Player(this, actorId);
            actorInstanceList.Add(playerActor);
        }

        public void QueuePacket(BasePacket basePacket)
        {
            zoneConnection.QueuePacket(basePacket);
        }

        public void QueuePacket(SubPacket subPacket, bool isAuthed, bool isEncrypted)
        {
            zoneConnection.QueuePacket(subPacket, isAuthed, isEncrypted);
        }

        public Player GetActor()
        {
            return playerActor;
        }

        public void Ping()
        {
            lastPingPacket = Utils.UnixTimeStampUTC();
        }

        public bool CheckIfDCing()
        {
            uint currentTime = Utils.UnixTimeStampUTC();
            if (currentTime - lastPingPacket >= 5000) //Show D/C flag
                playerActor.SetDCFlag(true);
            else if (currentTime - lastPingPacket >= 30000) //DCed
                return true;
            else
                playerActor.SetDCFlag(false);
            return false;
        }

        public void UpdatePlayerActorPosition(float x, float y, float z, float rot, ushort moveState)
        {
            playerActor.oldPositionX = playerActor.positionX;
            playerActor.oldPositionY = playerActor.positionY;
            playerActor.oldPositionZ = playerActor.positionZ;
            playerActor.oldRotation = playerActor.rotation;

            playerActor.positionX = x;
            playerActor.positionY = y;
            playerActor.positionZ = z;
            playerActor.rotation = rot;
            playerActor.moveState = moveState;

            GetActor().zone.UpdateActorPosition(GetActor());

        }

        public void UpdateInstance(List<Actor> list)
        {
            List<BasePacket> basePackets = new List<BasePacket>();
            List<SubPacket> RemoveActorSubpackets = new List<SubPacket>();
            List<SubPacket> posUpdateSubpackets = new List<SubPacket>();

            //Remove missing actors
            for (int i = 0; i < actorInstanceList.Count; i++)
            {
                if (!list.Contains(actorInstanceList[i]))
                {
                    GetActor().QueuePacket(RemoveActorPacket.BuildPacket(playerActor.actorId, actorInstanceList[i].actorId));
                    actorInstanceList.RemoveAt(i);
                }
            }

            //Add new actors or move
            for (int i = 0; i < list.Count; i++)
            {
                Actor actor = list[i];

                if (actor.actorId == playerActor.actorId)
                    continue;

                if (actorInstanceList.Contains(actor))
                {
                    GetActor().QueuePacket(actor.CreatePositionUpdatePacket(playerActor.actorId));
                }
                else
                {
                    GetActor().QueuePacket(actor.GetSpawnPackets(playerActor.actorId, 1));
                    GetActor().QueuePacket(actor.GetInitPackets(playerActor.actorId));
                    GetActor().QueuePacket(actor.GetSetEventStatusPackets(playerActor.actorId));
                    actorInstanceList.Add(actor);

                    if (actor is Npc)
                    {
                        ((Npc)actor).DoOnActorSpawn(playerActor);
                    }
                }
            }

        }


        public void ClearInstance()
        {
            actorInstanceList.Clear();
        }

    }
}
