call /scripts/misc/onlinehelputils.tcl
layout clear
ohlp_initstyle

// ---- do not change anything above this line ---

layout print "/(ac)/(fn4) Deathmatch /p"

layout print "/(ac,fn1)/p"
pickone {	"You’ve lost..."
			"You’ve failed..."
			"You have been defeated..."
			"This is your end..."
			"Your mission has failed..."
			"You blew it..."
			"The vultures are already waiting..."
			"The last one gets eaten by the hamsters..."
			"You’ve kicked the bucket..."
		}
layout print "/p/p/(ac)/(iidata/gui/docpix/Deathmatch_l.tga)/p"

// ---- do not change anything below this line ---

layout print "/p"
