require("/quests/man/man0l0")

function init(npc)
	return "/Chara/Npc/Populace/PopulaceStandard", false, false, false, false, false, npc:GetActorClassId(), false, false, 0, 1, "TEST";	
end

function onSpawn(player, npc)

	man0l0Quest = player:GetQuest("man0l0");	
	
	if (man0l0Quest ~= nil) then
		if (man0l0Quest ~= nil) then
			if (man0l0Quest:GetQuestFlag(MAN0L0_FLAG_MINITUT_DONE3) == false) then
				npc:SetQuestGraphic(player, 0x2);
			end
		end
	end
end

function onEventStarted(player, npc, triggerName)

	man0l0Quest = player:GetQuest("man0l0");	

	if (triggerName == "talkDefault") then
		if (man0l0Quest:GetQuestFlag(MAN0L0_FLAG_MINITUT_DONE3) == false) then
			player:RunEventFunction("delegateEvent", player, man0l0Quest, "processTtrMini003", nil, nil, nil);
			npc:SetQuestGraphic(player, 0x0);
			man0l0Quest:SetQuestFlag(MAN0L0_FLAG_MINITUT_DONE3, true);
			man0l0Quest:SaveData();		
			player:GetDirector():OnTalked(npc);
		else
			player:RunEventFunction("delegateEvent", player, man0l0Quest, "processEvent000_8", nil, nil, nil);
		end
	else		
		player:EndEvent();
	end		
	
end

function onEventUpdate(player, npc)	
	player:EndEvent();
end