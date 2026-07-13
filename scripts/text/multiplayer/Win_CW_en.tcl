call /scripts/misc/onlinehelputils.tcl

proc restart {} {
	set ex "call_method_static GameObserver ExecBroadcastWin RestartCW"
	set link [layout autoxlink $ex [lmsg "continue"]]
}

layout clear
ohlp_initstyle

// ---- do not change anything above this line ---

layout print "/(ac)/(fn4) CounterDiggles /p/p"

layout print "/(ac,fn1,ls5)"
pickone {	"You’ve won!"
			"Mission completed!"
			"Victory!!!"
			"You are the winner!!!"
			"Mission successful!"
		}

// ---- do not change anything below this line ---

layout print "/(ac)/p[restart]"
layout print "/p/(ac)/(iidata/gui/docpix/CounterWiggles_w.tga)/p"
layout print "/p"