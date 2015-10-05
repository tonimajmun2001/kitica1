﻿using FFXIVClassic_Lobby_Server;
using FFXIVClassic_Lobby_Server.dataobjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVClassic_Map_Server.dataobjects
{
    class Player
    {        
        Actor playerActor;

        ClientConnection conn1;
        ClientConnection conn2;

        public uint characterID = 0;
        public uint actorID = 0;
        
        uint currentZoneID = 0;

        List<Actor> actorInstanceList = new List<Actor>();

        bool isDisconnected;

        public Player(uint actorId)
        {
            this.actorID = actorId;
            createPlayerActor(actorId, null);
        }

        public void addConnection(ClientConnection conn)
        {
            if (conn1 == null && conn2 != null)
                conn1 = conn;
            else if (conn2 == null && conn1 != null)
                conn2 = conn;
            else
                conn1 = conn;
        }

        public bool isClientConnectionsReady()
        {
            return (conn1 != null && conn2 != null);
        }

        public void disconnect()
        {
            isDisconnected = true;
            conn1.disconnect();
            conn2.disconnect();
        }

        public void setConnection1(ClientConnection conn)
        {
            conn1 = conn;
        }

        public void setConnection2(ClientConnection conn)
        {
            conn2 = conn;
        }

        public ClientConnection getConnection1()
        {
            return conn1;
        }

        public ClientConnection getConnection2()
        {
            return conn1;
        }

        public void createPlayerActor(uint actorId, Character chara)
        {
            playerActor = new Actor(actorId);
            actorInstanceList.Add(playerActor);
        }

        public void updatePlayerActorPosition(float x, float y, float z, float rot, ushort moveState)
        {
            playerActor.positionX = x;
            playerActor.positionY = y;
            playerActor.positionZ = z;
            playerActor.rotation = rot;
            playerActor.moveState = moveState;
        }            

        public void sendMotd()
        {
            World world = Database.getServer(ConfigConstants.DATABASE_WORLDID);
            //sendChat(world.motd);
        }

        public void sendChat(Player sender, string message, int mode)
        {

        }

    }
}