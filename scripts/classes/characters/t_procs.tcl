proc set_idle_anim {} {
	if { [get_gnomeposition this] == 0 } {
		switch { [irandom 3] } {
			0	{set_anim this troll.stehen_warten_a 0 2}
			1	{set_anim this troll.stehen_warten_b 0 2}
			2	{set_anim this troll.stehen_warten_c 0 2}
		}
		set_anim this troll.stehen_warten_a 0 2 ;#set idle anim
	} else {
		set_anim this troll.stehen_warten_a 0 2 ;#set idle anim wall
	}
}

proc walk_random {plength} {
	state_disable this
	action this walk "-canclimb 0 -randompath $plength -randomz 5 -animsets 0" {state_enable this}
	particle_z 0
	return true
}

proc error_freeze {} {
//#ifdef _DEBUG
	state_disable this
	set_anim this troll.vorne_mitte_get_tot 4 0
//#elif
	play_anim troll.vorne_mitte_get_tot
	set_attrib this atr_Hitpoints 0.0
//#endif
}

proc walk_pos {pos} {
	state_disable this
	set pos [vector_fix $pos]
	action this walk "-target \{$pos\} -animsets 0" {state_enable this; }
	particle_z 0
	return true
}

proc run_pos {pos} {
	state_disable this
	set pos [vector_fix $pos]
	action this walk "-target \{$pos\} -animsets 1" {state_enable this}
	particle_z 0
	return true
}

proc run_pos_obj {pos obj {dist 1.8}} {
	state_disable this
	set pos [vector_fix $pos]
	action this walk "-target \{$pos\} -objbreak \{$obj $dist\} -animsets 1" "state_enable this; run_pos_stop $obj"
	particle_z 0
	return true
}

proc wait_time {time} {
	state_disable this
	action this wait $time { state_enable this }
}

proc particle_z {activate} {
	global part_z_active
	if {$activate != $part_z_active} {
		if {$activate} {
			change_particlesource this 1 4 {0 0 0} {0 0 0} 2 1 0 10
		}
		set_particlesource this 1 $activate
		set part_z_active $activate
	}
}

proc play_anim {anim} {
	global standstate
//	log "Troll [get_objname this]: playanim = $anim"
	switch $anim {
		"standup"	{set standstate "standing";particle_z 0}
		"laydown"	{set standstate "lying";particle_z 0}
		"sleepa"	{set standstate "lying";particle_z 1}
		"sleepb"	{set standstate "lying";particle_z 1}
		"bed_getup_1" {set standstate "standing";particle_z 0}
		"bed_getup_2" {set standstate "standing";particle_z 0}
		"bed_getup_3" {set standstate "standing";particle_z 0}
		"bed_sleep_1" {set standstate "bedlying";particle_z 1}
		"bed_sleep_2" {set standstate "bedlying";particle_z 1}
		"bed_sleep_3" {set standstate "bedlying";particle_z 1}
		"bed_laydown_1" {set standstate "bedlying";particle_z 1}
		"bed_laydown_1" {set standstate "bedlying";particle_z 1}
		"bed_laydown_1" {set standstate "bedlying";particle_z 1}

		default		{
						set alist [split $anim "_"]
						set type [lindex $alist 0]
						if { $type == "sit" } {
							set categ [lindex $alist 1]
							set subani [lindex $alist 2]
							switch $categ {
								"sleep"		{
												switch $subani {
													"getup"			{set standstate "sitting";particle_z 0}
													"doze"			{set standstate "sittinglying";particle_z 1}
													"fallasleep"	{set standstate "sittinglying";particle_z 0}
												}
											}
								"standup"	{set standstate "standing";particle_z 0}
								"sitdown"	{set standstate "sitting";particle_z 0}
								default		{set standstate "sitting";particle_z 0}
							}
						} else {
							set standstate "standing";particle_z 0
						}
					}
	}
	state_disable this
	action this anim $anim {state_enable this}
	return true
}

proc rotate_toright {} {state_disable this;action this rotate 4.71 {state_enable this}}
proc rotate_toleft {} {state_disable this;action this rotate 1.57 {state_enable this}}
proc rotate_toback {} {state_disable this;action this rotate 3.14 {state_enable this}}
proc rotate_tofront {} {state_disable this;action this rotate 0 {state_enable this}}
proc rotate_toang {ang} {state_disable this;action this rotate $ang {state_enable this}}

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

proc rotate_towards {item} {
	state_disable this;
	action this rotate [expr 1.57+[vector_angle [get_pos this] [get_pos $item]]] {state_enable this}
	return true
}


proc set_idle_anim {} {
	if { [get_gnomeposition this] == 0 } {
			switch [hf2i [random 4]] {
				0 	{set_anim this troll.stehen_warten_a 0 2 ;#set idle anim}
				0 	{set_anim this troll.stehen_warten_b 0 2 ;#set idle anim}
				0 	{set_anim this troll.stehen_warten_c 0 2 ;#set idle anim}
			}
	} else {
		set_anim this troll.stehen_zu_klettern 0 1 ;#set idle anim
	}
}

proc TR_sleep {args {4 8}} {
	set min [lindex $args 0]
	set max [lindex $args 1]
	tasklist_add this "play_anim laydown"
	set rnd [irandom $min [expr $max + 1]]

//	log "[get_objname this] sleeeeeep $min $max -> $rnd"

	for {set i 0} {$i < $rnd} {incr i} {
		if { [irandom 2] == 0 } {
			set ext "a"
		} else {
			set ext "b"
		}
		tasklist_add this "play_anim sleep$ext"
	}
	tasklist_add this "play_anim standup"
}

proc TR_sleep_bed {{floor 1} {min 4} {max 8} {dir back}} {
	tasklist_add this "rotate_to$dir "
	tasklist_add this "play_anim bed_laydown_$floor"
	log "Troll [get_objname this]: Floor = $floor, min = $min, max = $max, dir = $dir"

	set rnd [irandom $min [expr $max + 1]]
	log "Troll [get_objname this]: rnd = $rnd"
	for {set i 0} {$i < $rnd} {incr i} {
		tasklist_add this "play_anim bed_sleep_$floor"
	}
	tasklist_add this "play_anim bed_getup_$floor"

	log "Troll = [get_objname this]: Tasklist = [tasklist_list this]"
}

proc TR_eatndrink {{min 4} {max 8} {dir back}} {
	tasklist_add this "rotate_to$dir "
	tasklist_add this "play_anim sit_sitdown"

	set rnd [irandom $min [expr $max + 1]]
	for {set i 0} {$i < $rnd} {incr i} {
		if { [irandom 2] == 0 } {
			tasklist_add this "play_anim sit_eatndrink_eat"
		} else {
			tasklist_add this "play_anim sit_eatndrink_drink"
			tasklist_add this "play_anim sit_eatndrink_wipe"
		}
	}
	tasklist_add this "play_anim sit_standup"
}

proc TR_gaehnen {} {
	tasklist_add this "play_anim gape"
}

proc TR_jucken {} {
	tasklist_add this "play_anim itch"
}

proc TR_salutieren {} {
	tasklist_add this "play_anim salut"
}

proc get_random_of {str} {
	set rlist [split $str ""]
	set which [irandom [llength $rlist]]
	return [lindex $rlist $which]
}

proc TR_lookaround {{abc "abc"}} {
	tasklist_add this "play_anim troll.stehen_umschauen_[get_random_of abc]"
}

proc TR_scout {} {
	tasklist_add this "play_anim scout"
}

proc TR_wait {{time 2}} {
	set_idle_anim
	wait_time $time
}

proc get_next_sleep {} {
	tasklist_add this "play_anim sleep[get_random_of ab]"
}

proc get_next_sleepbed {} {
	global bedfloor
	tasklist_add this "play_anim bed_sleep_$bedfloor"
}


proc deliver_turn {{round 0}} {
	global next_gambler dran
	set del_list [list]
	set next 0
	set awaycnt 0
	foreach item $next_gambler {
		if { ![obj_valid $item] } {
			lappend del_list $item
			continue
		}
		set opos [get_pos this]
		set ipos [get_pos $item]
		set dist [vector_abs [vector_sub $opos $ipos]]
		if { $dist > 6.2 } {
			//log "next gambler is to far: $dist"
			incr awaycnt
			continue
		}
		set sst [call_method $item get_standstate]
		if { $sst != "sitting" } {
			//log "next gambler isn't sitting!!"
			continue
		}
		set next $item
		break
	}
	if {$next} {
		set dran 0
		call_method $next set_dran $round
	} else {
		//log "no next gambler found!!!"

		if { $awaycnt == [llength next_gambler] } {
			#log "no gambler near!!"
			#***************
		}
	}
	execute_dellist $del_list
}

proc execute_dellist {dl} {
	global next_gambler
	set old $next_gambler
	set next_gambler [list]
	foreach item $old {
		if { [lsearch $dl $item] == -1} {
			lappend next_gambler $item
		}
	}
}

proc get_next_dice {} {
	global next_gambler dran standstate game_actions
	if { $dran } {
		tasklist_add this "play_anim sit_gamble_dice"
		set rnd [irandom 10]
		switch $rnd {
			1 {tasklist_add this "play_anim sit_gamble_win"}
			2 {tasklist_add this "play_anim sit_gamble_lose"}
		}
		tasklist_add this "deliver_turn"
		tasklist_add this "play_anim sit_idle_a"

	} else {
		switch $standstate {
			"sittinglying"	{
								set rnd [irandom 10]
								switch $rnd {
									1 {tasklist_add this "play_anim sit_sleep_getup"}
									default {tasklist_add this "play_anim sit_sleep_doze"}
								}
							}
			"sitting"		{
								set sleepchance [lindex $game_actions 0]
								set eatchance [lindex $game_actions 1]
								set rnd [irandom 100]
								#log "[get_objname this]-ga:'$game_actions'-------$rnd - $sleepchance"
								if { $rnd <= $sleepchance } {
									tasklist_add this "play_anim sit_sleep_fallasleep"
									return
								}
								if { $rnd <= $sleepchance + $eatchance } {
									set rnd [irandom 2]
									switch $rnd {
										0 {
											tasklist_add this "play_anim sit_eatndrink_eat"
											return
										  }
										1 {
											tasklist_add this "play_anim sit_eatndrink_drink"
											tasklist_add this "play_anim sit_eatndrink_wipe"
											return
										  }
									}
								}
								set rnd [irandom 100]
								if { $rnd < 5 } {
									tasklist_add this "play_anim sit_misc_gape"
									return
								} elseif { $rnd < 10 } {
									tasklist_add this "play_anim sit_misc_headshake"
									return
								}
								tasklist_add this "play_anim sit_idle_a"
							}
		}
	}
}

proc get_next_card {} {
    global next_gambler dran standstate game_actions round
#    log "Das ist Kartenspieler Troll [get_ref this] Round = $round"
	if { $dran } {

        if {$round == 10} {
            set rnd [irandom 1 3]
			switch $rnd {
				1 {tasklist_add this "play_anim sit_card_win"}
				2 {tasklist_add this "play_anim sit_card_lose"}
			}
        	set round 0
        }

        if {$round == 0} {
        	tasklist_add this "play_anim sit_card_take"
        	tasklist_add this "play_anim sit_card_mix"
        }
        if {$round == 1} {
        	tasklist_add this "play_anim sit_card_take"
        	tasklist_add this "play_anim sit_card_sort"
        }

        if {$round > 1} {tasklist_add this "play_anim sit_card_play"}

        incr round

		tasklist_add this "deliver_turn $round"
		tasklist_add this "play_anim sit_card_idle"

	} else {
		switch $standstate {
			"sittinglying"	{
								set rnd [irandom 10]
								switch $rnd {
									1 {tasklist_add this "play_anim sit_sleep_getup"}
									default {tasklist_add this "play_anim sit_sleep_doze"}
								}
							}
			"sitting"		{
								set sleepchance [lindex $game_actions 0]
								set eatchance [lindex $game_actions 1]
								set rnd [irandom 100]
								#log "[get_objname this]-ga:'$game_actions'-------$rnd - $sleepchance"
								if { $rnd <= $sleepchance } {
									tasklist_add this "play_anim sit_sleep_fallasleep"
									return
								}
								if { $rnd <= $sleepchance + $eatchance } {
									set rnd [irandom 2]
									switch $rnd {
										0 {
											tasklist_add this "play_anim sit_eatndrink_eat"
											return
										  }
										1 {
											tasklist_add this "play_anim sit_eatndrink_drink"
											tasklist_add this "play_anim sit_eatndrink_wipe"
											return
										  }
									}
								}
								set rnd [irandom 100]
								if { $rnd < 5 } {
									tasklist_add this "play_anim sit_misc_gape"
									return
								} elseif { $rnd < 10 } {
									tasklist_add this "play_anim sit_misc_headshake"
									return
								}
								tasklist_add this "play_anim sit_card_idle"
							}
		}
	}
}

proc get_next_action {} {
	global action_list lastpos
	set index 0
	set actidx -1
	foreach item $action_list {
		set nr [lindex $item 0]
		if { $nr == $lastpos } {
			set actidx $index
		}
		incr index
	}
#	log "aindex: $index"
	set nextact 0
	if { $actidx != -1 } {
		set actchance [lindex [lindex $action_list $actidx] 1]
		if { [llength $actchance] > 0 } {
			set rnd [random 100]
#			log "arnd: $rnd"
#			log "actch: $actchance"
			set ch 0
			foreach item $actchance {
				set ch [expr $ch + [lindex $item 0]]
				#log "chance: $ch"
				if { $rnd < $ch } {
					set nextact [lindex $item 1]
					break
				}
			}
		}
	}
	if { $nextact != 0 && $nextact != "" } {
		set evact "TR_$nextact"
		eval $evact
	}
	return 0
}

proc get_next_pos {} {
	global guard_poslist lastpos pos_walklist
	#log "guard_poslist: '$guard_poslist'"
	#log "pos_walklist: '$pos_walklist'"
	#log "lastpos: '$lastpos'"

	set index 0
	set actidx -1
	foreach item $guard_poslist {
		set nr [lindex $item 0]
		if { $nr == $lastpos } {
			set actidx $index
			break
		}
		incr index
	}
	set nextpos -1
	if { $actidx != -1 } {
		set walkchance [lindex [lindex $pos_walklist $actidx] 1]
		if { [llength $walkchance] > 0 } {
			set rnd [random 100]
			set ch 0
			foreach item $walkchance {
				set ch [expr $ch + [lindex $item 0]]
				if { $rnd < $ch } {
					set nextpos [lindex $item 1]
					break
				}
			}
		}
	}
	if { ![string is integer $nextpos] } {
		return $nextpos
	}
	set index 0
	set actidx -1
	foreach item $guard_poslist {
		set nr [lindex $item 0]
		if { $nr == $nextpos } {
			set actidx $index
		}
		incr index
	}
	if { $actidx != -1 } {
		return [generate_pos $actidx]
	}
	log "[get_objname this] warning: no nextpos found!!!"
	#set rndx [irandom [llength $guard_poslist]]
	return "error"
}

proc generate_pos {idx} {
	global guard_poslist lastpos range_list
	set posl [lindex $guard_poslist $idx]
	set pos [lindex $posl 1]
	set lastpos [lindex $posl 0]
	set rng [lindex [lindex $range_list $idx] 1]
	set newrng [random $rng]
	set ang [random 6.28318]
	set z [expr sin($ang)]
	set x [expr cos($ang)]
	set vec [vector_pack $x 0 $z]
	set vec [vector_mul $vec $rng]
	set pos [vector_add $vec $pos]
	return $pos
}

proc find_info_obj {typ} {
	set iolist [obj_query this "-type info -range 2"]
	foreach item $iolist {
		if { $typ == [lindex [split [get_objname $item] "_"] 1] } {
//			log "[get_objname this]: info_obj found: [get_objname $item]..."
			return $item
		}
	}
//	log "[get_objname this]: info_obj not found!!!"
	return 0
}

proc find_info_obj_pos {typ} {
	set iolist [obj_query this "-type info"]
	set newlist [list]
	foreach item $iolist {
		if { "Pos" == [lindex [split [get_objname $item] "_"] 1] } {
			if { "Troll" == [lindex [split [get_objname $item] "_"] 2] } {
				if { $typ == [call_method $item get_info "name"] } {
//					log "[get_objname this]: info_obj found: [get_objname $item]..."
					lappend newlist $item
				}
			}
		}
	}
	return $newlist
}

proc find_info_obj_alarm {typ} {
	set iolist [obj_query this "-type info -range 100"]
	set newlist [list]
	foreach item $iolist {
		if { "Alarm" == [lindex [split [get_objname $item] "_"] 1] } {
			if { "Troll" == [lindex [split [get_objname $item] "_"] 2] } {
				if { $typ == [call_method $item get_info "name"] } {
					log "[get_objname this]: info_obj found: [get_objname $item]..."
					lappend newlist $item
				}
			}
		}
	}
	return $newlist
}


proc get_posinfo {InfoID} {
	global guard_poslist pos_walklist lastpos range_list action_list action_list2 alarm_poslist
	set alarm_poslist [list]
	set iolist [find_info_obj_alarm $InfoID]
	foreach item $iolist {
		set pos [get_pos $item]
		lappend alarm_poslist $pos
		call_method $item destroy
	}
	set iolist [find_info_obj_pos $InfoID]
	set xlist [list]
	set wxlist [list]
	set range_list [list]
	set guard_poslist [list]
	foreach item $iolist {
		set pos [get_pos $item]
		set nr [lindex [split [get_objname $item] "_"] 3]
		set xnr [call_method $item get_info "nr"]
		if { $xnr != 0 } { set nr $xnr	}
		set poswalk [call_method $item get_info "walk"]
		if { $poswalk == 0 } { set poswalk [list] }
		set range [call_method $item get_info "range"]
		if { $range == 0 } { set range 0.5 }
		set act [call_method $item get_info "actions"]
		if { $act == 0 } { set act [list] }
		set act2 [call_method $item get_info "actions2"]
		if { $act2 == 0 } { set act2 [list] }
		lappend xlist "$nr \{$pos\}"
		lappend wxlist "$nr \{$poswalk\}"
		lappend range_list "$nr $range"
		lappend action_list "$nr \{$act\}"
		lappend action_list2 "$nr \{$act2\}"
		if { [get_info "speier"] == 0 } {
			call_method $item destroy
		}
	}
	set walkorder [get_info "walkorder"]
	lappend walkorder [lindex $walkorder 0]
	if { $walkorder != 0 } {
		set index 0
		foreach item $wxlist {
			set ch 0
			set nr [lindex $item 0]
			set chances [lindex $item 1]
			foreach subitem $chances {
				set ch [expr $ch + [lindex $subitem 0]]
			}
			if { $ch < 100 } {
				set wpos [lsearch $walkorder $nr]
				if { $wpos != -1 } {
					incr wpos
					set wpos [lindex $walkorder $wpos]
					set newchance "[expr 100 - $ch] $wpos"
					lappend chances $newchance
					//set wxlist [lreplace $wxlist $index $index "$nr \{$chances\}"]
					lrep wxlist $index "$nr \{$chances\}"
				}
			}
			incr index
		}
	}
	set pos_walklist $wxlist
	set guard_poslist $xlist
	log [get_objname this]
	log "pos_walklist $wxlist"
	log "guard_poslist $xlist"
	set lastpos [lindex [lindex $xlist 0] 0]
}

proc take_items {} {
	set objlist [obj_query this "-type \{material tool\} -boundingbox \{-0.32 -0.32 -0.64 0.32 0.32 0.64\}"]
	set boxlist [obj_query this "-flagpos boxed -boundingbox \{-0.32 -0.32 -0.64 0.32 0.32 0.64\}"]

	set objlist [lnand 0 [lor $objlist $boxlist]]
	
	//log "TAKEITEMs [get_objname this]: $objlist"

	foreach item $objlist {
		set taken "not"
		set itemclass [get_objclass $item]
		if {[check_method $itemclass oeffnen]} {continue}
		if { $itemclass != "Pilz" }	{
			if { [string range $itemclass 0 5] != "Schatz" || [string length $itemclass] == 10 } {
				inv_add this $item
				set_rotx $item 0.0
				set_rotz $item 0.0
				set_owner $item [get_owner this]
				set taken ""
			}
		}
		//log "[get_objname this]: Item found: [get_objname $item] - was $taken taken"
	}
}

proc get_gambleorder {} {
	global next_gambler dran troll_startroty
	if { $next_gambler == -1 } {
		set trlistx [lnand 0 [obj_query this "-class Troll -range 8"]]
		set trlist [list]
		foreach item $trlistx {
			if { [call_method $item get_occupation] == "dicing" || [call_method $item get_occupation] == "cards"} {
				lappend trlist $item
			}
		}
		if { $trlist != 0 } {
			set next_gambler $trlist
			set gl $trlist
			lappend gl [get_ref this]
			foreach item $trlist {
				set first [lindex $gl 0]
				//set gl [lreplace $gl 0 0]
				lrem gl 0
				lappend gl $first
				call_method $item set_next_gambler $gl
			}
			set dran 1
		}
	}
	//log "[get_objname this]'s gambleorder: $next_gambler $dran"
}

proc troll_init {} {
	global occupation guard_poslist troll_startpos current_weapon weapon_name scan_range troll_startroty standard_wake_time
	global action_state wake_timer game_actions standstate bedfloor current_shield shield_name shield_out
	global troll_texturetype

	// taking items at same Position to Inventory
	take_items

	set occupation [get_info "occupation"]
	set InfoID [get_info "name"]
	set troll_startpos [get_pos this]
	set troll_startroty [get_roty this]

	set troll_texturetype 0
	if { [irandom 2] == 1 } {
		set troll_texturetype 4
	}

	switch $occupation {
		"guard"			{
							get_posinfo $InfoID
							set standard_wake_time 4
							set troll_texturetype 0
						}
		"salut"			{
							get_posinfo $InfoID
							set standard_wake_time 4
							//set troll_texturetype 1
						}
		"dicing"		{
							set standard_wake_time 5
							set sl [get_info "sleep"]
							if { $sl != 0 } {
								//set game_actions [lreplace $game_actions 0 0 $sl]
								lrep game_actions 0 $sl
							}
							set ed [get_info "eatdrink"]
							if { $ed != 0 } {
								//set game_actions [lreplace $game_actions 1 1 $ed]
								lrep game_actions 1 $ed
							}
							set standstate "sitting"
							get_gambleorder
							get_posinfo $InfoID
						}
		"sleep"			{
							set standard_wake_time 8
							set troll_texturetype 2
						}
		"sleepbed"		{
							set bedfloor [get_info "floor"]
							set standard_wake_time 8
							set troll_texturetype 2
						}
		"cards"			{
							set standard_wake_time 5
							set sl [get_info "sleep"]
							if { $sl != 0 } {
								//set game_actions [lreplace $game_actions 0 0 $sl]
								lrep game_actions 0 $sl
							}
							set ed [get_info "eatdrink"]
							if { $ed != 0 } {
								//set game_actions [lreplace $game_actions 1 1 $ed]
								lrep game_actions 1 $ed
							}
							set standstate "sitting"
							get_gambleorder
							get_posinfo $InfoID
						}

		default		{ log "[get_objname this]: invalid occupation: $occupation" }
	}

	set txtvar [get_info texture]
	if { $txtvar != 0 } {
		set troll_texturetype [expr $txtvar - 1]
	}

	set_textureanimation this 0 $troll_texturetype
	set_textureanimation this 1 $troll_texturetype

	set srange [get_info "scanrange"]
	#log "*scan_range: $srange"
	if { $srange != 0 } { set scan_range $srange }
	set wtime [get_info "waketime"]
	#log "*waketime: $wtime"
	if { $wtime != 0 } { set standard_wake_time $wtime }
	set wake_timer $standard_wake_time

	set weapon [get_info "weapon"]
	if { $weapon != 0 } {
		log "+weapon : $weapon"
		sel /obj
		set item [new $weapon]
		set_hoverable $item 0
		set_selectable $item 0
		set id [get_weapon_id $item true]
		//log "set_weapon_class [get_ref this] $id"
		set_weapon_class this $id
		set current_weapon $item
		set weapon_name $weapon
	}

	set shield [get_info "shield"]
	if { $shield != 0 } {
		log "+shield : $shield"
		sel /obj
		set item [new $shield]
		set_hoverable $item 0
		set_selectable $item 0
		set id [get_weapon_id $item true]
		//log "set_shield_class [get_ref this] $id"
		set_shield_class this $id
		set current_shield $item
		set shield_name $shield
	}


	if { $occupation == "guard" || $occupation == "salut" } {
		weapon_takeout
	}

	set action_state "loop"
	return 1
}

proc weapon_takeout {} {
	global weapon_name weapon_out current_weapon current_shield shield_name shield_out
	//log "++++weapon wout:$weapon_out wnam: $weapon_name cwe: $current_weapon"
	if { $current_weapon != 0 } {
		if { $weapon_out == 0 } {
			set wnam "schwert";#[string tolower $weapon_name]
			state_disable this
			action this anim troll.stehen_zu_$wnam "
											link_obj $current_weapon this 0
											if {$current_shield != 0} {link_obj $current_shield this 1}
											set_visibility $current_weapon 1
											if {$current_shield != 0} {set_visibility $current_shield 1}
											state_enable [get_ref this]
											" "
											link_obj $current_weapon this 0
											if {$current_shield != 0} {link_obj $current_shield this 1}
											set_visibility $current_weapon 1
											if {$current_shield != 0} {set_visibility $current_shield 1} "
		}
	}
	set weapon_out 1
	set shield_out 1
	return
}

proc weapon_putin {} {
	global weapon_name weapon_out current_weapon current_shield shield_name shield_out
	if { $current_weapon != 0 } {
		if { $weapon_out != 0 } {
			set wnam "schwert";#[string tolower $weapon_name]
			state_disable this
			action this anim troll.$wnam\_zu_stehen "
											link_obj $current_weapon
											if {$current_shield != 0} {link_obj $current_shield}
											set_visibility $current_weapon 0
											if {$current_shield != 0} {set_visibility $current_shield 0}
											state_enable [get_ref this]
											" "
											link_obj $current_weapon
											if {$current_shield != 0} {link_obj $current_shield}
											set_visibility $current_weapon 0
											if {$current_shield != 0} {set_visibility $current_shield 0} "
		}
	}
	set weapon_out 0
	set shield_out 0
	return
}

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

proc initiate_alarm {who pos} {
	set opos [get_pos this]
	#if { [vector_dist $pos $opos] < 1.6 } {
		set tlist [obj_query this "-class Troll -range 10"]
		if { $tlist != 0 } {
			play_anim squeeze
			foreach item $tlist {
				#log "call_method $item  mstart_attack $who"
				call_method $item  mstart_attack $who
			}
		} else {
			log "initiate_alarm error: no other trolls found !!!"
			return
		}
	#}
}

proc calc_sit_pos {} {
	global troll_startpos troll_startroty
	set ay [expr $troll_startroty + 4.71]
	set v [vector_pack [expr cos($ay)] 0 [expr sin($ay)]]
	set v [vector_mul $v 0.7245]

//	log "[get_objname this]:---calc_sit_pos: $v"

#	set z [expr [lindex $v 2] + 0.5] ;# / 2]
	set z [expr [lindex $v 2]  / 2]
	set v [vector_setz $v $z]

	return [vector_add $troll_startpos $v]
}

proc go_back_to_work {} {
	global troll_startroty troll_startpos occupation bedfloor
	#log "Troll [get_ref this]: GO_BACK_TO_WORK - occupation = $occupation"
	switch $occupation {
		"sleep"		{
						tasklist_add this "weapon_putin"
						tasklist_add this "play_anim discover"
						tasklist_add this "walk_pos_loop"
					}
		"sleepbed"	{
						tasklist_add this "weapon_putin"
						tasklist_add this "play_anim discover"
						tasklist_add this "walk_pos_loop"
					}
		"dicing"	{
						tasklist_add this "weapon_putin"
						tasklist_add this "play_anim discover"
						tasklist_add this "walk_pos_loop"
					}
		"guard"		{
						tasklist_add this "play_anim discover"
						tasklist_add this "walk_pos_loop"
					}
		"salut"		{
						tasklist_add this "play_anim discover"
						tasklist_add this "walk_pos_loop"
					}
		"cards"		{
						tasklist_add this "weapon_putin"
						tasklist_add this "play_anim discover"
						tasklist_add this "walk_pos_loop"
					}
	}
}

proc walk_pos_loop {} {
	global troll_startroty troll_startpos occupation bedfloor

	if {$occupation == "dicing" || $occupation == "cards"} {
		set st_pos [vector_abs [vector_sub [calc_sit_pos] [get_pos this]]]
		#log "Troll [get_ref this]: st_pos = $st_pos"
		if {$st_pos > 1} {
			#log "Troll [get_ref this]: pos this = [get_pos this]         Calc_sit_pos = [calc_sit_pos], start_pos = $troll_startpos"
			tasklist_add this "walk_pos \{[calc_sit_pos]\}"
			tasklist_add this "walk_pos_loop"
			return
		}
	} else {
		set st_pos [vector_abs [vector_sub $troll_startpos [get_pos this]]]
		#log "Troll [get_ref this]: st_pos = $st_pos"
		if {$st_pos > 1} {
			#log "Troll [get_ref this]: pos this = [get_pos this]         start_pos = $troll_startpos"
			tasklist_add this "walk_pos \{$troll_startpos\}"
			tasklist_add this "walk_pos_loop"

			return
		}
	}
	switch $occupation {
		"sleep"	{
					tasklist_add this "rotate_toang $troll_startroty"
					tasklist_add this "play_anim laydown"
					tasklist_add this "set action_state loop"
				}
		"guard"	{
					tasklist_add this "set action_state loop"
				}
		"sleepbed"	{
						tasklist_add this "rotate_toang $troll_startroty"
						tasklist_add this "play_anim bed_laydown_$bedfloor"
						tasklist_add this "set action_state loop"
					}
		"dicing"	{
						tasklist_add this "rotate_toang $troll_startroty"
						tasklist_add this "play_anim sit_sitdown"
						tasklist_add this "set action_state loop"
					}
		"cards"		{
						tasklist_add this "rotate_toang $troll_startroty"
						tasklist_add this "play_anim sit_sitdown"
						tasklist_add this "set action_state loop"
					}
		"salut"		{
						tasklist_add this "rotate_toang $troll_startroty"
//						tasklist_add this "play_anim sit_sitdown"
						tasklist_add this "set action_state loop"
					}
	}
}

proc get_up {} {
	global standstate bedfloor
	switch $standstate {
		"standing"	{}
		"lying"		{play_anim standup}
		"bedlying"	"play_anim bed_getup_$bedfloor"
		"sitting"	{play_anim sit_standup}
	}
}

proc start_attack {who} {
	global alarmed standing action_state
	set action_state "ontheway"
	set pos [get_pos $who]
	set ownpos [get_pos this]
	set vdif [vector_sub $pos $ownpos]
	set length [vector_abs $vdif]
	set nlength [expr $length - 2]
	set prc [expr $nlength / $length]
	set ndif [vector_mul $vdif $prc]
	set pos [vector_add $ownpos $ndif]

	tasklist_add this "get_up"
	tasklist_add this "run_pos_obj \{$pos\} $who"
	tasklist_add this "find_enemy 18"
}


proc troll_alert {} {
	global alarmable alarm_poslist scan_range action_state
	set bdist 1000
	set bpos 0
	set ownpos [get_pos this]
	foreach item $alarm_poslist {
		set dist [vector_abs [vector_sub $ownpos $item]]
		if { $dist < $bdist } {
			set bdist $dist
			set bpos $item
		}
	}
	log "trollalert!: alarm_poslist '$alarm_poslist' bpos '$bpos'"
	if { $bpos == 0 } { find_enemy ; return }		;#	no alarmpos found -> directly attack

	set fzwerg [obj_query this "-range $scan_range -class [get_enemy_classes] -limit 1"]
	if { $fzwerg == 0 } { log "[get_objname this] : alert-error 1"; return }

	set alarmable_troll_found 0
	set tlist [obj_query this "-class Troll -pos \{$bpos\} -range 6"]
	if { $tlist != 0 } {
		foreach item  $tlist {
			set sts [call_method $item get_standstate]
			log "- $sts"
			if { $sts == "lying" || $sts == "sitting" || $sts == "bedlying" } {
				set alarmable_troll_found 1
				break
			}
		}
	}
	if { $alarmable_troll_found == 0 } { find_enemy ; return }		;#	no alarmable troll found -> directly attack

	set alarmable 0
	action this wait 0.1 	;#break actions
	state_enable this
	tasklist_clear this

	set action_state "ontheway"

	tasklist_add this "rotate_towards $fzwerg"
	tasklist_add this "play_anim fright"
	if { $bdist < 6 } {
		tasklist_add this "play_anim alarmstart"
		tasklist_add this "play_anim alarmloop[get_random_of abc]   "
		tasklist_add this "play_anim alarmstop"
		tasklist_add this "initiate_alarm $fzwerg \{$bpos\}"
		tasklist_add this "start_attack $fzwerg"
	} else {
		tasklist_add this "run_pos_obj \{$bpos\} $fzwerg"
		tasklist_add this "initiate_alarm $fzwerg \{$bpos\}"
		tasklist_add this "start_attack $fzwerg"
	}
}

proc get_act_scanrange {} {
	global standstate scan_range action_state
	if { $action_state == "wait" } {
		return 9
	}
	switch $standstate {
		"standing" 	{return $scan_range}
		"lying"		{return 2}
		"sitting"	{return [expr $scan_range * 0.8]}
		"sittinglying"	{return 2}
		"bedlying"	{return 3.8}
		default		{return $scan_range}
	}
}

proc change_to_wait {} {
	global action_state wake_timer standard_wake_time
	set action_state "wait"
	set wake_timer $standard_wake_time
//	log "[get_ref this] change_to_wait:      as: $action_state wt: $wake_timer"
}

proc notify_gamblers {} {
	set tlist [obj_query this "-class Troll -range 8"]
	if { $tlist != 0 } {
		foreach item  $tlist {
			set ast [call_method $item get_actionstate]
			if {$ast == "loop"} {
				tasklist_clear $item
				#tasklist_add $item "play_anim sit_standup"
				tasklist_add $item "change_to_wait"
			}
		}
	}
}

proc scan_for_enemy {} {
	global occupation action_state
	set rng [get_act_scanrange]
	set fzwerg [obj_query this "-range $rng -class [get_enemy_classes] -limit 1"]
	#log "[get_objname this] scan: rng: $rng found:($fzwerg)"

	if { $fzwerg != 0 } {
		switch $action_state {
			"ontheway"	{}
			"fight"	{}
			"wait"		{find_enemy}
			"loop"		{
							if { $occupation == "guard" || $occupation == "salut" } {
								troll_alert
							} else {
								notify_gamblers
								find_enemy
							}
						}
		}
	}
}

proc find_enemy {{rng "default"}} {
	if {[get_max_fow this]<10} {return 0}
	global attack_behaviour attack_item approach
	set rng [get_act_scanrange]

	log "-->[get_objname this] find_enemy rng:$rng"

	set attack_item 0
	set fzwerg_list [obj_query this "-range $rng -class [get_enemy_classes]"]
	if { $fzwerg_list != 0 } {
		foreach fzwerg $fzwerg_list {
			set attack_behaviour "offensive"
			set attack_item $fzwerg
			if { [state_get $attack_item] == "fight_dispatch"  } {
				#continue
			}
			if { [get_attrib $attack_item atr_Hitpoints] < 0.01 } {
				continue
			}
			set pos [get_attack_pos this $attack_item]
			if { [lindex $pos 0] == 0 | [lindex $pos 0] == -1 } { continue }

			set obj [lnand "0 $attack_item" [obj_query this "-class \{Troll [string map {"\}" "" "\{" ""} [get_enemy_classes]]\} -pos \{$pos\} -range 1.0"]]
			if { [llength $obj] != 0 } {
				continue
			}

			set alarmed 1
			action this wait 0.1 	;#break actions
			state_enable this
			tasklist_clear this
			set action_state "fight"
			get_up
			set attack_behaviour "offensive"
			set approach 1
			fight_startfight
			return 1
		}
	}
	tasklist_add this "change_to_wait"
	return 0
}

proc check_occupation {} {
	global occupation
	if { [string first $occupation "guard-sleep-dicing-sleepbed-cards-salut"] != -1 } {return 1}
	return 0
}

proc get_next_wait {} {
	global wake_timer action_state

	set tl [lnand 0 [obj_query this -class Troll -range 6]]
	foreach item $tl {
		if { [state_get $item] == "fight_dispatch" } {
			tasklist_add this "play_anim fidle[irandom 4]"
			return
		}
	}

	//if { $wake_timer > 0 } {
		incr wake_timer -1
		if { $wake_timer < 1 } {
			go_back_to_work
			return
		}
	//}
	tasklist_add this "walk_random [expr 2 + [hf2i [random 2]]]"
}

proc handle_guard {} {
	global action_state
	switch $action_state {
		"loop"	{
					set act [get_next_action]
					set pos [get_next_pos]
					if { [lindex $pos 0] == "random" } {
						tasklist_add this "walk_random [expr [lindex $pos 1] + [irandom 1]]"
					}
					if { [lindex $pos 0] == "error" } {
						tasklist_add this "error_freeze"
					}
					if { $pos != "error" } {
						tasklist_add this "walk_pos \{$pos\}"
					} else {
						tasklist_add this "walk_random [expr 6 + [irandom 1]]"
					}
				}
		"wait"	{
					get_next_wait
				}
	}
}

proc handle_salut {} {
	global action_state
	switch $action_state {
		"loop"	{
						set anim "salutewait[get_random_of abc]"
						tasklist_add this "play_anim $anim"
				}
		"wait"	{
					get_next_wait
				}
	}
}


proc handle_sleep {} {
	global action_state
	switch $action_state {
		"loop"	{
					get_next_sleep
				}
		"wait"	{
					get_next_wait
				}
	}
}

proc handle_sleepbed {} {
	global action_state
	switch $action_state {
		"loop"	{
					get_next_sleepbed
				}
		"wait"	{
					get_next_wait
				}
	}
}

proc handle_dicing {} {
	global action_state
	switch $action_state {
		"loop"	{
					get_next_dice
				}
		"wait"	{
					get_next_wait
				}
	}
}

proc handle_cards {} {
     global action_state
	switch $action_state {
		"loop"	{
					get_next_card
				}
		"wait"	{
					get_next_wait
				}
	}
}

proc weapon_shield_takeout {{bAnim 0}} {
	global weapon_name weapon_out current_weapon
	weapon_takeout
	return 1
}

proc run_away {{pos "auto"}} {
	if { $pos == "auto" } {
		set pos [calc_escape_pos]
	}
	if { $pos == 0 } { return }
	state_disable this
	set pos [vector_fix $pos]
	set is_escaping 1
	if { [lindex $pos 0] == -1 } {
		action this walk "-randompath [irandom 4 10] -animsets 1 -speedtype 1 " { set is_escaping 0 ; state_enable this ; run_away } { set is_escaping 0 ; tasklist_add this "run_away" }
	} else {
		action this walk "-target \{$pos\} -animsets 1 -speedtype 1 " { set is_escaping 0 ; state_enable this ; run_away } { set is_escaping 0 ; tasklist_add this "run_away" }
	}
	return true
}

proc get_enemy_classes {} {
	return "\{Zwerg Wuker Spinne Holztuer Steintuer Metalltuer Kristalltuer\}"
}

proc beamto_inv {item} {
	inv_add this $item
	set_rotx $item 0.0
	set_rotz $item 0.0
	set_owner $item [get_owner this]
	return true
}

