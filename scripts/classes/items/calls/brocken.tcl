if {[in_class_def]} {
	call scripts/classes/items/calls/resources.tcl
	call scripts/misc/aggr_events.tcl
	method mine {obj} {
		if {$content > 0} {
			incr content -1
			add_attrib this PilzAge -1
			set pos [get_pos $obj]
			set pos [vector_add $pos [vector_random 0.6 0 0.6]]
			sel /obj
			set ns [new [string map {brocken ""} [get_objclass this]]]
			set_pos $ns $pos
			set_owner $ns [get_owner this]
			eval "add_expattrib $obj $expincr"
			setanim
		}
		if {$content == 0} {
			del this
		}
	}
	method change_owner {new_owner} {
		add_owner_attrib [get_owner this] [get_objclass this] -1
		set_owner this $new_owner
		add_owner_attrib [get_owner this] [get_objclass this] 1
	}
	//method get_content {} {
	//	return $content
	//}
	//method get_radius {} {
	//	return [hmax [expr $content / 30] 0.5]
	//}
	method Editor_Set_Info {ifo} {
		global info_string
		set info_string $ifo
		set ifo [lindex $ifo 0]
		set cnt [lindex $ifo 1]
		if { [string is integer $cnt] } {
			set content $cnt
			set_attrib this PilzAge $cnt
			setanim
		} else {
			log "[get_objname this]: Error content is not an integer: '$ifo'"
		}
		foreach entry $ifo {
			switch [lindex $entry 0] {
				"aggr" {set player_aggressivity [lindex $entry 1]}
				"aggrmax" {set aggr_max [lindex $entry 1]}
			}
		}
	}

} else {
	call scripts/classes/items/calls/resources.tcl
	call scripts/misc/aggr_events.tcl
	set content 30
	set_attrib this PilzAge 30
	set info_string "\{inhalt 30\}"
	set ani [string tolower [string map {brocken ""} [get_objclass this]]]
	set_anim this "$ani\_00.standard" 0 0
	set_viewinfog this 1
	set_storable this 1
	set_physic this 1
	set_attrib this weight 0.38
	set_hoverable this 1
	set_collision this 1

	proc setanim {} {
		global content
		//set aniph [hmax [expr 9 - int($content / 3)] 0]
		set aniph 0
		if 			{$content <= 2} {
			set aniph 9
		} elseif	{$content <= 3} {
			set aniph 8
		} elseif 	{$content <= 4} {
			set aniph 7
		} elseif 	{$content <= 5} {
			set aniph 6
		} elseif 	{$content <= 7} {
			set aniph 5
		} elseif 	{$content <= 10} {
			set aniph 4
		} elseif 	{$content <= 13} {
			set aniph 3
		} elseif 	{$content <= 16} {
			set aniph 2
		} elseif 	{$content <= 20} {
			set aniph 1
		}

		set ani [string tolower [string map {brocken ""} [get_objclass this]]]
		set_anim this $ani\_00.standard $aniph 0
	}
}
