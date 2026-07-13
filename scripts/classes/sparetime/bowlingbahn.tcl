//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class Bowlen food material 0 {} {}

def_class Bowlingbahn wood production 3 {} {


	method prod_item_actions item {
		return [list]
	}

	method get_inactive {} { return 0 }
	method machanim {which} {
		if {$which==0} {
			set_anim this bowlingbahn.anim_pudel 0 1
		} elseif {$which==1} {
			set_anim this bowlingbahn.anim_strike 0 1
		} else {
			set_anim this bowlingbahn.ohne_kugel 0 0
		}
	}
	method ask_for_seat {} {
		return [ask_for_free_seat]
	}
	method ask_for_reserve {} {
		for {set i $prod_guest_seats} {$i<$prod_guest_waits} {incr i} {
			if {[prod_guest guestget this $i]==0} {
				return $i
			}
		}
		return 0
	}
	method reserve_seat {ref} {
		for {set i $prod_guest_seats} {$i<$prod_guest_waits} {incr i} {
			if {[prod_guest guestget this $i]==0} {
				prod_guest guestset this $i $ref
				return $i
			}
		}
		return 0
	}
	method default_link {} {return $prod_guest_seats}
	method ask_seat_cnt {} {
		set retval -1
		for {set i 0} {$i<$prod_guest_waits} {incr i} {
			if {[prod_guest guestget this $i]==0} {
				incr retval
			}
		}
		return $retval
	}
	method get_next_action {gnome} {
		return [get_next_guest_action $gnome]
	}
	method remove_from_guestlist {gnome} {
		global guests gueststates wincount guesttimer
		log "leaving Bowling: [get_objname $gnome] ($gnome) ($guests) ($gueststates)"
		if {[set id [lsearch $guests $gnome]]==-1} {return ""}
		//if {[lindex $gueststates $id]>11} {set ret {"play_anim sitchairbeerstop"}}
		//if {[lindex $gueststates $id]>9} {lappend ret "play_anim standup_chair"} {set ret ""}
		set ret ""
		if {[lindex $wincount $id]} {lappend ret "sparetime_place_finished"}
		if {[lindex $guesttimer $id]>30} {lappend ret "sparetime_place_fail bfl"}
		remove_guest $id
		return $ret
	}
	method guest_stateset {id val} {set_guest_state $id $val}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim bowlingbahn.standard
	class_flagoffset 3.0 3.7

	obj_init {
		set_anim this bowlingbahn.standard 0 $ANIM_LOOP   ;#bowlingbahn animation fehlt noch
		set_collision this 1

		call scripts/misc/genericprod.tcl

		prod_guest addlink this 1
		prod_guest addlink this 2
		prod_guest addlink this 3
		set prod_guest_seats 3
		prod_guest addlink this 6
		prod_guest addlink this 7
		set prod_guest_waits 5

		set myref [get_ref this]
		set guests {0 0 0 0 0}
		set gueststates {0 0 0 0 0}
		set guesttimer {0 0 0 0 0}
		set wincount {0 0 0 0 0}
		set current_bowler 0
		
		set build_dummys [list 24 25 26 27 28 29 30]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechtsholz unten_linksholz unten_rechtsholz unten_linksholz oben_rechtsholz unten_rechtsholz unten_rechtsholz}
		set damage_dummys {24 30}	

		proc ask_for_free_seat {{waiter 0}} {
			global gueststates prod_guest_seats prod_guest_waits
			if {[prod_guest guestfree this]/$prod_guest_seats} {return 0} {
				if {!$waiter&&[lcount [lrange $gueststates $prod_guest_seats [expr {$prod_guest_waits-1}]] 0]!=$prod_guest_waits-$prod_guest_seats} {
					return 0
				} else {
					return 1
				}
			}
		}
		proc get_random_seat {} {
			global prod_guest_seats
			set rlst {}
			for {set i 0} {$i<$prod_guest_seats} {incr i} {
				if {[prod_guest guestget this $i]==0} {
					lappend rlst $i
				}
			}
			return [lindex $rlst [irandom [llength $rlst]]]
		}
		proc add_guest {ref} {
			global guests prod_guest_waits
			for {set i 0} {$i<$prod_guest_waits} {incr i} {
				if {[prod_guest guestget this $i]==$ref} {
					break
				}
			}
			if {$i==$prod_guest_waits} {
				log "WARNING: $ref [get_objname $ref] is not in Bahn"
				for {set i 0} {$i<$prod_guest_waits} {incr i} {
					log "$i: [prod_guest guestget this $i]"
				}
				return -1
			}
			lrep guests $i $ref
			return $i
		}
		proc set_guest_state {id state} {
			global gueststates
			lrep gueststates $id $state
			reset_guest_timer $id
		}
		proc lock_for_guest {id} {
			global guests myref
			lrep guests $id $myref
			prod_guest guestset this $id $myref
		}
		proc incr_guest_timer {id} {
			global guesttimer
			set timer [lindex $guesttimer $id]
			lrep guesttimer $id [expr $timer+1]
		}
		proc incr_win_count {id} {
			global wincount
			set timer [lindex $wincount $id]
			lrep wincount $id [expr $timer+1]
		}
		proc reset_guest_timer {id} {
			global guesttimer
			lrep guesttimer $id 0
		}
		proc remove_guest {id} {
			global guests gueststates guesttimer wincount current_bowler
			if {[lindex $guests $id]==$current_bowler} {set current_bowler 0}
			lrep guests $id 0
			lrep gueststates $id 0
			lrep guesttimer $id 0
			lrep wincount $id 0
		}
		proc get_next_guest_action {gnome} {
			global guests gueststates guesttimer myref wincount
			global prod_guest_seats prod_guest_waits current_bowler
			set rlst [list]
			set gid [lsearch $guests $gnome]
			if {$gid==-1} {set gid [add_guest $gnome]}
			if {$gid==-1} {return "sparetime_place_end"}
			set gst [lindex $gueststates $gid]
			set gti [lindex $guesttimer $gid]
			if {$gti>30||[lindex $wincount $gid]>2} {return {"sparetime_place_end"}}
			set gdy [prod_guest getlink this $gid]
			switch $gst {
				0 {
					if {$gid>2} {
						set_guest_state $gid 1
					} else {
						set_guest_state $gid 2
					}
				}
				1 {
					if {[ask_for_free_seat 1]} {
						set i [get_random_seat]
						lappend rlst "sparetime_take_seat $myref $i 2"
						lock_for_guest $i
						set_guest_state $gid 2
					} else {
						lappend rlst "rotate_toangle [random 1.6 2.5]"
						lappend rlst "play_anim scout"
						lappend rlst "sparetime_idle_loop"
					}
				}
				2 {
					if {$current_bowler} {
						set rmood 0.0
						if {$current_bowler==$gnome} {
							set current_bowler 0
							reset_guest_timer $gid
							set ranim leftright
						} else {
							if {[lsearch [tasklist_list $current_bowler] "play_anim bowllose"]/2==0} {
								set ranim boo
								set rmood 0.02
							} elseif {[lsearch [tasklist_list $current_bowler] "play_anim bowlwin"]/2==0} {
								set ranim [lindex {applaud cheer} [irandom 2]]
								set rmood 0.02
							} else {
								set ranim leftright
							}
						}
						lappend rlst "rotate_toback"
						lappend rlst "sparetime_filler_loop"
						lappend rlst "sparetime_place_relief $ranim $rmood"
						lappend rlst "sparetime_filler_loop"
					} else {
						set mostwait 0
						foreach id [lnand $gid {0 1 2}] {
							set mostwait [hmax $mostwait [lindex $guesttimer $id]]
						}
						if {$gti>$mostwait} {
							set current_bowler $gnome
							if {rand()>[get_attrib $gnome exp_Kampf]+[get_attrib $gnome exp_F_Ballistic]+0.4} {
								set win 0
							} else {
								set win 1
								incr_win_count $gid
							}
							lappend rlst "walk_dummy $myref 0"
							lappend rlst "rotate_toright"
							lappend rlst "play_anim bowla"
							lappend rlst "call_method $myref machanim 2;play_anim bowlb"
							lappend rlst "call_method $myref machanim $win;play_anim bowlc"
							set rmood [expr {0.05+$win*0.25}]
							lappend rlst "sparetime_place_relief [lindex {bowllose bowlwin} $win] $rmood"
							lappend rlst "walk_dummy $myref $gdy"
						}
					}
				}
			}
			incr_guest_timer $gid
			return $rlst
		}
		sparetime this announce fun

	}

	obj_exit {
		sparetime this disannounce
	}

}

