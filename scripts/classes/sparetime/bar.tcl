call scripts/misc/utility.tcl

def_class Barbetrieb service material 0 {} {}

def_class Bar service production 2 {} {

	class_fightdist 1.8

	method prod_item_actions item {
		return [get_next_chief_action]
//		log "bar-action"
		set rlst [list]
		set order [prod_guest nextorder this]
		set bier [inv_find this Bier]
		if { $bier == -1 }  {return "\{prod_anim impatient\}" }
		if { $order == -1 } {return "\{prod_anim impatient\}" }
		set dummy [prod_guest getlink this $order]
		set guest [prod_guest guestget this $order]
		set roty [expr [vector_unpacky [get_linkrot this $dummy]] + 1.57]

		lappend rlst "prod_goworkdummy 0"
		lappend rlst "prod_turnfront"
		lappend rlst "prod_anim barkeeper"
		lappend rlst "prod_consume_from_workplace Bier"
		lappend rlst "prod_turnback"
		lappend rlst "prod_goworkdummy $dummy"
		lappend rlst "rotate_toangle $roty"
		lappend rlst "prod_anim_exp bend \{\} 1 1 {{{exp_Service 0.01}}} 1.0"
		lappend rlst "call_method $guest sparetime_affect 1"

		prod_guest remorder this $order
		return $rlst
	}


	method get_inactive {} { if {[get_prod_slot_cnt this Barbetrieb]} {return 0} {return 1} }
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
		if {![get_prod_slot_cnt this Barbetrieb]} {return 0}
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
		global guests gueststates guestbeers current_worker
		log "leaving Bar: [get_objname $gnome] ($gnome) ($guests) ($gueststates)"
		if {[set id [lsearch $guests $gnome]]==-1} {return ""}
		set ret ""
		if {[lindex $guestbeers $id]>2} {
			lappend ret "sparetime_place_finished"
		} else {
			if {[lindex $guesttimer $id]>10} {
				if {[get_prod_materialneed this]} {
					lappend ret "sparetime_place_fail pnb"
				} elseif {$current_worker} {
					if {[lcount $guests 0]<4} {
						lappend ret "sparetime_place_fail pfl"
					}
				} else {
					lappend ret "sparetime_place_fail pns"
				}
			}
		}
		if {[lindex $gueststates $id]>11} {lappend ret "play_anim sitchairbeerstop"}
		if {[lindex $gueststates $id]>9} {lappend ret "play_anim standup_chair"}
		remove_guest $id
		return $ret
	}
	method guest_stateset {id val} {
		if {[set who [lindex $guests $id]]} {
			tasklist_clear $who
			set_guest_state $id $val
		}
	}
	method removeorder {id} {prod_guest resetorder this $id}
	
	method incr_beer_count {id} {
		incr_guest_beers $id
	}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	def_event evt_timer0

	handle_event evt_timer0 {
		if { ![get_boxed this] } {
			bar_betrieb
		}
	}

	class_defaultanim bar.standard
	class_flagoffset 1.4 0.0

	obj_init {
		set_anim this bar.standard 0 $ANIM_LOOP
		set_inventoryslotuse this 1
		set_energyconsumption this 0
		set_collision this 1

		call scripts/misc/genericprod.tcl
		timer_event this evt_timer0 -repeat -1 -interval 3 -userid 0

		set_prod_materialneed this 1
		set_prod_switchmode this 1

		set myref [get_ref this]
		set dummyposlist {{0.25 -0.8 -1.55 3.0} {0.7 -0.8 -0.5 0.5} {-0.15 -0.8 -1.5 5.7} \
		{-0.7 -0.8 -0.5 1.5} {-0.15 -0.63 -0.5 1.4} {0.15 -0.63 -0.6 0.0}}

		prod_guest addlink this 4
		prod_guest addlink this 5
		prod_guest addlink this 6
		prod_guest addlink this 7
		set prod_guest_seats 4
		prod_guest addlink this 3
		prod_guest addlink this 2
		prod_guest addlink this 1
		set prod_guest_waits 7
		set freeseats 15
		set looktochief 0

		set guests {0 0 0 0 0 0 0}
		set gueststates {0 0 0 0 0 0 0}
		set guesttimer {0 0 0 0 0 0 0}
		set guestbeers {0 0 0 0 0 0 0}

		set build_dummys [list 24 25 26 27 28 29]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechtsholz oben_rechtsholz oben_linksholz unten_rechtsholz unten_rechtsholz unten_rechtsholz}
		set damage_dummys {20 27}

		proc ask_for_free_seat {{waiter 0}} {
			global gueststates prod_guest_seats prod_guest_waits
			if {[prod_guest guestfree this]/$prod_guest_seats} {return 0} {
				if {!$waiter&&[lcount [lrange $gueststates $prod_guest_seats $prod_guest_waits] 0]!=$prod_guest_waits-$prod_guest_seats} {
					return 0
				} else {
					return 1
				}
			}
		}
		proc get_dummy_pos {dummy} {
			global dummyposlist
			incr dummy -4
			return [lindex $dummyposlist $dummy]
		}
		proc tablett_look {beercnt} {
			if {$beercnt < 2} {
				return eins
			} 
			if {$beercnt == 2} {
				return zwei
			}
			return drei
		}
		proc add_guest {ref} {
			global guests prod_guest_waits
			for {set i 0} {$i<$prod_guest_waits} {incr i} {
				if {[prod_guest guestget this $i]==$ref} {
					break
				}
			}
			if {$i==$prod_guest_waits} {
				log "WARNING: $ref [get_objname $ref] is not in Bar"
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
		proc incr_guest_timer {id} {
			global guesttimer
			set timer [lindex $guesttimer $id]
			lrep guesttimer $id [expr {$timer+1}]
		}
		proc incr_guest_beers {id} {
			global guestbeers
			set count [lindex $guestbeers $id]
			lrep guestbeers $id [expr {$count+1}]
		}
		proc reset_guest_timer {id} {
			global guesttimer
			lrep guesttimer $id 0
		}
		proc remove_guest {id} {
			global guests gueststates guesttimer guestbeers
			lrep guests $id 0
			lrep gueststates $id 0
			lrep guesttimer $id 0
			lrep guestbeers $id 0
		}
		proc get_next_guest_action {gnome} {
			global guests gueststates guesttimer myref guestbeers
			global prod_guest_seats prod_guest_waits current_worker
			global looktochief
			set rlst [list]
			set gid [lsearch $guests $gnome]
			if {$gid==-1} {set gid [add_guest $gnome]}
			if {$gid==-1} {return "sparetime_place_end"}
			set gst [lindex $gueststates $gid]
			set gti [lindex $guesttimer $gid]
			set gdy [prod_guest getlink this $gid]
			switch $gst {
				// 1-9 wartend, 9-19 sitzend
				0 { ;# angemeldet
					if {$gid>=$prod_guest_seats} {
						if {$current_worker} {
							set_guest_state $gid 2
							log "[get_objname $gnome] entering bar (0->2)"
						} else {
							set_guest_state $gid 1
							log "[get_objname $gnome] entering bar (0->1)"
						}
					} else {
						set_guest_state $gid 9
						log "[get_objname $gnome] entering bar (0->9)"
					}
					lappend rlst "global sparetime_talkanswer;set sparetime_talkanswer 0"
				}
				1 { ;# wartet auf Barkeeper
					if {$gti>10} {
						lappend rlst "sparetime_place_end"
					} elseif {$current_worker} {
						set_guest_state $gid 2
						lappend rlst "sparetime_idle_loop"
					} else {
						lappend rlst "sparetime_idle_loop"
						lappend rlst "sparetime_idle_loop"
						lappend rlst "sparetime_idle_loop"
						lappend rlst "sparetime_idle_loop"
					}
				}
				2 { ;# wartet auf Platzanweisung
					if {$gti>30} {
						lappend rlst "sparetime_place_end"
					} else {
						if {$looktochief} {
							lappend rlst "rotate_towards $current_worker"
						} else {
							set lookpos [vector_add [get_pos this] [get_linkpos this [irandom 8 10]]]
							log "lookpos $lookpos"
							lappend rlst "rotate_towards \{$lookpos\}"
						}
						lappend rlst "sparetime_idle_loop"
						lappend rlst "sparetime_idle_loop"
						lappend rlst "sparetime_idle_loop"
						lappend rlst "sparetime_idle_loop"
						if {$looktochief} {
							lappend rlst "sparetime_place_relief scratchhead 0.05"
						}
					}
				}
				3 { ;# Platz suchen und hinsetzen
					if {$gti>10} {
						lappend rlst "sparetime_place_end"
					} elseif {[ask_for_free_seat 1]} {
						lappend rlst "sparetime_take_seat $myref"
						//set_guest_state $gid 10
						log "[get_objname $gnome] sitting down (3->10)"
					} else {
						lappend rlst "sparetime_idle_loop"
						lappend rlst "sparetime_idle_loop"
						lappend rlst "sparetime_idle_loop"
						lappend rlst "sparetime_idle_loop"
					}
				}
				9 { ;# hinsetzen
					set roty [lindex [get_linkrot this $gdy] 1]
					lappend rlst "rotate_toangle $roty"
					lappend rlst "play_anim sitdown_chair"
					set_guest_state $gid 10
					log "[get_objname $gnome] sitting down (9->10)"
				}
				10 { ;# Bier bestellen
					if {[lindex $guestbeers $gid]>5||$gti>30} {
						lappend rlst "sparetime_place_end"
					} else {
						lappend rlst "play_anim sitchairloop"
						lappend rlst "play_anim sitchairloop"
						lappend rlst "play_anim sitchairloop"
						lappend rlst "play_anim sitchairloop"
						lappend rlst "play_anim sitchairloop"
						set rlst [linsert $rlst [irandom 3 5] "sparetime_place_relief sitchairorder 0.05"]
						set rlst [linsert $rlst [irandom 2] "play_anim sitchairbore"]
						prod_guest addorder this $gid
					}
				}
				11 { ;# Bier entgegen nehmen
					set idx [inv_find this Halbzeug_bier]
					if {$idx!=-1} {
						set obj [inv_get this $idx]
						inv_rem this $obj
						del $obj
					}
					//lappend rlst "change_tool Halbzeug_bier 0 0"
					lappend rlst "play_anim sitchairbeerstart"
					set_guest_state $gid 12
				}
				12 { ;# Bier trinken
					if {$gti>3} {
						//lappend rlst "change_tool 0 0 0"
						lappend rlst "play_anim sitchairbeerstop"
						set_guest_state $gid 10
					} else {
						lappend rlst "play_anim sitchairbeerloop"
						lappend rlst "play_anim sitchairbeerloop"
						lappend rlst "play_anim sitchairbeerloop"
						lappend rlst "play_anim sitchairbeerloop"
						lappend rlst "call_method $myref incr_beer_count $gid;sparetime_place_relief sitchairbeerdrink 0.3"
					}
					//log "[get_objname this] (12) gueststate: $gti ($gid) -> $rlst"
				}
			}
			incr_guest_timer $gid
			return $rlst
		}
		proc get_next_chief_action {} {
			global current_worker
			if {[dist_between this $current_worker]>10} {return {"prod_goworkdummy 0"}}
			global freeseats looktochief prod_guest_seats prod_guest_waits gueststates
			global guesttimer
			set rlst [list]
			set freetables ""
			set exper [prod_getgnomeexper $current_worker [call_method this prod_item_exp_infl Barbetrieb]]
			set exper [hmax 1.0 [expr {$exper+0.2}]]
			set exp_incrs [call_method this prod_item_exp_incr Barbetrieb]
			if {(3&$freeseats)==3} {lappend freetables 9}
			if {(12&$freeseats)==12} {lappend freetables 8}
			set orderlist [list]
			set orderheight 0
			set mostorder -1
			set maxorder 0
			set guestcnt 0
			for {set i 0} {$i<$prod_guest_seats} {incr i} {
				if {[lindex $gueststates $i]} {incr guestcnt}
				set order [prod_guest getorder this $i]
				lappend orderlist $order
				incr orderheight $order
				if {$order>$maxorder} {set mostorder $i;set maxorder $order}
			}
			set next_guest_toplace 0
			set longestwait -1
			if {[ask_for_free_seat 1]} {
				for {} {$i<$prod_guest_waits} {incr i} {
					if {[lindex $gueststates $i]} {incr guestcnt}
					set istate [lindex $gueststates $i]
					if {$istate==2} {
						set gti [lindex $guesttimer $i]
						if {$gti>$longestwait} {
							set next_guest_toplace $i
							set longestwait $gti
						}
					} elseif {$istate==3} {
						set next_guest_toplace 0
						break
					}
				}
			}
			set beer 0
			foreach item [inv_list this] {
				if {[get_objclass $item]=="Bier"} {
					incr beer
				}
			}
			set beam [list]
			set beamlen 0.0
			if {$orderheight} {
				lappend beam [fincr beamlen [expr {($beer+$orderheight)*0.1*($exper+0.3)}]]
			} else {
				lappend beam 0.0
			}
			if {$next_guest_toplace} {
				lappend beam [fincr beamlen [expr {$longestwait*0.5*($exper+0.5)}]]
				set looktochief 1
			} else {
				lappend beam 0.0
				set looktochief 0
			}
			lappend beam [fincr beamlen [hmax [expr {[llength $freetables]*0.25}] 0.0]]
			fincr beamlen [expr {(0.5+$guestcnt*0.2)*(1-$exper)}]
			//log "beam: ($beam) $beamlen - $beer,$orderheight,$longestwait,($freetables),$exper"
			set rnd [expr {rand()*$beamlen}];set i 0
			foreach val $beam {
				if {$rnd<$val} {
					switch $i {
						0 {if {$beer} {set act "beer";break}}
						1 {if {$next_guest_toplace} {set act "place";break}}
						2 {set act "clean";break}
					}
				}
				set act "laze"
				incr i
			}
			log "Deciding what to do: ($orderheight,$longestwait,$freetables,($beam),$rnd/$beamlen,$exper)->$act"
			switch $act {
				"beer" {
					global dummyposlist
					set cpos 0
					set mostbeercnt [hmin 2.99 [expr 2*$exper-1+$orderheight*0.05+$beer*0.05+rand()*0.5]]
					set beercnt 1
					if {$mostbeercnt>1.0} {
						set otherseat [expr $mostorder^1]
						set otherorder [prod_guest getorder this $otherseat]
						if {$otherorder} {
							incr beercnt
							lappend maxorder $otherorder
							lappend mostorder $otherseat
						}
					}
					if {$mostbeercnt>$beercnt} {
						set othertable [expr [lindex $mostorder 0]^2]
						lappend othertable [expr $othertable^1]
						set mostothertablesorder 0
						foreach seat $othertable {
							set seatorder [prod_guest getorder this $seat]
							if {$seatorder} {
								if {$mostothertablesorder&&$mostbeercnt>$beercnt} {
									if {$mostothertablesorder<$seatorder} {
										set mostorder [lreplace $mostorder 2 2 $seat]
										set maxorder [lreplace $maxorder 2 2 $seatorder]
									}
								} else {
									incr beercnt
									lappend mostorder $seat
									lappend maxorder $seatorder
								}
							}
						}
					}
					lappend rlst "prod_walk_and_consume_itemtype Bier"
					lappend rlst "prod_goworkdummy 0"
					lappend rlst "prod_setworkdummy 0"
					lappend rlst "prod_turnfront"
					foreach i $mostorder {
						lappend rlst "prod_anim barkeeper"
					}
					log "[get_objname this] bring beer to $mostorder ($maxorder) $mostbeercnt ($exper)"
					lappend rlst "prod_anim worktopholz"
					lappend rlst "change_tool Halbzeug_tablett 0 0;prod_changetoollook [tablett_look $beercnt]"
					for {set i 0} {$i<$beercnt} {incr i} {
						lappend rlst "prod_create_itemtype_ppinv_hidden Halbzeug_bier"
					}
					set restexp 1.0
					foreach seat $mostorder {
						if {$seat>1} {
							set dummy 8;set angle 3.6
						} else {
							set dummy 9;set angle 5.5
						}
						if {$cpos!=$dummy} {
							lappend rlst "prod_goworkdummy_with_box $dummy"
							lappend rlst "prod_turnangle $angle walkbox"
							set cpos $dummy
						}
						incr beercnt -1
						lappend rlst "prod_anim trayservea"
						lappend rlst "change_tool_look [tablett_look $beercnt];prod_anim trayserveb"
						lappend rlst "prod_beam_itemtype_near_dummypos Halbzeug_bier $dummy [lindex $dummyposlist $seat];prod_anim trayservec"
						lappend rlst "prod_call_method removeorder $seat;prod_call_method guest_stateset $seat 11"
						lappend rlst "prod_exp $exp_incrs [set restexp [expr $restexp*0.6]]"
					}
					lappend rlst "prod_anim trayputawaya"
					lappend rlst "change_tool 0 0 0;prod_anim trayputawayb"
					lappend rlst "prod_goworkdummy 0"
					lappend rlst "prod_turnfront"
				}
				"place" {
				//	log "[get_objname this] placing $next_guest_toplace ($longestwait)"
					lappend rlst "prod_goworkdummy 0"
					lappend rlst "prod_turnfront"
					lappend rlst "prod_anim showleft"
					lappend rlst "prod_anim showright"
					lappend rlst "prod_call_method guest_stateset $next_guest_toplace 3"
					lappend rlst "prod_exp $exp_incrs 0.2"
				}
				"clean" {
					if {$freetables!=""&&rand()<0.8} {
						set table [lindex $freetables [irandom [llength $freetables]]]
						if {$table==8} {set angle 2.2} {set angle 4.1}
					//	log "[get_objname this] cleaning table $table"
						lappend rlst "prod_goworkdummy $table"
						lappend rlst "prod_turnangle $angle"
						lappend rlst "prod_anim cleanfloorstart"
						lappend rlst "prod_anim cleanfloorloop"
						lappend rlst "prod_anim cleanfloorloop"
						lappend rlst "prod_anim cleanfloorloop"
						lappend rlst "prod_anim cleanfloorstop"
						lappend rlst "prod_goworkdummy 0"
						lappend rlst "prod_turnfront"
					} else {
					//	log "[get_objname this] cleaning desk"
						lappend rlst "prod_goworkdummy 0"
						lappend rlst "prod_turnfront"
						lappend rlst "prod_anim cleanfloorstart"
						lappend rlst "prod_anim cleanfloorloop"
						lappend rlst "prod_anim cleanfloorloop"
						lappend rlst "prod_anim cleanfloorloop"
						lappend rlst "prod_anim cleanfloorstop"
					}
					lappend rlst "prod_exp $exp_incrs 0.05"
				}
				"laze" {
				//	log "[get_objname this] lazing"
					if {[vector_dist3d [get_pos $current_worker] [vector_add [get_pos this] [get_linkpos this 0]]]>0.5} {
						lappend rlst "prod_goworkdummy 0"
						log "walking"
					}
					lappend rlst "prod_turnfront"
					lappend rlst "sparetime_filler_loop"
				}
			}
			return $rlst
		}

		proc bar_betrieb {} {
			global current_worker prod_guest_seats gueststates freeseats looktochief
			set freeseats 0
			for {set i 0} {$i<$prod_guest_seats} {incr i} {
				if {[prod_guest guestget this $i]==0} {set_guest_state $i 0}
				if {[lindex $gueststates $i]<10} {incr freeseats [expr 1<<$i]}
			}
			if {$current_worker} {
				if {![obj_valid $current_worker]} {
					set current_worker 0
					set looktochief 0
				}
			}
		//	set_prod_materialneed this 0
			if {[inv_find this Bier] == -1} {
				set_prod_materialneed this 1
			} else {
				set beer 0
				foreach item [inv_list this] {
					if {[get_objclass $item]=="Bier"} {
						incr beer
					}
				}
				if {$beer>7} {
					set_prod_materialneed this 0
				}
			}
		}

		sparetime this announce fun

	}

	obj_exit {
		sparetime this disannounce
	}

}

