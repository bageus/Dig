def_class Fisch none tool 0 {moves} {
	call scripts/misc/animclassinit.tcl

	obj_init {

		set_anim this fisch_a.schwimmen 0 $ANIM_LOOP

		set_viewinfog this 0
		set_physic this 0
		set_attrib this weight 0.01
		set_attrib this hitpoints 0.02
		state_triggerfresh this idle
		set_selectable this 0
		set_hoverable this 0
		set_sequenceactive this 1

		set initialized 0
		set info_string ""
		set status "unfree"
		set flyrange 3.0

		set iSCount 5

		proc get_info {name} {
			global info_string
			if { ![info exists info_string] } {set info_string ""}
			foreach item $info_string {
				set inam [lindex $item 0]
				set ival [lindex $item 1]
				if { $name == $inam } {
					return $ival
				}
			}
			return 0
		}

		proc eval_info {ifo} {
			global status flyrange
			set rng [get_info "range"]
			if { $rng > 0.5 } {
				set flyrange $rng
			}
			set st [get_info "status"]
			if { $st != 0 } {
				set status $st
			}
		}

       	proc play_anim {anim} {
        	state_disable this
        	action this anim $anim {state_enable this}
        	return true
        }
	}

	method Editor_Set_Info {ifo} {
		set info_string $ifo
		eval_info $ifo
	}

	state idle {
		if { !$initialized } {
			#set_posy this [expr [get_posy this] - 1]
			set startpos [get_pos this]
			set initialized 1
		}
		set par1 "-range $flyrange -swim 1"
		set par2 ""
		if {$status != "free"} {
			set par2 "-around \{$startpos\}"
		}

		incr iSCount -1
		if { $iSCount <= 0 } {
			set iSCount 10

			set iRnd [irandom 5]
			switch $iRnd {
				0	{set_anim this fisch_a.schwimmen 0 2}
				1	{set_anim this fisch_a.treiben 0 2}
				2	{play_anim fisch_a.looping ; set iSCount 1; return}
				3   {play_anim fisch_a.ringelrei ; set iSCount 1; return}
				4   {play_anim fisch_a.springen ; set iSCount 1; return}
			}
		}

		set params "$par1 $par2"
		state_disable this
		if { [catch { action this fly $params "state_enable this" }] } {
			del this
		}
	}
}
