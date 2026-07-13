call /scripts/misc/onlinehelputils.tcl

proc restart {} {
	set ex "call_method_static GameObserver ExecBroadcastWin RestartCF"
	return [layout autoxlink $ex /(tx[lmsg "continue"])]
}

layout clear
ohlp_initstyle

// ---- do not change anything above this line ---

layout print "/(ac)/(fn4) Capture The Flag /p/p"

layout print "/(ac,fn1,ls5)"
pickone {	"You’ve won!"
			"Mission completed!"
			"Victory!!!"
			"You are the winner!!!"
			"Mission successful!"
		}

// ---- do not change anything below this line ---

layout print "/p/(ac)[restart]"
layout print "/p/(ac)/(iidata/gui/docpix/CaptureTheFlag_w.tga)/p"
layout print "/p"
