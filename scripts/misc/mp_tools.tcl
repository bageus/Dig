set temp_list [list]

set wassplit 0
proc split_template {orgname orgpmp iW iH templist} {
	global bSplit SplitTemplateList SplitPMPList temppos wassplit
	//log "MPSplitLoad: $orgname"
	foreach item $templist {
		set tmp [lindex $item 0]
		set pmp [lindex $item 1]
		set x [lindex $item 2]
		set y [lindex $item 3]
		map_template "data/templates/pmp/split/$pmp" [expr $x + [lindex $temppos 0]] [expr $y + [lindex $temppos 1]]
		call "data/templates/tcl/split/$tmp"
		MapTemplateSet [expr $x + [lindex $temppos 0]] [expr $y + [lindex $temppos 1]]
	}
	set wassplit 1
	proc MapTemplateSet {x y} {}	;# Dummyproc
}



proc apply_level {level} {
	global temppos wassplit
	foreach nexttemp $level {

       	if { [lindex $nexttemp 0] == "mat" } {
       		//log "applying material: $nexttemp"
       		sm_draw_stone -matlist 16 16 $nexttemp
       		continue
       	}

		proc MapTemplateSet {x y} {}	;# Dummyproc

    	set template [lindex $nexttemp 0]
    	set x [lindex $nexttemp 1]
    	set y [lindex $nexttemp 2]

		//log "Multiplayer: loading template: $template"

		set temppos "$x $y"
		call "data/templates/tcl/$template"

		if { !$wassplit } { MapTemplateSet $x $y }

		set wassplit 0
    }
}


proc send_status_msg {msg {showloading 0}} {
	log "Net: SendStatusMsg: $msg"
	net pushstatusmsg $msg
	if { $showloading } {
		load_info $msg
	}
}

proc recieve_status_msg {} {
	set msg [net popstatusmsg]
	if { $msg != "" } {
		log "Net: RecieveStatusMsg: $msg"
	}
	return $msg
}

set starttemplatelist [list]
proc create_starttemplatelist {tl} {
	global starttemplatelist
	set starttemplatelist [list]

	set tl [string map {"\n" " " "	" " "} $tl]
	set tl [string map {"  " " "} $tl]

	for {set i 1} {$i < 16} {incr i 2} {
		set sublist [list]

		set subl_urw  [list]
		set subl_swf  [list]
		set subl_kris [list]
		set subl_lava [list]

		set templist [lindex $tl $i]

		for {set it 0} {$it < 8} {incr it 2} {
			set subl_[lindex $templist $it] [lindex $templist [expr $it + 1]]
		}

		lappend sublist $subl_urw
		lappend sublist $subl_swf
		lappend sublist $subl_kris
		lappend sublist $subl_lava

		lappend starttemplatelist $sublist
	}
}


