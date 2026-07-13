# wuker methods

method get_trapped {type} {
	if {$type=="petrify"} {
		set trap_time 30
		set trap_mode 0
		set trap_anim "petrified"
		set trap_type "petrify"
	}
	if {$type=="splat"} {
		set trap_time 3
		set trap_mode 0
		set trap_anim "dieminea"
		set trap_type "splat"
	}
	state_triggerfresh this trapped
}

method destroy {} {
	destruct this
	del this
}

method seq_idle {} {
	seq_idle
}


method Editor_Set_Info {ifo} {
	set info_string $ifo
	foreach entry $ifo {
		switch [lindex $entry 0] {
			"aggr"		{ set player_aggressivity [lindex $entry 1] }
			"prisoned"	{ set prisoned [lindex $entry 1] }
			"aggrmax" {set aggr_max [lindex $entry 1]}
		}
	}
}
