if {[in_class_def]} {
	call scripts/misc/animclassinit.tcl
	def_event evt_timer0
	method oeffnen {requestor time} {
		set opentime $time
		if {[get_posx $requestor]>[get_posx this]} {set side a} {set side b}
		if {$status=="closed"} {
			action this anim open$side {
				set status open
				set_attrib this Schaltstatus 0
				set_pf_influence this 0 0 0 0 0 0
				if {$opentime!=-1} {
					action this wait $opentime {
						call_method [get_ref this] schliessen
					}
				} else {
					set_anim this [subst \$openanim$side] 0 $ANIM_STILL
				}
			}
		}
		rem_fstopper this
	}
	method schliessen {} {
		if {$status=="open"} {
			action this anim close$side {
				set status closed
				if {$influence_pf} {set_pf_influence this -1 -20 +1 +4 INT_MAX 0}
				set_attrib this Schaltstatus 1
				set_anim this $standanim 0 $ANIM_STILL
			}
		}
		set_fstopper this {0 -2} {0 1} 0
	}
	method schalten {requestor swttime} {
		if {$switcheroperator=="and"} {
			foreach item $switchers {
				if {[ref_get $item status]=="off"&&$item!=$requestor} {return}
			}
		}
		call_method this oeffnen $requestor $swttime
	}
	method_const walk_collision_callback {ref} {
		log "Door [get_ref this] colliding with $ref ?"
		if {[get_attrib this Schaltstatus]} {
			log "YES"
			return 1
		} else {
			log "NO"
			return 0
		}
	}
	method get_uniquename {} {return $name}
	method door_ident {} {return "door"}
	method Editor_Set_Info {infolist} {
		global info_string
		set info_string $infolist
		foreach sublist $infolist {
			switch [lindex $sublist 0] {
				"name" {set name [lindex $sublist 1]}
				"switchmode" {set remotemode [lindex $sublist 1]}
				"switchercnt" {set switchercnt [lindex $sublist 1]}
				"connection" {set switcheroperator [lindex $sublist 1]}
				"switcherrange" {set switcherrange [lindex $sublist 1]}
				"switcherlist" {
					set definedswitchers [lindex $sublist 1]
					set switcherrange 1000
					set switchercnt [llength $definedswitchers]
				}
				"init" 	{
							if { [lindex $sublist 1] == "open" } {
								call_method this oeffnen this -1
							}
				}
				"swc"	{
					set switchercontrol [lindex $sublist 1]
				}
			}
		}
	}
	handle_event evt_timer0 {
		set switcherlist [obj_query this "-type tool -range $switcherrange"]
		//log "DOOR [get_ref this]: sl $switcherlist"
		if {$switcherlist==0||$switchercontrol} {return}
		foreach item $switcherlist {
			if {[string range [get_objclass $item] 0 7]=="Schalter"} {
				if {$definedswitchers!=""} {
					if {[check_method [get_objclass $item] get_uniquename]} {
						if {-1!=[lsearch $definedswitchers [call_method $item get_uniquename]]} {
							lappend switchers $item
						}
					}
				} else {
					lappend switchers $item
				}
			if {[llength $switchers]==$switchercnt} {break}
			}
		}
		foreach item $switchers {
			if {![ref_get $item predefined]} {call_method $item set_switchmode $remotemode}
			//log "---------------------"
			//log "DOORFOUND switcher ([get_ref this]): $item ([ref_get $item predefined])"
			if {[call_method $item get_var 1]==""} {call_method $item set_var 1 [get_ref this]}
			call_method $item set_actiononpress "call_method \[find_door \$var1\] schalten $item -1"
			call_method $item set_actiononrelease "call_method \[find_door \$var1\] schliessen"
		}
	}
	obj_exit {
		set_pf_influence this 0 0 0 0 0 0
	}
} else {
	call scripts/misc/animclassinit.tcl
	set_hoverable this 0
	set_anim this $standanim 0 $ANIM_STILL
	set status closed
	set_attrib this Schaltstatus 1
	set info_string ""
	set remotemode "once"
	set switchercnt 1
	set switcheroperator "or"
	set switcherrange 5
	set switchers [list]
	set definedswitchers [list]
	set name [get_objname this]
	set switchercontrol 0
	set_collision this 1
	set_viewinfog this 1
	set_buildupstate this 1
	if {$influence_pf} {set_pf_influence this -1 -20 +1 +4 INT_MAX 0}
	timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
}
