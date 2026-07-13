if {[in_class_def]} {
	
	def_event evt_timer_aggrcheck
	def_event evt_timer_notdead
	
	handle_event evt_timer_aggrcheck {
		if {1||[is_storymgr]} {
			set pa [get_owner_attrib 0 PlayerAggressivity]
			set smzone [sm_get_zone [get_posy this]]
			if {$smzone=="Urwald"} {
				set correct 1.0
				set playermin [expr {0.5+($player_aggressivity-0.5)*$correct}]
				set playermax [expr {0.5+($aggr_max-0.5)*$correct}]
			} elseif {$smzone=="Metall"} {
				set correct 1.0
				set playermin [expr {0.5+($player_aggressivity-0.5)*$correct}]
				set playermax [expr {0.5+($aggr_max-0.5)*$correct}]
			} else {
				set playermin $player_aggressivity
				set playermax $aggr_max
				set correct 1.0
			}
			if {$pa<$playermin||$pa>$playermax} {
				time_line_log ""
				time_line_log "--------[clock format [clock seconds] -format "%d-%m-%y %X"]---------"
				time_line_log "[get_objname this] deleting itself: $playermin-$pa-$playermax (Korrektur: $correct) <- $player_aggressivity"
				catch {take_items}
				catch {init_take}
				set idx 0
				foreach item [inv_list this] {
					inv_rem this $idx
					del $item
					incr idx
				}
				del this
			}
		}
	}
	
	handle_event evt_timer_notdead {
		adjust_player_aggr
	}
	
	method set_aggr_contact {} {
		if {$aggr_contact==0} {
			init_aggr_contact
			log "[get_objname this] aggr_contact remote"
		}
	}
	
	method call_aggr_finish {} {
		adjust_player_aggr 1
	}
	
	method im_attacking_you {} {
		// oh, im attacked
		set aggr_attacked 1
	}
	
} else {
	
	timer_event this evt_timer_aggrcheck -attime [expr {[gettime]+0.2}]
	
	set player_aggressivity -2.0
	set aggr_max 2.0
	set aggr_contact 0
	set aggr_attacked 0
	set aggr_counted 0
	set aggr_counter 0
	set dangerousness 1
	
	proc check_for_player_contact {} {
		global aggr_contact
		if {$aggr_contact} {return}
		if {[is_storymgr]} {
			if {[get_max_fow this]<10} {return}
			set pg [obj_query this -class Zwerg -owner 0 -range 5 -limit 1]
			if {$pg} {
				log "[get_objname this] aggr_contact seen"
				init_aggr_contact
			}
		} else {
			set aggr_contact 1
		}
	}
	
	proc init_aggr_contact {} {
		global aggr_contact dangerousness aggr_counter
		if {$aggr_contact} {return}
		set myclass [get_objclass this]
		switch $myclass {
			"Wuker" {
				set myrange 5
				set dangerousness 1.5
			}
			"Schwefelwuker" {
				set myrange 5
				set dangerousness 2
			}
			"Troll" {
				set myrange 15
				set dangerousness 3
			}
			"Spinne" {
				set myrange 10
				set dangerousness 3
			}
			"Kristallbrut" {
				set myrange 15
				set dangerousness 3
			}
			"Lavabrut" {
				set myrange 25
				set dangerousness 2
			}
			"Fresspflanze" {
				set myrange 5
				set dangerousness 2
			}
			"Alienpflanze" {
				set myrange 5
				set dangerousness 3
			}
			default {
				set myrange 5
				set dangerousness 1
			}
		}
		set ctime [gettime]
		set aggr_contact $ctime
		set companions [lnand 0 [obj_query this -class $myclass -range $myrange]]
		set dangerousness [expr {$dangerousness*(1.0+[llength $companions]*0.5)}]
		log "[get_objname this] aggr_contact set to [gettime] ($dangerousness) [llength $companions]"
		set nexttime [expr {100*$dangerousness}]
		timer_event this evt_timer_notdead -attime [expr {$ctime+$nexttime}]
		timer_event this evt_timer_notdead -attime [expr {$ctime+$nexttime*2.0}]
		timer_event this evt_timer_notdead -attime [expr {$ctime+$nexttime*5.0}]
		timer_event this evt_timer_notdead -attime [expr {$ctime+$nexttime*10.0}]
		foreach comp $companions {
			call_method $comp set_aggr_contact
		}
	}
	
	proc adjust_player_aggr {{remote 0}} {
		if {[is_storymgr]} {
			global aggr_counted aggr_contact aggr_attacked dangerousness
			global died_in_fight aggr_counter
			if {$aggr_counted} {return}
		//	set aggr_counted 1
			set dead 0
			if {[get_attrib this atr_Hitpoints]<0.1} {
				if {[obj_query this -class Zwerg -owner 0 -range 8 -limit 1]} {
					set dead 1
				} else {
					return
				}
			}
			set ctime [gettime]
			set vorher [get_owner_attrib 0 PlayerAggressivity]
			if {$dead} {
				set factor [expr {0.4*$dangerousness*(1.5-$vorher)}]
				if {!$aggr_attacked} {
					fincr aggr_contact -150.0
				}
				set aggr_incr [expr {$factor*pow(10,($aggr_contact-$ctime)/1800.0)*0.01}]
				set aggr_counted 1
				log "[get_objname this] getötet: adding $aggr_incr to players aggressivity"
			} else {
				set factor [expr {20.0*(1.5-$vorher)/($dangerousness+16.0)}]
				if {$remote} {
					set aggr_incr [expr {-0.01*$factor}]
				} else {
					set aggr_incr [expr {-0.0025*$factor}]
				}
				incr aggr_counter
				log "[get_objname this] verschont: adding $aggr_incr to players aggressivity"
			}
		//	set aggr_incr [expr {($aggr_contact+900.0-[gettime])*0.0000111}]
		//	set aggr_incr [hmax [hmin $aggr_incr 0.01] -0.01]
			set vorher [string range [expr {$vorher*100.0}] 0 4]
			add_owner_attrib 0 PlayerAggressivity $aggr_incr
			set nachher [string range [expr {[get_owner_attrib 0 PlayerAggressivity]*100.0}] 0 4]
			set aggrincr [string range [expr {$aggr_incr*100.0}] 0 4]
			
			// nur fuer Timeline
			set zeit [expr {[gettime]-$aggr_contact}]
			if {[string index $aggr_incr 0]!="-"} {set aggr_incr +$aggr_incr}
			set ctime [gettime]
			set cday [expr {int($ctime/1800.0)}]
			set cmonth [expr {$cday/20}]
			set cday [expr {$cday-$cmonth*20}]
			incr cmonth
			incr cday
			time_line_log ""
			time_line_log "--[clock format [clock seconds] -format "%d-%m-%y %X"]--$cday.$cmonth.1--"
			if {$dead} {
				time_line_log "[get_objname this] wurde nach $zeit Sekunden getötet. ($dangerousness)"
				if {$aggr_attacked} {
					time_line_log "Das Monster wurde angegriffen. PA: $aggr_incr $vorher -> $nachher"
				} else {
					time_line_log "Es erfolgte kein Angriffsbefehl. PA: $aggr_incr $vorher -> $nachher"
				}
			} else {
				time_line_log "[get_objname this] wurde nach $zeit Sekunden noch nicht getötet. ($dangerousness)"
				time_line_log "PA: $aggr_incr $vorher -> $nachher"
			}
		}
	}
	
}
