if {[in_class_def]} {

	method get_info {name} {
		global info_string undefined
		foreach item $info_string {
			set inam [lindex $item 0]
			set ival [lindex $item 1]
			if { $name == $inam } {
				return $ival
			}
		}
		return $undefined
	}

	method destroy {} {
//		log "[get_objname this] deleted..."
		del this
	}

	method init {} {
	}

	method Editor_Set_Info {ifo} {
		// Anti-Verklammerungs-Filter '}{' -> '} {'
		set ifo [string map {"\}\{" "\} \{"} $ifo]
		set info_string $ifo
		call_method this init
	}


} else {

	set undefined 0

	if { ![info exists info_string] } {
		set info_string ""
	}

	proc get_info {name} {
		global info_string
		if { ![info exists info_string]  } {set info_string ""}
		return [call_method this get_info $name]
	}

	proc set_info {ifo} {
		global info_string
		//log "ifo : $ifo"
		set info_string [lreplace [split $ifo "-"] 0 0]
		//log "ifo_str: $info_string"
	}
	if { [lindex [split [get_objname this] "_"] 1] == "Pos" } {
		set_anim this trigger_fahne3.standard 0 0
	} else {
		set_anim this trigger_fahne2.standard 0 0
	}

	if {[lindex [split [get_objname this] "_"] 1] == "Lore" } {
		set_anim this achse_zug.standard 0 0
	}

	if {![get_mapedit]} {
		set_visibility this 0
	}
}
