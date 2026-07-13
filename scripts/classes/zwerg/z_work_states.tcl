// ---------------------------------------------------------------------
// z_states.tcl
//
// Zwerg state handlers
// ---------------------------------------------------------------------


if {[in_class_def]} {
	state work_auto {
		state_work_auto_proc
	}

	state work_dispatch {
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
//			log "task-command:$command"
			eval $command
			return;
		}
		state_work_dispatch_proc
	}

	state_enter work_idle {
		state_enter_work_idle_proc
	}

	state_leave work_idle {

		state_leave_work_idle_proc
	}

	state work_idle {
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
//			log "spare:$command"
			eval $command
			return;
		}
		state_work_idle_proc
	}

	state work_active {
		state_work_active_proc
	}

	state work_breakable {
		state_work_breakable_proc
	}


	state mad_scientist {
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
//			log "task-command:$command"
			eval $command
			return;
		}
		state_mad_scientist_proc
	}


	state mad_scientist_dance {
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
//			log "task-command:$command"
			eval $command
			return;
		}
		state_mad_scientist_dance_proc
	}


} else {


	proc state_work_auto_proc {} {
		global state_log state_shell current_workplace current_worktask current_workitem current_workclass current_digpos current_workpos idletimeout current_occupation

		if {$state_log} {log "[get_objname this] passing state code WORK_AUTO"}
		if {$state_shell} {print "[get_objname this] passing state code WORK_AUTO"}
		set current_occupation "work"
		set idletimeout 0
		log "[get_objname this] got task: $current_worktask"
		if { $current_workplace == 0 } {
//			log "  place: none"
		} else {
//			log "  place: [get_objname $current_workplace]"
		}
		if { $current_workclass == 0 } {
//			log "  class: none"
		} else {
//			log "  class: $current_workclass"
		}
		if { $current_worktask == "pickup" } {
			state_trigger this work_dispatch
			tasklist_add this "prod_getfromanywhere $current_workclass"
			return
		}
		if { $current_worktask == "pickupitem" } {
			state_trigger this work_dispatch
			tasklist_add this "pickup $current_workitem"
			return
		}
		if { $current_worktask == "bringprod" } {
			state_trigger this work_dispatch
			log "[get_objname this]: $current_workclass --> $current_workplace"
			tasklist_add this "prod_transporttoprodlist \{ $current_workclass \} $current_workplace"
			return
		}
		if { $current_worktask == "transferprod" } {
			state_trigger this work_dispatch
			log "[get_objname this]: $current_workitem --> $current_workplace"
			tasklist_add this "prod_bringtoprod $current_workitem $current_workplace"
			return
		}
		if { $current_worktask == "work" } {
			state_trigger this work_dispatch
			tasklist_add this "prod_autobuild $current_workclass $current_workplace"
			return
		}
		if { $current_worktask == "dig" } {
			state_trigger this work_dispatch
			tasklist_add this "dig_starttask \{$current_digpos\}"
			return
		}
		if { $current_worktask == "pack" } {
			state_trigger this work_dispatch
			tasklist_add this "prod_autopack $current_workplace"
			return
		}
		if { $current_worktask == "buildup" } {
			state_trigger this work_dispatch
			tasklist_add this "prod_autobuildup $current_workplace"
			return
		}
		if { $current_worktask == "unpack" } {
			state_trigger this work_dispatch
			tasklist_add this "prod_autounpack $current_workplace \{$current_workpos\}"
			return
		}
		if { $current_worktask == "repair" } {
			state_trigger this work_dispatch
			tasklist_add this "prod_autorepair $current_workplace"
			return
		}
		state_trigger this work_idle
	}


	proc state_work_dispatch_proc {} {
		global state_log state_shell prod_failurecount current_workplace current_worktask current_workitem current_workclass
		global current_digpos idletimeout current_occupation current_worklist current_plan
		global errorInfo

		if {$state_log} {log "[get_objname this] passing state code WORK_DISPATCH"}
		if {$state_shell} {print "[get_objname this] passing state code WORK_DISPATCH"}
		set current_occupation "work"
		set idletimeout 0

		if { [llength $current_worklist] == 0 } {
			set current_workplace 0
			state_trigger this work_idle
			return
		}
		set current_workdelay 2
		set currenttask [lindex $current_worklist 0]
//		log "next task:$currenttask"
		set evalerrMsg ""
		set evalresult 0
		if { [catch { set evalresult [eval $currenttask] } evalerrMsg ] } {
			log "Tasklist error in tasklist:$evalerrMsg"
			log " info:$errorInfo"
			if {$current_workplace != 0 && $current_workplace != "dig"} {
				log " Workplace: [get_objname $current_workplace]"
			}
			log " Current task: '$currenttask'"
			log " List:$current_worklist"
		}
//      if {[is_selected this]} {log "Command:$currenttask"}
		if { ![string is boolean $evalresult] || $evalresult == "" } {
			log "Tasklist warning: '$currenttask' returned '$evalresult' no boolean !!!"
		} else {
			if { $evalresult } {
				set prod_failurecount 0
				set current_worklist [lreplace $current_worklist 0 0]
				if {$current_workplace != 0 && $current_workplace != "dig"} {
					call_method $current_workplace prod_progress
				}
				return
			} else {
//   			log "production of item failed"
				if { $current_plan == "sparetime" } {
					state_triggerfresh this sparetime_dispatch
					return
				}
				incr prod_failurecount
				if {$prod_failurecount < 5} {
					state_disable this
					action this wait $current_workdelay {state_enable this}
				} else {
					set prod_failurecount 0
					set current_worklist [lreplace $current_worklist 0 0]
					state_triggerfresh this work_idle
				}
				return
			}
		}
	}


	proc state_enter_work_idle_proc {} {
		global state_log state_shell current_worktask current_workplace current_workclass
		global last_event event_repeat

		if {$state_log} {log "[get_objname this] entering state WORK_IDLE (state_enter code - reseting workplace, gnome_idle this 1)"}
		if {$state_shell} {print "[get_objname this] entering state WORK_IDLE (state_enter code - reseting workplace, gnome_idle this 1)"}
		stop_prod
		set current_worktask ""
		set current_workclass 0
		set last_event ""
		set event_repeat 0
		gnome_idle this 1
	}


	proc state_leave_work_idle_proc {} {
		global state_log state_shell
		if {$state_log} {log "[get_objname this] leaving state WORK_IDLE (state_leave code - gnome_idle this 0)"}
		if {$state_shell} {print "[get_objname this] leaving state WORK_IDLE (state_leave code - gnome_idle this 0)"}
		gnome_idle this 0
	}


	proc state_work_idle_proc {} {
		global state_log state_shell idletimeout current_workplace current_time_plan current_plan current_occupation
		global current_muetze_ref muetzen_counter muetzen_counter_start current_muetze_name muetzen_counter work_strike
		global sparetime_is_on

		if {$state_log} {log "[get_objname this] passing state code WORK_IDLE"}
		if {$state_shell} {print "[get_objname this] passing state code WORK_IDLE"}
//		log "[get_objname this]: WORK_IDLE IDLETIMEOUT = $idletimeout, CURRENT_PLAN = $current_plan"

		prod_gnome_state this idle

		incr idletimeout

		set current_occupation "idle"
		if { $idletimeout > 3 } {
			// guck mal ob du aus dem Weg gehen solltest oder so
			if {[act_when_idle]} {return}
		}
		if { $idletimeout > 5 } {
			if { [get_remaining_sparetime this] > 0.0 } {
//				log "[get_objname this] CHANGE TO SPARETIME work_idle"
				state_enable this
				state_triggerfresh this sparetime_dispatch
				return
			} elseif {$sparetime_is_on} {
				sparetime_state_end
			}
		}
		if { $idletimeout >10 } {
			if {$work_strike} {
				//zum Strike State wechseln
				 state_triggerfresh this strike
			}
			if { [get_gnomeposition this] == 0 } {
				if {rand()<0.25} {
					// idleanimationen bei Untaetigkeit
					state_triggerfresh this prodfill_dispatch
				}
			} else {
				if {[get_gnomeposition this]&&[get_prodautoschedule this]&&![walk_down_from_wall]} {return}
			}
		}

		if {$muetzen_counter < 0} {
			if {![get_prodautoschedule this]} {
                if {[call_method this get_nameofmuetze fight] != $current_muetze_name} {
					prod_change_muetze fight
					set muetzen_counter $muetzen_counter_start
				}
			} else {
				if {$idletimeout > 10 && $current_plan == "work"} {
					if {[call_method this get_nameofmuetze arbeitslos] != $current_muetze_name} {
						prod_change_muetze arbeitslos
						set muetzen_counter $muetzen_counter_start
					}
				}
			}
		}
		incr muetzen_counter -1

		set_idle_anim
		state_disable this
		action this wait 1 {state_enable this}
	}


	proc state_work_active_proc {} {
		global state_log state_shell current_plan
		if {$state_log} {log "[get_objname this] passing state code WORK_ACTIVE"}
		if {$state_shell} {print "[get_objname this] passing state code WORK_ACTIVE"}
		if { $current_plan == "sparetime" } {
			log "[get_objname this] CHANGE TO SPARETIME work_active"
			state_triggerfresh this sparetime_dispatch
			return
		}
	}

	proc state_work_breakable_proc {} {
		global state_log state_shell current_plan
		if {$state_log} {log "[get_objname this] passing state code WORK_BREAKABLE"}
		if {$state_shell} {print "[get_objname this] passing state code WORK_BREAKABLE"}
		if { $current_plan == "sparetime" } {
			log "[get_objname this] CHANGE TO SPARETIME work_breakable"
			state_triggerfresh this sparetime_dispatch
			return
		}
	}


	proc state_mad_scientist_proc {} {
		set_prodautoschedule this 0
		add_attrib this atr_Nutrition 1.0
		add_attrib this atr_Alertness 1.0
		add_attrib this atr_Mood      1.0

		set rnd [irandom 9]
//		log "mad scientist: action $rnd"
		switch $rnd {
			0 { tasklist_add this "play_anim calculator"
				tasklist_add this "play_anim calculator"
				tasklist_add this "play_anim scratchhead"
				tasklist_add this "play_anim calculator"
			  }
			1 { tasklist_add this "play_anim standloopa"
				tasklist_add this "play_anim standloopa"
				tasklist_add this "play_anim standloopa"
			  }
			2 { tasklist_add this "play_anim standloopa"
				tasklist_add this "play_anim standloopa"
			  }
			3 {	tasklist_add this "play_anim showright"
				tasklist_add this "play_anim showleft"
				tasklist_add this "play_anim dontknow"
			  }
			4 { tasklist_add this "play_anim listenastart"
				tasklist_add this "play_anim listenaloop"
				tasklist_add this "play_anim listenaloop"
				tasklist_add this "play_anim listenastop"
			  }
			5 { tasklist_add this "play_anim listenbstart"
				tasklist_add this "play_anim listenbloop"
				tasklist_add this "play_anim listenbloop"
				tasklist_add this "play_anim listenbstop"
			  }
			6 { if {[random 1.0] < 0.5} {
					tasklist_add this "play_anim talkacntae"
				    tasklist_add this "play_anim talkacntap"
				    tasklist_add this "play_anim talkacntbq"
				    tasklist_add this "play_anim talkacntce"
				    tasklist_add this "play_anim talkacntcp"
			    } else {
			  		tasklist_add this "play_anim talkacpoae"
			    	tasklist_add this "play_anim talkacpobe"
			    	tasklist_add this "play_anim talkacpobq"
			    	tasklist_add this "play_anim talkacpocp"
			    	tasklist_add this "play_anim talkacpocq"
				}
			  }
			7 { tasklist_add this "play_anim invent_c"
			    tasklist_add this "play_anim invent_c"
			    tasklist_add this "play_anim invent_b"
			    tasklist_add this "play_anim invent_done"
			  }
		    8 { tasklist_add this "play_anim invent_c"
			    tasklist_add this "play_anim invent_c"
			    tasklist_add this "play_anim invent_b"
			    tasklist_add this "play_anim invent_c"
			  }
		}

		if {$rnd == 6} {
			set talk [smalltalk get "msc"]
			set talk [lindex $talk [irandom [llength $talk]]]
			speechicon this add $talk [expr {[string length $talk]/8}] 1
		}
	}


	proc state_mad_scientist_dance_proc {} {
		set_prodautoschedule this 0
		add_attrib this atr_Nutrition 1.0
		add_attrib this atr_Alertness 1.0
		add_attrib this atr_Mood      1.0

		set rnd [irandom 4]
//		log "mad scientist: action $rnd"
		switch $rnd {
			0 { tasklist_add this "play_anim discoa"
			  }
			1 { tasklist_add this "play_anim discoc"
				tasklist_add this "play_anim discoc"
				tasklist_add this "play_anim discoc"
			  }
			2 { tasklist_add this "play_anim discod"
			  }
			3 {	action this rotate [random 6.0]
			  }
		}
	}

}
