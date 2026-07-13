//# STOPIFNOT FULL
call scripts/misc/utility.tcl

def_class _Liebesdienst service material 1 {} {}
def_class Bordell service production 4 {} {
	
	method prod_item_actions {item} {
		global gender current_worker progress exper
		set gender [call_method $current_worker get_gender]
		set rlst [list]
		set exper [prod_getgnomeexper $current_worker [call_method this prod_item_exp_infl _Liebesdienst]]
		if {$progress} {
			for {set i 0} {$i<20} {incr i} {
				lappend rlst "prod_call_method get_action"
			}
		} else {
			if {[vector_dist3d [get_pos $current_worker] [get_dummy_pos 22]]>1} {
				lappend rlst "prod_goworkdummy 22"
			}
			lappend rlst "prod_turnfront"
			lappend rlst "prod_anim brothela"
			lappend rlst "prod_anim kneebend"
			if {$gender=="male"} {
				lappend rlst "prod_anim scratch"
			} else {
				lappend rlst "prod_anim breathe"
			}
			lappend rlst "prod_anim brothela"
		}
		return $rlst
	}
	
	method get_action {} {get_next_action}
	method im_in_place {} {set worker_inplace 1}
	
	method guest_action {who} {return [guest_action $who]}
	method guest_end_action {who} {return [guest_break $who]}
	method set_progress {int} {set progress $int}
	
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl
	
	method stop_prod_tasklist {who} {
		global exper
		set exper 0.0
		set cmdlist [worker_break $who]
		stop_production
		return $cmdlist
	}
	
	def_event auto_close
	handle_event auto_close {
		if {$bed_open} {close_bed}
	}
	
	class_defaultanim bordell.standard
	class_flagoffset 1.7 3.5
	
	obj_init {
		set_anim this bordell.standard 0 $ANIM_LOOP
		call scripts/misc/genericprod.tcl
		set_energyconsumption this 0
		set_collision this 1
		set_prod_switchmode this 1
		
		prod_guest addlink this 0 0
		sparetime this announce sex
		
		set guesttimer 0
		set guestdummy 0
		set waitdummy 0
		set guest 0
		set progress 0
		set gender "unset"
		set worker_inplace 0
		set breaked 0
		set prodref [get_ref this]
		set bed_open 0
		set exper 0.0
		set finished 0
		
		set build_dummys [list 24 25 26 27 28 29 30]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {oben_rechtsholz unten_linksholz unten_rechtsholz unten_linksholz oben_rechtsholz unten_rechtsholz unten_rechtsholz}
		set damage_dummys {23 30}
				
		
		proc get_dummy_pos {dummy} {
			return [vector_add [get_pos this] [get_linkpos this $dummy]]
		}
		proc open_bed {} {
			global bed_open
			if {!$bed_open} {
				set_anim this bordell.oeffnen 0 1
				set bed_open 1
				timer_event this auto_close -attime [expr {[gettime]+5}]
			}
		}
		proc close_bed {} {
			global bed_open
			if {$bed_open} {
				set_anim this bordell.schliessen 0 1
				set bed_open 0
			}
		}
		proc shake_bed {} {
			global bed_open current_worker guest
			if {$bed_open} {
				action this anim bordell.schliessen {
					set_anim this bordell.bett_anim 0 2
				}
				set bed_open 0
				foreach g "$current_worker $guest" {switch_visibility $g 0}
			}
		}
		proc switch_visibility {who bool} {
			if {$who} {
				set_visibility $who $bool
				set m [call_method $who get_muetze_ref]
				set_visibility $m $bool
			}
		}
		proc get_next_action {} {
			global progress guest guestdummy waitdummy current_worker gender
			global worker_inplace breaked exper finished
			set rlst [list]
			set mypos [get_pos this]
			set exp_incrs [call_method this prod_item_exp_incr _Liebesdienst]
			log "brlpgr: $progress"
			switch $progress {
				1 {
					if {[vector_dist3d $mypos [get_dummy_pos 22]]>1} {
						lappend rlst "prod_goworkdummy 22"
						set worker_inplace 0
					} else {
						lappend rlst "prod_turnfront"
						lappend rlst "prod_anim brothela"
						set worker_inplace 1
					}
				}
				2 {
					if {$gender=="male"} {
						lappend rlst "prod_goworkdummy $waitdummy"
					} else {
						lappend rlst "prod_goworkdummy $guestdummy"
					}
					lappend rlst "prod_call_method im_in_place"
					if {$guestdummy==1} {
						lappend rlst "prod_turnright"
					} else {
						lappend rlst "prod_turnleft"
					}
					lappend rlst "prod_anim brothelcstart"
					set worker_inplace 0
				}
				3 {
					if {[vector_dist3d [get_pos $guest] [get_dummy_pos $guestdummy]]>1} {
						if {$gender=="male"} {set dummy $waitdummy} {set dummy $guestdummy}
						if {[vector_dist3d $mypos [get_dummy_pos $dummy]]>1} {
							lappend rlst "prod_goworkdummy $dummy"
						} else {
							lappend rlst "prod_anim teeter_w"
						}
					} elseif {[vector_dist3d $mypos [get_dummy_pos $guestdummy]]>1} {
						if {!$worker_inplace} {
							lappend rlst "prod_goworkdummy $guestdummy"
							lappend rlst "prod_call_method im_in_place"
							if {$guestdummy==1} {
								lappend rlst "prod_turnright"
							} else {
								lappend rlst "prod_turnleft"
							}
							lappend rlst "prod_anim brothelcstart"
							set worker_inplace 0
						}
						lappend rlst "prod_anim brothelcloop"
					} elseif {$worker_inplace} {
						lappend rlst "prod_anim brothelcstart"
						lappend rlst "prod_anim brothelcloop"
						set worker_inplace 1
					} else {
						lappend rlst "prod_anim brothelcloop"
					}
				}
				4 {
					shake_bed
					set imax [expr {$exper*10+5}]
					if {$gender=="female"} {fincr imax 1.0}
					set fract [expr {1.0/$imax}]
					for {set i 0} {$i<$imax} {incr i} {
						lappend rlst "prod_anim brothelcloop"
						lappend rlst "prod_exp $exp_incrs $fract"
					}
				}
				5 {
					switch_visibility $current_worker 1
					open_bed
					lappend rlst "prod_anim brothelcstop"
					lappend rlst "prod_anim tired"
					lappend rlst "prod_end_prod"
				}
				default {
					switch_visibility $current_worker 1
					if {$breaked==4} {
						open_bed
						lappend rlst "prod_anim brothelcstop"
						if {$guest} {
							lappend rlst "rotate_towards $guest"
						}
						lappend rlst "prod_anim impatient"
						set guest 0
					} elseif {$finished} {
						open_bed
						lappend rlst "prod_anim brothelcstop"
						lappend rlst "prod_anim tired"
						lappend rlst "prod_end_prod"
					}
				}
			}
			foreach cmd $rlst {
				log "brlcmd: $cmd"
				tasklist_add $current_worker $cmd
			}
		}
		proc worker_break {who} {
			global progress worker_inplace breaked guest
			if {$progress||$breaked} {
				set step [hmax $progress $breaked]
				set rlst [list]
				close_bed
				switch_visibility $who 1
				if {$breaked==4} {
					lappend rlst "prod_anim brothelcstop"
					if {$guest} {
						tasklist_clear $guest
						lappend rlst "rotate_towards $guest"
					}
					lappend rlst "prod_anim impatient"
				}
				return $rlst
			} else {
				return ""
			}
		}
		proc guest_action {who} {
			global progress guest guestdummy waitdummy current_worker gender
			global worker_inplace prodref breaked exper finished
			set rlst [list]
			log "sexpgr: $progress"
			switch $progress {
				0 {
					set finished 0
					set guest $who
					if {$gender==[call_method $who get_gender]} {
						return sparetime_sex_end
					}
					set progress 1
					set worker_inplace 0
					return [guest_action $guest]
				}
				1 {
					if {$current_worker&&$worker_inplace} {
						if {rand()<0.5} {
							set guestdummy 1
							set waitdummy 16
						} else {
							set guestdummy 3
							set waitdummy 17
						}
						lappend rlst "rotate_toback"
						if {$gender=="male"} {
							lappend rlst "play_anim brothela"
							lappend rlst "walk_dummy $prodref $guestdummy"
							lappend rlst "prod_callmethod $prodref set_progress 3"
						} else {
							lappend rlst "walk_dummy $prodref $waitdummy"
							//lappend rlst "prod_callmethod $prodref set_progress 2"
						}
						set progress 2
						set worker_inplace 0
					} else {
						lappend rlst "rotate_toangle [random 6.28]"
						lappend rlst "play_anim scout"
						lappend rlst "play_anim teeter_w"
					}
				}
				2 {
					if {$current_worker} {
						if {$worker_inplace} {
							lappend rlst "walk_dummy $prodref  $guestdummy"
							lappend rlst "prod_callmethod $prodref set_progress 3"
						} else {
							set progress 3
							lappend rlst "rotate_towards $current_worker"
							lappend rlst "play_anim teeter_w"
						}
						set worker_inplace 0
					} else {
						lappend rlst "walk_dummy $prodref 0"
						set progress 0
					}
				}
				3 {
					if {$current_worker} {
						open_bed
						if {$guestdummy==1} {
							lappend rlst "rotate_toright"
						} else {
							lappend rlst "rotate_toleft"
						}
						lappend rlst "play_anim brothelcstart"
						lappend rlst "play_anim brothelcloop"
						lappend rlst "prod_callmethod $prodref set_progress 4"
					} else {
						lappend rlst "walk_dummy $prodref 0"
						set progress 0
					}
				}
				4 {
					if {$current_worker} {
						shake_bed
						set imax [expr {$exper*10+5}]
						if {$gender=="female"} {fincr imax -1.0}
						for {set i 0} {$i<$imax} {incr i} {
							lappend rlst "play_anim brothelcloop"
						}
						lappend rlst "prod_callmethod $prodref set_progress 5"
						lappend rlst "play_anim brothelcloop"
					} else {
						open_bed
						lappend rlst "play_anim brothelcstop"
						lappend rlst "walk_dummy $prodref 0"
						set progress 0
					}
				}
				5 {
					switch_visibility $guest 1
					open_bed
					lappend rlst "sparetime_sex_relief $exper"
					lappend rlst "play_anim brothelcstop"
					lappend rlst "sparetime_sex_end"
					lappend rlst "prod_callmethod $prodref set_progress 0"
					set finished 1
					set guest 0
				}
			}
			return $rlst
		}
		proc guest_break {who} {
			global guest progress breaked current_worker
			set rlst [list]
			if {$progress} {
				set breaked $progress
				if {$progress==4} {
					open_bed
					switch_visibility $who 1
					lappend rlst "play_anim brothelcstop"
					if {$current_worker} {
						tasklist_clear $current_worker
					}
				}
				set progress 0
			}
			set guest 0
			return $rlst
		}
		
	}
	
	obj_exit {
		sparetime this disannounce
	}
	
}

