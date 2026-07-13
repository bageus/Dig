def_class Fee none dummy 0 {} {
	obj_init {
		set_physic this 0
		set_sequenceactive this 1
		state_triggerfresh this idle
		set rad 4
		set startpos 0
		set targpos 0
		set dir [expr 0.5 + [random 0.5]]
		set rotx 0 ;#.2
		set rotz 0 ;#.5
		set disort 0.5
        set dither 1

		//set rad [expr ]
		set dir [expr 0.1 + [random 0.8]]

		if { [irandom 2] == 1 } {
			set dir [expr $dir * -1]
		}

		change_particlesource this 0 25 {0 0 0} {0 0 0} 255 3 0
		set_particlesource this 0 1
	}

	state idle {

		set rnd [irandom 10]
		if { $rnd == 0 } {
			return
		}
		if { $rnd == 5 } {
			set dir [expr - $dir]
		}

		if { $startpos == 0 } {
			set startpos [get_pos this]
			set targpos $startpos
			#log "*** $startpos"
		}
		fairy_move this $targpos $rad $rotx $rotz [expr $dir * [hmax 1.0 $dither]]
		set nrad [expr [random [expr $rad / 3]] + (($rad * 2.0) / 3.0)]
		set rad [expr $rad * 0.9 + $nrad *0.1]
		if { [irandom 50] == 0 } {
			set rad [expr 1 + [random 3.5]]
		}
		fincr rotx [random [expr $disort * 0.1 * $dither]]
		fincr rotz [random [expr $disort * 0.1 * $dither]]
	}

	method move_to {pos} {
		set targpos $pos
	}

	method move_back {} {
		set targpos $startpos
	}

	method dither {fact} {
		set dither $fact
	}

	method normal {} {
		set dither 1
		set rad 4
	}

	method radius {r} {
		set rad $r
	}

}
