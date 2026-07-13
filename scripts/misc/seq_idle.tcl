if { [info exists seq_idle_anims] } {
	set seq_idle_list [list]
	set seq_idle_table [list]
	set index 0
	foreach item $seq_idle_anims {
		set ch [lindex $item 0]
    	set bWall 0
    	if { [string first "w" $ch] != -1 } {
    		set bWall 1
    		set ch [string map {"w" ""} $ch]
    	}
    	for {set i 0} {$i < $ch} {incr i} {
    		set aidx $index
    		if { $bWall } {
    			set aidx "w$aidx"
    		}
    		lappend seq_idle_table $aidx
    	}
    	incr index
    }
    set seq_idle_last_index -1

    proc lfilter {lOrgL bM sF {iIdx 0}} {
    	set lNew [list]
    	foreach item $lOrgL {
    		set bFound [expr [string first $sF [lindex $item $iIdx]] != -1]
    		if { $bFound == $bM } {
    			set item [string map "$sF \"\"" $item]
    			lappend lNew $item
    		}
    	}
    	return $lNew
    }

    proc class2DB {cname} {
    	switch $cname {
    		"Zwerg" {return "mann"}
    		"Troll" {return "troll"}
    		"Wuker" {return "wuker"}
    		"Drachenbaby" {return "drache01"}
    	}
    }

    proc seq_idle {} {
    	global seq_idle_list seq_idle_anims seq_idle_table seq_idle_last_index

    	if { [llength $seq_idle_list] == 0 } {
    		set newtable [lfilter [lnand $seq_idle_last_index $seq_idle_table] [get_gnomeposition this] "w"]

    		if { [llength $newtable] == 0 } {
    			set seq_idle_list standard
    		} else {
        		set ridx [irandom [llength $newtable]]
        		set idx [lindex $newtable $ridx]
        		set anims [lindex [lindex $seq_idle_anims $idx] 1]
        		foreach item $anims {
        			lappend seq_idle_list $item
        		}
        		set seq_idle_last_index $idx
        	}
    	}

    	if { [llength $seq_idle_list] > 0 } {
    		set nextidle [lrem seq_idle_list 0]
   			action this anim [class2DB [get_objclass this]].$nextidle {seq_idle}
    	}
    }
} else {
	proc seq_idle {} {log "Warning: no seq_idle_anims defined for Class [get_objclass this]"}
}


