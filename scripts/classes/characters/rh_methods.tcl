# Riesenhamster methods

method get_trapped {type} {
	if {$type=="petrify"} {
		set trap_time 30
		set trap_mode 0
		set trap_anim "petrified"
	}
	if {$type=="splat"} {
		set trap_time 3
		set trap_mode 0
		set trap_anim "dieminea"
	}
	state_triggerfresh this trapped 
}


method put_in_wheel {wheel} {
	global ANIM_LOOP is_in_wheel

	if {$is_in_wheel} {
		return
	}

	if {![obj_valid $wheel]} {
		return
	}

	action this wait 0.1	;// break all other actions
	set_pos this [vector_add [get_pos $wheel] {-0.2 -1.2 0}]
	set_rot this {0 4.71 0}
	set_visibility this 1
	set_anim this riesenhamster.laufrad 0 $ANIM_LOOP
	set is_in_wheel 1
	set_hoverable this 0
	state_disable this
}


method free {wheel} {
	global is_in_wheel
	
	set_pos this [vector_fix [get_pos this]]
	set is_in_wheel 0
	set_hoverable this 1
	state_enable this
	state_triggerfresh this idle
}


method destroy {} {
	destruct this
	del this
}