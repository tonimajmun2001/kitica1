--[[

CheckCommand Script

Handles player examining someone

--]]

function onEventStarted(player, commandactor, arg1, arg2, arg3, arg4, checkedActorId)

	actor = player:getActorInInstance(checkedActorId);
	
	if (actor ~= nil) then
		player:examinePlayer(actor);
	end
	
	player:endEvent();
	
end