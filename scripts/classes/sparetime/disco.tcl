//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class _Auflegen service material 0 {} {}

def_class Disco wood production 4 {} {

	method prod_item_actions {item} {
		global music current_worker
//		log "disco-action"
		set rlst [list]

		set exper [prod_getgnomeexper $current_worker [call_method this prod_item_exp_infl $item]]
		set exp_incrs [call_method this prod_item_exp_incr $item]
		set music [decide_music $exper]
		lappend rlst "prod_goworkdummy 14"
		lappend rlst "prod_machineanim disco.anim loop"
		lappend rlst "prod_turnfront"
		lappend rlst "prod_anim dja"
		for {set i 0} {$i<10} {incr i} {
			if {rand()<0.3} {
				lappend rlst "prod_anim djc"
			} else {
				lappend rlst "prod_anim djhigh"
			}
			lappend rlst "prod_exp $exp_incrs 0.1"
		}

		return $rlst
	}

	method get_inactive {} {
		if {[get_prod_slot_cnt this _Auflegen]} {
			return 0
		} else {
			return 1
		}
	}
	method ask_for_seat {} {
		if {[prod_guest guestfree this]==-1} {return 0} {return 1}
	}
	method get_random_seat {} {
		return [get_random_seat]
	}
	method ask_for_reserve {} {
		return 0
	}
	method default_link {} {return $prod_guest_seats}
	method ask_seat_cnt {} {
		set retval -1
		for {set i 0} {$i<$prod_guest_seats} {incr i} {
			if {[prod_guest guestget this $i]==0} {
				incr retval
			}
		}
		return $retval
	}
	method get_next_action {gnome} {
		return [get_next_action $gnome]
	}
	method remove_from_guestlist {gnome} {
		global guests gueststates guesttimer dancecount
		log "leaving Disco: [get_objname $gnome] ($gnome) ($guests) ($gueststates)"
		if {[set id [lsearch $guests $gnome]]==-1} {return ""}
		set gst [lindex $gueststates $id]
		set ret ""
		if {[lindex $dancecount $id]>1} {
			lappend ret "sparetime_place_finished"
		} elseif {[lindex $guesttimer $id]>15} {
			if {!$current_worker} {
				lappend ret "sparetime_place_fail dns"
			}
		}
		if {$gst==2} {lappend ret {"play_anim standup_chair"}}
		if {$gst==3} {lappend ret {"play_anim lean_stop"}}
		remove_guest $id
		return $ret
	}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim disco.standard
	class_flagoffset 2.8 4.2

	obj_init {
		set_anim this disco.standard 0 $ANIM_LOOP	;# animation fehlt noch
		call scripts/misc/genericprod.tcl
		set_energyclass this $tttenergyclass_Disco
		set_energyconsumption this $tttenergycons_Disco
		set_prod_switchmode this 1
		set_collision this 1

		prod_guest addlink this 9
		prod_guest addlink this 10
		prod_guest addlink this 11
		prod_guest addlink this 12
		prod_guest addlink this 13
		set prod_guest_seats 5
		set sitangles {2.32 5.37 5.82 3.01 1.47}
		set leandummies {3 5 6}
		set usedlean {}
		set leanangles {5.85 3 2.9}
		set dancedummies {1 7 4 5 2 3 6}
		set useddance {}
		set leaners {}
		set dancers {}

		set music 0
		set myref [get_ref this]
		set guests {0 0 0 0 0}
		set gueststates {0 0 0 0 0}
		set guesttimer {0 0 0 0 0}
		set dancecount {0 0 0 0 0}

		set build_dummys [list 16 17 18 19 20 21 22 23]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechtsmetall oben_rechtsmetall unten_linksmetall unten_linksmetall oben_rechtsmetall unten_linksmetall unten_rechtsmetall unten_rechtsmetall}
		set damage_dummys {24 30}

		proc decide_music {exper} {
			set rnd [irandom 1000]
			set r 0
			for {set i 0} {$i<[string length $rnd]} {incr i} {
				incr r [string index $rnd $i]
			}
			set r [expr {int($exper*3.0+$r*0.2-3.0)}]
			set r [hmax 0 [hmin $r 2]]
			log "Music: $r ($exper)"
			return $r
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
		proc set_guest_state {id state} {
			global gueststates
			lrep gueststates $id $state
			reset_guest_timer $id
		}
		proc incr_guest_timer {id} {
			global guesttimer
			set timer [lindex $guesttimer $id]
			lrep guesttimer $id [expr $timer+1]
		}
		proc incr_dance_count {id} {
			global dancecount
			set timer [lindex $dancecount $id]
			lrep dancecount $id [expr $timer+1]
		}
		proc reset_guest_timer {id} {
			global guesttimer
			lrep guesttimer $id 0
		}
		proc add_guest {ref} {
			global guests prod_guest_seats
			for {set i 0} {$i<$prod_guest_seats} {incr i} {
				if {[prod_guest guestget this $i]==$ref} {
					break
				}
			}
			if {$i==$prod_guest_seats} {
				log "WARNING: $ref [get_objname $ref] is not in Disco"
				for {set i 0} {$i<$prod_guest_seats} {incr i} {
					log "$i: [prod_guest guestget this $i]"
				}
				return -1
			}
			lrep guests $i $ref
			return $i
		}
		proc remove_guest {id} {
			global guests gueststates guesttimer dancecount
			global dancers useddance leaners usedlean
			set ddx [lsearch $dancers $id]
			if {$ddx!=-1} {
				lrem dancers $ddx
				lrem useddance $ddx
			}
			set ldx [lsearch $leaners $id]
			if {$ldx!=-1} {
				lrem leaners $ldx
				lrem usedlean $ldx
			}
			lrep guests $id 0
			lrep gueststates $id 0
			lrep guesttimer $id 0
		}
		proc decide_dance {current gid} {
			global current_worker music
			global leandummies usedlean dancedummies useddance leaners dancers
			if {$current==3} {
				set leanpossible 1
				set ldx [lsearch $leaners $gid]
				set ld [lindex $leaners $ldx]
			} elseif {$current==5} {
				set leanpossible 1
				set ddx [lsearch $dancers $gid]
				set ld [lindex $dancers $ddx]
			} else {
				set lds [lnand $usedlean $leandummies]
				if {$lds==""} {set leanpossible 0} {set leanpossible 1}
				set ldx [irandom [llength $lds]]
				set ld [lindex $lds $ldx]
			}
			if {$current_worker&&rand()<0.4} {
				set lc 1.11
				set dc 1.11
			} elseif {$leanpossible} {
				set lc [lindex {0.6 0.3 0.1} $music]
				set dc [lindex {0.8 0.5 0.2} $music]
			} else {
				set lc 1.11
				set dc [lindex {0.7 0.5 0.3} $music]
			}
			if {$current==2} {fincr dc 0.1}
			if {$current==3} {fincr lc -0.1;fincr dc -0.1}
			if {$current>3} {fincr lc 0.1}
			set rnd [expr {rand()}]
			if {$rnd>$dc} {
				if {$current>3} {return $current}
				if {$current==3} {
					lrem leaners $ldx
					lrem usedlean $ldx
					set dd $ld
					set nw 5
				} else {
					set dds [lnand $useddance $dancedummies]
					set ddx [irandom [llength $lds]]
					set dd [lindex $dds $ddx]
					set nw 4
				}
				lappend dancers $gid
				lappend useddance $dd
				return [list $nw $dd]
			} elseif {$rnd>$lc} {
				if {$current==3} {return 3}
				if {$current>3} {
					if {$current==4} {
						set ddx [lsearch $dancers $gid]
					}
					lrem dancers $ddx
					lrem useddance $ddx
				}
				lappend leaners $gid
				lappend usedlean $ld
				set ldx [lsearch $leaners $gid]
				return [list 3 $ldx]
			} else {
				if {$current==2} {return 2}
				if {$current==3} {
					lrem leaners $ldx
					lrem usedlean $ldx
				} elseif {$current>3} {
					if {$current==4} {
						set ddx [lsearch $dancers $gid]
					}
					lrem dancers $ddx
					lrem useddance $ddx
				}
				return 2
			}
		}
		proc get_next_action {gnome} {
			global guests gueststates guesttimer myref dancecount
			global prod_guest_seats current_worker music
			global sitangles leanangles leandummies
			set rlst [list]
			set gid [lsearch $guests $gnome]
			if {$gid==-1} {set gid [add_guest $gnome]}
			if {$gid==-1} {return "sparetime_place_end"}
			set gst [lindex $gueststates $gid]
			set gti [lindex $guesttimer $gid]
			set gdc [lindex $dancecount $gid]
			if {$gti>15||$gdc>5} {return {"sparetime_place_end"}}
			if {$gst==0} {
				if {$current_worker} {
					lappend rlst "rotate_towards $current_worker"
				}
				set_guest_state $gid 1
			} else {
				if {$gst==1||$gti>3&&rand()<0.5} {
					set ret [decide_dance $gst $gid]
					set ns [lindex $ret 0]
					//log "[get_objname $gnome] ($gnome) decides: $gst -> $ns"
					global dancers useddance leaners usedlean
					//log "($dancers) ($useddance) ($leaners) ($usedlean)"
				} else {
					set ns $gst
				}
				if {$ns!=$gst} {
					switch $gst {
						2 {
							lappend rlst "play_anim standup_chair"
						}
						3 {
							lappend rlst "play_anim leanstop"
						}
					}
					switch $ns {
						2 {
							set du [prod_guest getlink this $gid]
							lappend rlst "walk_dummy $myref $du"
							lappend rlst "rotate_toangle [lindex $sitangles $gid]"
							lappend rlst "play_anim sitdown_chair"
						}
						3 {
							set lid [lindex $ret 1]
							if {$gst!=5} {
								lappend rlst "walk_dummy $myref [lindex $leandummies $lid]"
								lappend rlst "rotate_toangle [lindex $leanangles $lid]"
							}
							lappend rlst "play_anim leanstart"
						}
						4 {
							set du [lindex $ret 1]
							lappend rlst "walk_dummy $myref $du"
							lappend rlst "rotate_towards $myref"
						}
						5 {
							lappend rlst "rotate_towards $myref"
						}
					}
					set_guest_state $gid $ns
				} else {
					switch $gst {
						1 {
							lappend rlst "rotate_toangle [random 6.2]"
						}
						2 {
							lappend rlst "sparetime_place_relief sitchairloop 0.002"
							lappend rlst "sparetime_place_relief sitchairloop 0.002"
							lappend rlst "sparetime_place_relief sitchairloop 0.002"
							lappend rlst "play_anim sitchairbore"
						}
						3 {
							lappend rlst "sparetime_place_relief leanloop 0.01"
							lappend rlst "sparetime_place_relief leanloop 0.01"
						}
						default {
							set crot [get_roty $gnome]
							set nrot ""
							set glist [obj_query $gnome -class Zwerg -owner own -boundingbox {-2 -6 -1 2 6 1}]
							if {$glist!=0&&rand()<0.3} {
								set gn [lindex $glist [irandom [llength $glist]]]
								set angle [get_anglexz [get_pos $gnome] [get_pos $gn]]
								set diff [expr {$angle-$crot}]
								if {$diff<-3.14} {fincr diff 6.28} {if {$diff>3.14} {fincr diff -6.28}}
								if {abs($diff)<0.6} {
									set nrot $angle
								}
							}
							if {$nrot==""} {
								set diff [random 0.6 1.3]
								if {rand()<0.5} {set diff -$diff}
								set nrot [expr {$crot+$diff}]
							}
							if {$diff<0} {set dir -0.2} {set dir 0.2}
							set stepcnt [expr {int($diff/$dir)}]
							set moodgain [expr {0.2/($stepcnt+1)}]
							for {set i 0} {$i<=$stepcnt} {incr i} {
								lappend rlst "set_roty this $crot;sparetime_place_relief discoc $moodgain"
								fincr crot $dir
							}
							incr_dance_count $gid
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

