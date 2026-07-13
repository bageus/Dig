call /scripts/misc/onlinehelputils.tcl
layout clear
ohlp_initstyle

// ---- do not change anything above this line ---

layout print "/(ac)/(fn4) Deathmatch /p/p"

layout print "/(ac,fn1,ls5)"
pickone {	"You’ve won!"
			"Mission completed!"
			"Victory!!!"
			"You are the winner!!!"
			"Mission successful!"
		}
layout print "/p/p/(ac)/(iidata/gui/docpix/Deathmatch_w.tga)/p"

// ---- do not change anything below this line ---

layout print "/p"