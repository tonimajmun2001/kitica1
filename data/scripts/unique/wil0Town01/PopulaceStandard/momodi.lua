function onEventStarted(player, npc)
	defaultWil = getStaticActor("DftWil");
	player:runEventFunction("delegateEvent", player, defaultWil, "defaultTalkWithMomodi_001", nil, nil, nil);
end

function onEventUpdate(player, npc, blah, menuSelect)
	
	player:endEvent();
	
end