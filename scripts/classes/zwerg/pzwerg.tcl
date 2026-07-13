def_class PseudoZwerg none dummy 0 {} {
	call scripts/misc/animclassinit.tcl	// anim members
	call scripts/misc/obj_attribs.tcl

	def_event evt_timer0
	def_event evt_zwerg_birth
	def_event evt_zwerg_workannounce

	method Editor_Set_Info {ifo} {
		set info_string $ifo
	}
	
	method set_gender {gender} {
		set gnome_gender $gender
		set_objgender this $gender
		set_objname this auto $gender
	}

	method activate {} {
		sel /obj
		set nz [new Zwerg]
		set_owner $nz [get_owner this]
		set_pos $nz [get_pos this]
		if { [catch {set_rot $nz [get_rot this]}] } {
			log "set_rot ERROR"
		}
		set otherattribs {}
		foreach attribut [get_expattrib] {
			lappend otherattribs [get_attrib this $attribut]
		}
		call_method $nz baby_to_gnome $gnome_gender [get_objname this] {} [get_attrib this atr_Nutrition] [get_attrib this atr_Alertness] [get_attrib this atr_Mood] [get_attrib this atr_Hitpoints] [get_attrib this atr_ExpMax] $otherattribs 1800
		call_method $nz init
		del this
		return $nz
	}

	obj_init {

		set gnome_gender "unset"
		set nam 0
		set_weapon_class this 0
		set_shield_class this 0
		set_texturevariation this [hf2i [random 4]] 0
		set_anim this mann.standard 0 $ANIM_LOOP		;# set standard anim

		timer_event this evt_timer0 -repeat 1 -interval 1 -userid 0 -attime 3

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

	}


	handle_event evt_timer0 {
		set_visibility this 0
		set_collision this 0
		foreach atr [concat atr_ExpMax [get_expattrib]] {
			if {[set val [get_info $atr]]} {
				set_attrib this $atr $val
			}
		}
		set nam [get_info name]
		if { $gnome_gender == "unset" } {
			set gend [get_info gender]
			if { $gend != 0 } {
				set gnome_gender $gend
			} else {
				if { ! [minimalrun] } {
					set gnome_gender [auto_choose_gender this]
				} else {
					set gnome_gender "male"
				}
			}
			set nam [get_info name]
		}
		if { $nam != 0 } {
			set_objname this $nam
		}
		if { $gnome_gender == "female" } {
			set_alternateanimdb this true
			if { $nam == 0 } {
				set_objname this auto female
			}
		} else {
			if { $nam == 0 } {
				set_objname this auto male
			}
		}
	}

}


