﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using FFXIVClassic_Lobby_Server.common;
using FFXIVClassic_Map_Server.dataobjects;
using FFXIVClassic_Lobby_Server.packets;
using System.IO;
using FFXIVClassic_Map_Server.packets.send.actor;
using FFXIVClassic_Map_Server;
using FFXIVClassic_Map_Server.packets.send;
using FFXIVClassic_Map_Server.dataobjects.chara;
using FFXIVClassic_Map_Server.Actors;
using FFXIVClassic_Map_Server.lua;
using FFXIVClassic_Map_Server.actors.chara.player;
using FFXIVClassic_Map_Server.Properties;

namespace FFXIVClassic_Lobby_Server
{
    class CommandProcessor
    {
        private Dictionary<uint, ConnectedPlayer> mConnectedPlayerList;
        private static WorldManager mWorldManager = Server.getWorldManager();
        private static Dictionary<uint, Item> gamedataItems = Server.getItemGamedataList();

        // For the moment, this is the only predefined item
        // TODO: make a list/enum in the future so that items can be given by name, instead of by id
        const UInt32 ITEM_GIL = 1000001;

        public CommandProcessor(Dictionary<uint, ConnectedPlayer> playerList)
        {
            mConnectedPlayerList = playerList;
        }

        public void sendPacket(ConnectedPlayer client, string path)
        {
            BasePacket packet = new BasePacket(path);

            if (client != null)
            {
                packet.replaceActorID(client.actorID);
                client.queuePacket(packet);
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    packet.replaceActorID(entry.Value.actorID);
                    entry.Value.queuePacket(packet);
                }
            }
        }

        public void changeProperty(uint id, uint value, string target)
        {
            SetActorPropetyPacket changeProperty = new SetActorPropetyPacket(target);

            changeProperty.setTarget(target);
            changeProperty.addInt(id, value);
            changeProperty.addTarget();

            foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
            {
                SubPacket changePropertyPacket = changeProperty.buildPacket((entry.Value.actorID), (entry.Value.actorID));

                BasePacket packet = BasePacket.createPacket(changePropertyPacket, true, false);
                packet.debugPrintPacket();

                entry.Value.queuePacket(packet);
            }
        }

        public void doMusic(ConnectedPlayer client, string music)
        {
            ushort musicId;

            if (music.ToLower().StartsWith("0x"))
                musicId = Convert.ToUInt16(music, 16);
            else
                musicId = Convert.ToUInt16(music);

            if (client != null)
                client.queuePacket(BasePacket.createPacket(SetMusicPacket.buildPacket(client.actorID, musicId, 1), true, false));
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    BasePacket musicPacket = BasePacket.createPacket(SetMusicPacket.buildPacket(entry.Value.actorID, musicId, 1), true, false);
                    entry.Value.queuePacket(musicPacket);
                }
            }
        }

        public void doWarp(ConnectedPlayer client, string entranceId)
        {
            uint id;

            try
            {
                if (entranceId.ToLower().StartsWith("0x"))
                    id = Convert.ToUInt32(entranceId, 16);
                else
                    id = Convert.ToUInt32(entranceId);
            }
            catch(FormatException e)
            {return;}

            FFXIVClassic_Map_Server.WorldManager.ZoneEntrance ze = mWorldManager.getZoneEntrance(id);

            if (ze == null)
                return;

            if (client != null)
                mWorldManager.DoZoneChange(client.getActor(), ze.zoneId, ze.privateAreaName, ze.spawnType, ze.spawnX, ze.spawnY, ze.spawnZ, 0.0f);
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    mWorldManager.DoZoneChange(entry.Value.getActor(), ze.zoneId, ze.privateAreaName, ze.spawnType, ze.spawnX, ze.spawnY, ze.spawnZ, 0.0f);
                }
            }
        }

        public void doWarp(ConnectedPlayer client, string zone, string privateArea, string sx, string sy, string sz)
        {
            uint zoneId;
            float x,y,z;

            if (zone.ToLower().StartsWith("0x"))
                zoneId = Convert.ToUInt32(zone, 16);
            else
                zoneId = Convert.ToUInt32(zone);

            if (mWorldManager.GetZone(zoneId) == null)
            {
                if (client != null)
                    client.queuePacket(BasePacket.createPacket(SendMessagePacket.buildPacket(client.actorID, client.actorID, SendMessagePacket.MESSAGE_TYPE_GENERAL_INFO, "", "Zone does not exist or setting isn't valid."), true, false));
                Log.error("Zone does not exist or setting isn't valid.");
            }

            x = Single.Parse(sx);
            y = Single.Parse(sy);
            z = Single.Parse(sz);

            if (client != null)
                mWorldManager.DoZoneChange(client.getActor(), zoneId, privateArea, 0x2, x, y, z, 0.0f);
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    mWorldManager.DoZoneChange(entry.Value.getActor(), zoneId, privateArea, 0x2, x, y, z, 0.0f);
                }
            }
        }

        public void printPos(ConnectedPlayer client)
        {
            if (client != null)
            {
                Player p = client.getActor();
                client.queuePacket(BasePacket.createPacket(SendMessagePacket.buildPacket(client.actorID, client.actorID, SendMessagePacket.MESSAGE_TYPE_GENERAL_INFO, "", String.Format("{0}\'s position: ZoneID: {1}, X: {2}, Y: {3}, Z: {4}, Rotation: {5}", p.customDisplayName, p.zoneId, p.positionX, p.positionY, p.positionZ, p.rotation)), true, false));
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();
                    Log.info(String.Format("{0}\'s position: ZoneID: {1}, X: {2}, Y: {3}, Z: {4}, Rotation: {5}", p.customDisplayName, p.zoneId, p.positionX, p.positionY, p.positionZ, p.rotation));
                }
            }
        }

        private void setGraphic(ConnectedPlayer client, uint slot, uint wId, uint eId, uint vId, uint cId)
        {
            if (client != null)
            {
                Player p = client.getActor();
                p.graphicChange(slot, wId, eId, vId, cId);
                p.sendAppearance();
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();
                    p.graphicChange(slot, wId, eId, vId, cId);
                    p.sendAppearance();
                }
            }
        }

        private void giveItem(ConnectedPlayer client, uint itemId, int quantity)
        {
            if (client != null)
            {
                Player p = client.getActor();
                p.getInventory(Inventory.NORMAL).addItem(itemId, quantity);
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();
                    p.getInventory(Inventory.NORMAL).addItem(itemId, quantity);
                }
            }
        }

        private void giveItem(ConnectedPlayer client, uint itemId, int quantity, ushort type)
        {
            if (client != null)
            {
                Player p = client.getActor();

                if (p.getInventory(type) != null)
                    p.getInventory(type).addItem(itemId, quantity);
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();

                    if (p.getInventory(type) != null)
                        p.getInventory(type).addItem(itemId, quantity);
                }
            }
        }

        private void removeItem(ConnectedPlayer client, uint itemId, int quantity)
        {
            if (client != null)
            {
                Player p = client.getActor();
                p.getInventory(Inventory.NORMAL).removeItem(itemId, quantity);
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();
                    p.getInventory(Inventory.NORMAL).removeItem(itemId, quantity);
                }
            }
        }

        private void removeItem(ConnectedPlayer client, uint itemId, int quantity, ushort type)
        {
            if (client != null)
            {
                Player p = client.getActor();

                if (p.getInventory(type) != null)
                    p.getInventory(type).removeItem(itemId, quantity);
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();

                    if (p.getInventory(type) != null)
                        p.getInventory(type).removeItem(itemId, quantity);
                }
            }
        }

        private void giveCurrency(ConnectedPlayer client, uint itemId, int quantity)
        {
            if (client != null)
            {
                Player p = client.getActor();
                p.getInventory(Inventory.CURRENCY).addItem(itemId, quantity);
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();
                    p.getInventory(Inventory.CURRENCY).addItem(itemId, quantity);
                }
            }
        }

        private void removeCurrency(ConnectedPlayer client, uint itemId, int quantity)
        {
            if (client != null)
            {
                Player p = client.getActor();
                p.getInventory(Inventory.CURRENCY).removeItem(itemId, quantity);
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();
                    p.getInventory(Inventory.CURRENCY).removeItem(itemId, quantity);
                }
            }
        }

        private void giveKeyItem(ConnectedPlayer client, uint itemId)
        {
            if (client != null)
            {
                Player p = client.getActor();
                p.getInventory(Inventory.KEYITEMS).addItem(itemId, 1);
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();
                    p.getInventory(Inventory.KEYITEMS).addItem(itemId, 1);
                }
            }
        }

        private void removeKeyItem(ConnectedPlayer client, uint itemId)
        {
            if (client != null)
            {
                Player p = client.getActor();
                p.getInventory(Inventory.KEYITEMS).removeItem(itemId, 1);
            }
            else
            {
                foreach (KeyValuePair<uint, ConnectedPlayer> entry in mConnectedPlayerList)
                {
                    Player p = entry.Value.getActor();
                    p.getInventory(Inventory.KEYITEMS).removeItem(itemId, 1);
                }
            }
        }

        /// <summary>
        /// We only use the default options for SendMessagePacket.
        /// May as well make it less unwieldly to view
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void sendMessage(ConnectedPlayer client, String message)
        {
            if (client != null)
               client.getActor().queuePacket(SendMessagePacket.buildPacket(client.actorID, client.actorID, SendMessagePacket.MESSAGE_TYPE_GENERAL_INFO, "", message));
        }

        internal bool doCommand(string input, ConnectedPlayer client)
        {
            input.Trim();
            if (input.StartsWith("!"))
                input = input.Substring(1);

            String[] split = input.Split(' ');
            split = split.Select(temp => temp.ToLower()).ToArray(); // Ignore case on commands


            // Debug
            //sendMessage(client, string.Join(",", split));

            if (split.Length >= 1)
            {
                if (split[0].Equals("help"))
                {
                    if (split.Length == 1)
                    {
                        sendMessage(client, Resources.CPhelp);
                    }
                    if (split.Length == 2)
                    {
                        if (split[1].Equals("mypos"))
                            sendMessage(client, Resources.CPmypos);
                        else if (split[1].Equals("music"))
                            sendMessage(client, Resources.CPmusic);
                        else if (split[1].Equals("warp"))
                            sendMessage(client, Resources.CPwarp);
                        else if (split[1].Equals("givecurrency"))
                            sendMessage(client, Resources.CPgivecurrency);
                        else if (split[1].Equals("giveitem"))
                            sendMessage(client, Resources.CPgiveitem);
                        else if (split[1].Equals("givekeyitem"))
                            sendMessage(client, Resources.CPgivekeyitem);
                        else if (split[1].Equals("removecurrency"))
                            sendMessage(client, Resources.CPremovecurrency);
                        else if (split[1].Equals("removeitem"))
                            sendMessage(client, Resources.CPremoveitem);
                        else if (split[1].Equals("removekeyitem"))
                            sendMessage(client, Resources.CPremovekeyitem);
                        else if (split[1].Equals("reloaditems"))
                            sendMessage(client, Resources.CPreloaditems);
                        else if (split[1].Equals("reloadzones"))
                            sendMessage(client, Resources.CPreloadzones);
                        /*
                        else if (split[1].Equals("property"))
                            sendMessage(client, Resources.CPproperty);
                        else if (split[1].Equals("property2"))
                            sendMessage(client, Resources.CPproperty2);
                        else if (split[1].Equals("sendpacket"))
                             sendMessage(client, Resources.CPsendpacket);
                        else if (split[1].Equals("setgraphic"))
                               sendMessage(client, Resources.CPsetgraphic);
                        */
                    }

                    return true;
                }
                else if (split[0].Equals("mypos"))
                {
                    try
                    {
                        printPos(client);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not load packet: " + e);
                    }
                }
                else if (split[0].Equals("reloadzones"))
                {
                    if (client != null)
                    {
                        Log.info(String.Format("Got request to reset zone: {0}", client.getActor().zoneId));
                        client.getActor().zone.clear();
                        client.getActor().zone.addActorToZone(client.getActor());
                        client.getActor().sendInstanceUpdate();
                        client.queuePacket(BasePacket.createPacket(SendMessagePacket.buildPacket(client.actorID, client.actorID, SendMessagePacket.MESSAGE_TYPE_GENERAL_INFO, "", String.Format("Reseting zone {0}...", client.getActor().zoneId)), true, false));
                    }
                    mWorldManager.reloadZone(client.getActor().zoneId);
                    return true;
                }
                else if (split[0].Equals("reloaditems"))
                {
                    Log.info(String.Format("Got request to reload item gamedata"));
                    if (client != null)
                        client.getActor().queuePacket(SendMessagePacket.buildPacket(client.actorID, client.actorID, SendMessagePacket.MESSAGE_TYPE_GENERAL_INFO, "", "Reloading Item Gamedata..."));
                    gamedataItems.Clear();
                    gamedataItems = Database.getItemGamedata();
                    Log.info(String.Format("Loaded {0} items.", gamedataItems.Count));
                    if (client != null)
                        client.getActor().queuePacket(SendMessagePacket.buildPacket(client.actorID, client.actorID, SendMessagePacket.MESSAGE_TYPE_GENERAL_INFO, "", String.Format("Loaded {0} items.", gamedataItems.Count)));
                    return true;
                }
                else if (split[0].Equals("sendpacket"))
                {
                    if (split.Length < 2)
                        return false;

                    try
                    {
                        sendPacket(client, "./packets/" + split[1]);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not load packet: " + e);
                    }
                }
                else if (split[0].Equals("graphic"))
                {
                    try
                    {
                        if (split.Length == 6)
                            setGraphic(client, UInt32.Parse(split[1]), UInt32.Parse(split[2]), UInt32.Parse(split[3]), UInt32.Parse(split[4]), UInt32.Parse(split[5]));
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not give item.");
                    }
                }
                else if (split[0].Equals("giveitem"))
                {
                    try
                    {
                        if (split.Length == 2)
                            giveItem(client, UInt32.Parse(split[1]), 1);
                        else if (split.Length == 3)
                            giveItem(client, UInt32.Parse(split[1]), Int32.Parse(split[2]));
                        else if (split.Length == 4)
                            giveItem(client, UInt32.Parse(split[1]), Int32.Parse(split[2]), UInt16.Parse(split[3]));
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not give item.");
                    }
                }
                else if (split[0].Equals("removeitem"))
                {
                    if (split.Length < 2)
                        return false;

                    try
                    {
                        if (split.Length == 2)
                            removeItem(client, UInt32.Parse(split[1]), 1);
                        else if (split.Length == 3)
                            removeItem(client, UInt32.Parse(split[1]), Int32.Parse(split[2]));
                        else if (split.Length == 4)
                            removeItem(client, UInt32.Parse(split[1]), Int32.Parse(split[2]), UInt16.Parse(split[3]));
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not remove item.");
                    }
                }
                else if (split[0].Equals("givekeyitem"))
                {
                    try
                    {
                        if (split.Length == 2)
                            giveKeyItem(client, UInt32.Parse(split[1]));
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not give keyitem.");
                    }
                }
                else if (split[0].Equals("removekeyitem"))
                {
                    if (split.Length < 2)
                        return false;

                    try
                    {
                        if (split.Length == 2)
                            removeKeyItem(client, UInt32.Parse(split[1]));
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not remove keyitem.");
                    }
                }
                else if (split[0].Equals("givecurrency"))
                {
                    try
                    {
                        if (split.Length == 2)
                            giveCurrency(client, ITEM_GIL, Int32.Parse(split[1]));
                        else if (split.Length == 3)
                            giveCurrency(client, UInt32.Parse(split[1]), Int32.Parse(split[2]));
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not give currency.");
                    }
                }
                else if (split[0].Equals("removecurrency"))
                {
                    if (split.Length < 2)
                        return false;

                    try
                    {
                        if (split.Length == 2)
                            removeCurrency(client, ITEM_GIL, Int32.Parse(split[1]));
                        else if (split.Length == 3)
                            removeCurrency(client, UInt32.Parse(split[1]), Int32.Parse(split[2]));
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not remove currency.");
                    }
                }
                else if (split[0].Equals("music"))
                {
                    if (split.Length < 2)
                        return false;

                    try
                    {
                        doMusic(client, split[1]);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.error("Could not change music: " + e);
                    }
                }
                else if (split[0].Equals("warp"))
                {
                    if (split.Length == 2)
                        doWarp(client, split[1]);
                    else if (split.Length == 5)
                        doWarp(client, split[1], null, split[2], split[3], split[4]);
                    else if (split.Length == 6)
                        doWarp(client, split[1], split[2], split[3], split[4], split[5]);
                    return true;
                }
                else if (split[0].Equals("property"))
                {
                    if (split.Length == 4)
                    {
                        changeProperty(Utils.MurmurHash2(split[1], 0), Convert.ToUInt32(split[2], 16), split[3]);
                    }
                    return true;
                }
                else if (split[0].Equals("property2"))
                {
                    if (split.Length == 4)
                    {
                        changeProperty(Convert.ToUInt32(split[1], 16), Convert.ToUInt32(split[2], 16), split[3]);
                    }
                    return true;
                }
            }
            return false;
        }
    }

}
