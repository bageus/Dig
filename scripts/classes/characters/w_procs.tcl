proc walk_pos {pos} {
	state_disable this
	set pos [vector_fix $pos]
	action this walk "-target \{$pos\} -animsets 0 -useobjects 0" {state_enable this}
	return true
}

proc walk_random_cl {plength} {
	state_disable this
	action this walk "-animsets 0  -randompath $plength -randomz 5 -useobjects 0" {state_enable this}
	return true
}

proc walk_random {plength} {
	set cc [get_gnomeposition this]
	state_disable this
	action this walk "-canclimb $cc -animsets 0  -randompath $plength -randomz 5 -useobjects 0" {state_enable this}
	return true
}

proc sniff_pos {pos} {
	state_disable this
	set pos [vector_fix $pos]
	action this walk "-target \{$pos\} -animsets 2 -useobjects 0" {state_enable this}
	return true
}

proc sniff_random {plength} {
	set cc [get_gnomeposition this]
	state_disable this
	action this walk "-canclimb $cc -animsets 2 -randompath $plength -randomz 5 -useobjects 0" {state_enable this}
	return true
}

proc run_pos {pos} {
	state_disable this
	set pos [vector_fix $pos]
	action this walk "-target \{$pos\} -animsets 1 -useobjects 0" {state_enable this}
	return true
}

proc run_pos_obj {pos obj {dist 1.8}} {
	state_disable this
	//set pos [vector_fix $pos]
	action this walk "-target \{$pos\} -animsets 1 -useobjects 0" "state_enable this;run_pos_stop $obj"
	return true
}

proc wait_time {time} {
	state_disable this
	action this wait $time { state_enable this }
}

proc play_anim {anim} {
	state_disable this
	action this anim $anim {state_enable this}
	return true
}

proc scratchc {} {
	tasklist_add this "play_anim scratchcstart"
	tasklist_add this "play_anim scratchcloop"
	tasklist_add this "play_anim scratchcloop"
	tasklist_add this "play_anim scratchcloop"
	tasklist_add this "play_anim scratchcend"
}

proc looking {} {
	tasklist_add this "play_anim lookleft"
	tasklist_add this "play_anim lookright"
}

proc jumping {} {
	tasklist_add this "play_anim jumpa"
//	tasklist_add this "play_anim jumpb"
}

proc rotate_toright {} {state_disable this;action this rotate 4.71 {state_enable this}}
proc rotate_toleft {} {state_disable this;action this rotate 1.57 {state_enable this}}
proc rotate_toback {} {state_disable this;action this rotate 3.14 {state_enable this}}
proc rotate_tofront {} {state_disable this;action this rotate 0 {state_enable this}}

proc rotate_to {dir} {
	switch $dir {
		right
			{rotate_toright}
		left
			{rotate_toleft}
		front
			{rotate_tofront}
		back
			{rotate_toback}
		default
			{rotate_tofront}
	}
}

proc rotate_towards {item {direction 0} {animset 0}} {
	if {[get_gnomeposition this]} {return}
	if {[llength $item]==1} {
		if {![obj_valid $item]} {
			return false
		}
			set itempos [get_pos $item]
	} else {
		set itempos $item
	}
	set angle [vector_angle "[lindex $itempos 0] 0 [expr [lindex $itempos 2]*0.5]" "[get_posx this] 0 [expr [get_posz this]*0.5]"]
	if {$direction} {fincr angle 1.57} {fincr angle -1.57}
	if {$angle<0.0} {fincr angle 6.2832}
	if {$angle>6.2832} {fincr angle -6.2832}
	set myangle [get_roty this]
	if {abs($angle-$myangle)<0.05||abs($angle-$myangle-6.28)<0.05} {return true}
	state_disable this
	action this rotate {$angle $animset} {state_enable this} {}
	return true
}


proc set_idle_anim {} {
	if { [get_gnomeposition this] == 0 } {
		set_anim this wuker.stand_atmen_a 0 2 ;#set idle anim
	} else {
		set_anim this wuker.kletter_standanim 0 2 ;#set idle anim
	}
}

proc loop_anim {anim min max} {
	set reps [hf2i [random [expr $max - $min]]]
	incr reps $min
	for {set i 0} {$i < $reps} {incr i} {
		tasklist_add this "play_anim $anim"
	}
}

proc find_enemy {} {
	if {[get_max_fow this]<10} {return 0}
	global scan_range attack_behaviour attack_item look_dir new_fight_pos sniff_range approach
	global enemy_classes
	set mindist 1000
	set attack_item 0
	set fzwerg_list [obj_query this -class $enemy_classes -range $sniff_range]
	if { $fzwerg_list == 0 } {
		return 0
	}
	foreach fzwerg $fzwerg_list {
		set ownpos [get_pos this]
		set enemypos [get_pos $fzwerg]
		set dist [vector_dist $ownpos $enemypos]
		if { $dist < $mindist } {
			set mindist $dist					;# nahen zwerg suchen für schnüffeln
		}
		set attack_behaviour "offensive"
		set attack_item $fzwerg
		if { $dist > $scan_range } {
			continue
		}
		if { [state_get $attack_item] == "fight_dispatch"  } {
			continue
		}

		log "[get_objname this]: [get_objname $attack_item]has [get_attrib $attack_item atr_Hitpoints] HP"
		if { [get_attrib $attack_item atr_Hitpoints] < 0.01 } {
			continue
		}

		if { [get_attack_pos this $attack_item] == 0 } { continue }
		set approach 1
		fight_startfight
		return 1
	}
	if { $mindist < $sniff_range } {
		return 2
	}
	return 0
}

proc beamto_inv {item} {
	inv_add this $item
	set_rotx $item 0.0
	set_rotz $item 0.0
	set_owner $item [get_owner this]
	return true
}

