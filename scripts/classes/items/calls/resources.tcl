if {[in_class_def]} {
	def_event evt_recource_init

	handle_event evt_recource_init {
        set lock [get_info "lock"]
        if {$lock != 0} {
        	set_lock this 1
        }
	}

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

	method Editor_Set_Info {ifo} {
		set info_string $ifo
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

	timer_event this evt_recource_init -repeat 0 -userid 0 -attime [expr [gettime]+0.3]
}
