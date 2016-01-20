﻿using FFXIVClassic_Lobby_Server.packets;
using FFXIVClassic_Map_Server.actors.chara;
using FFXIVClassic_Map_Server.packets.send.actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVClassic_Map_Server.dataobjects.chara
{
    class Character:Actor
    {
        public const int SIZE = 0;
        public const int COLORINFO = 1;
        public const int FACEINFO = 2;
        public const int HIGHLIGHT_HAIR = 3;
        public const int VOICE = 4;
        public const int WEAPON1 = 5;
        public const int WEAPON2 = 6;
        public const int WEAPON3 = 7;
        public const int UNKNOWN1 = 8;
        public const int UNKNOWN2 = 9;
        public const int UNKNOWN3 = 10;
        public const int UNKNOWN4 = 11;
        public const int HEADGEAR = 12;
        public const int BODYGEAR = 13;
        public const int LEGSGEAR = 14;
        public const int HANDSGEAR = 15;
        public const int FEETGEAR = 16;
        public const int WAISTGEAR = 17;
        public const int UNKNOWN5 = 18;
        public const int R_EAR = 19;
        public const int L_EAR = 20;
        public const int UNKNOWN6 = 21;
        public const int UNKNOWN7 = 22;
        public const int R_FINGER = 23;
        public const int L_FINGER = 24;

        public uint modelId;
        public uint[] appearanceIds = new uint[0x1D];

        public uint animationId = 0;

        public uint currentTarget = 0xC0000000;
        public uint currentLockedTarget = 0xC0000000;

        public uint currentActorIcon = 0;

        public Work work = new Work();
        public CharaWork charaWork = new CharaWork();
        
        public Character(uint actorID) : base(actorID)
        {            
            //Init timer array to "notimer"
            for (int i = 0; i < charaWork.statusShownTime.Length; i++)
                charaWork.statusShownTime[i] = 0xFFFFFFFF;
        }

        public SubPacket createAppearancePacket(uint playerActorId)
        {
            SetActorAppearancePacket setappearance = new SetActorAppearancePacket(modelId, appearanceIds);
            return setappearance.buildPacket(actorId, playerActorId);
        }

        public SubPacket createInitStatusPacket(uint playerActorId)
        {
            return (SetActorStatusAllPacket.buildPacket(actorId, playerActorId, charaWork.status));                      
        }

        public SubPacket createSetActorIconPacket(uint playerActorId)
        {
            return SetActorIconPacket.buildPacket(actorId, playerActorId, currentActorIcon);
        }

        public SubPacket createIdleAnimationPacket(uint playerActorId)
        {
            return SetActorIdleAnimationPacket.buildPacket(actorId, playerActorId, animationId);
        }

    }

}