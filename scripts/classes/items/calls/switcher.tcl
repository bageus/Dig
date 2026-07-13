if {[in_class_def]} {
	call scripts/misc/animclassinit.tcl

	class_viewinfog 1

	method get_status {} {
		if {$status=="on"&&$switchmode!="toggle"} {return 1} {return 0}
	}

	method get_swtchpos {} {
		if {$switchpos==0} {
			set switchpos [expr {[get_posx this] - sin([get_roty this])*0.8}]
//			lappend switchpos [expr ceil(([get_posy this]-2.5)/4.0)*4.0+2.5]
			lappend switchpos [expr {[get_posy this] + 0.5}]
			lappend switchpos [expr {[get_posz this] + cos([get_roty this])*1.6}]
			set switchpos [vector_fix $switchpos]
		}
		return $switchpos
	}

	switch [string range [get_class_name] 9 13] {
		"knopf" {method get_switchanim {} {return "switchnorm"} }
		"hebel" {method get_switchanim {} {return "switchup"} }
	}
	method get_obicon {} {
		if {$status=="off"} {
			return $objicon
		} else {
			if {$switchmode=="toggle"} {
				return [string map {arrow_up arrow_down arrow_down arrow_up} $objicon]
			} else {
				return "cross"
			}
		}
	}
	method set_switchmode {mode} {
		switch [lindex $mode 0] {
			"once" {
				set switchmode "once"
				set opentime -1
			}
			"hold" {
				set switchmode "hold"
				set opentime [lindex $mode 1]
			}
			"toggle" {
				set switchmode "toggle"
				set opentime -1
			}
			"nohold" {
				set switchmode rebound
				set opentime 0
			}
			default {log "invalid switchmode ([get_objname this])"}
		}
	}
	method druecken {} {
		action this wait 0.5 {
			if {$status=="off"||$switchmode=="toggle"} {
				if {$status=="off"} {set pressanim press} {set pressanim release}
				action this anim $pressanim {
					set_anim this $switchanim $switchframe $ANIM_STILL
					if {$status=="off"} {
						set status on
						if {$switchmode!="toggle"} {
							set_attrib this Schaltstatus 1
						}
						log "| $pressaction | pnow"
						if {$fpressaction != ""} {
							log "firstaction: $fpressaction"
							eval $fpressaction
							set fpressaction ""
						} else {
//#ifdef _DEBUG
							eval $pressaction
//#elif
							catch {eval $pressaction}
//#endif
						}
					} else {
						set status off
						set_attrib this Schaltstatus 0
//#ifdef _DEBUG
						eval $releaseaction
//#elif
						catch {eval $releaseaction}
//#endif
						log "| $releaseaction | rnow"
					}
					if {-1<$opentime} {
						action this wait $opentime {
							log "Drücken wait finish"
							if {$status=="off"} {set releaseanim release} {set releaseanim press}
							action this anim release {
								log "Drücken anim finish"
								if {$status=="on"} {
									set status off
									set_attrib this Schaltstatus 0
									catch {eval $releaseaction}
				//					log "| $releaseaction | rnow"
									set_anim this $standardanim $standardframe 0 $ANIM_STILL
								} else {
				//					log "| $pressaction | pnow"
									set status on
									set_attrib this Schaltstatus 1
									catch {eval $pressaction}
									set_anim this $switchanim $switchframe 0 $ANIM_STILL
								}
							} {
								log "Drücken anim break"
							}
						} {
							log "Drücken wait break"
							if {$status=="on"} {
								set status off
								set_attrib this Schaltstatus 0
								catch {eval $releaseaction}
			//					log "| $releaseaction | rnow"
								set_anim this $standardanim $standardframe 0 $ANIM_STILL
							} else {
			//					log "| $pressaction | pnow"
								set status on
								set_attrib this Schaltstatus 1
								catch {eval $pressaction}
								set_anim this $switchanim $switchframe 0 $ANIM_STILL
							}
						}
					} else {
						if {$status=="on"} {
							set_anim this $switchanim $switchframe 0 $ANIM_STILL
						} else {
							set_anim this $standardanim $standardframe 0 $ANIM_STILL
						}
					}
				}
			}
		}
	}
	method entrasten {} {
		if {$status=="on"} {
			action this anim release {
				set_anim this $standardanim $standardframe $ANIM_STILL
			}
			log "| $releaseaction | rnow"
			set status off
			set_attrib this Schaltstatus 0
			catch {eval $releaseaction}
		}
	}
	method set_actiononpress {script} {
		set pressaction $script
	}
	method set_actiononrelease {script} {
		set releaseaction $script
	}
	method set_var {nr val} {set var$nr $val}
	method get_var {nr} {return [subst \$var$nr]}
	method get_uniquename {} {return $name}
	method Editor_Set_Info {infolist} {
		global info_string
		set info_string $infolist
		foreach sublist $infolist {
			switch [lindex $sublist 0] {
				"name" {set name [lindex $sublist 1]}
				"switchmode" {call_method this set_switchmode [lindex $sublist 1];set predefined 1}
				"target" {
					set var1 [lindex $sublist 1]
					set pressaction   "call_method \[find_door \$var1\] schalten [get_ref this] -1"
					set releaseaction "call_method \[find_door \$var1\] schliessen"
				}
				"multi"	{
							//call_method this set_switchmode "nohold"
							call_method this set_switchmode "hold 5"
							set predefined 1
							set pressaction ""
							set releaseaction ""
							foreach item [lindex $sublist 1] {
								set cmd [lindex $item 0]
								set dor [lindex $item 1]
								set rcmd "open"
								switch $cmd {
									"open" 	{set rcmd "schalten [get_ref this] -1"}
									"close"	{set rcmd "schliessen"}
								}
                                set pressaction "$pressaction\; call_method \[find_door $dor\] $rcmd"
							}
						}
				"cmd"	{
							set pressaction "[lindex $sublist 1]"
						}
				"rcmd"	{
							set releaseaction "[lindex $sublist 1]"
						}
				"fcmd"	{
							set fpressaction "[lindex $sublist 1]"
						}
			}
		}
		//log "----------------------"
		//log "Method [get_ref this]: ($var1,$var2,$var3) $name, $doorlist"
		//log "pressaction $pressaction"
		//log "releaseaction $releaseaction"
	}
	handle_event evt_timer0 {
		//log "----------------------"
		//log "event before [get_ref this]: ($var1,$var2,$var3) $name, $doorlist"
		//log $pressaction
		//log "releaseaction $releaseaction"
		set pressaction [subst $pressaction]
		set releaseaction [subst $releaseaction]
		//log "event after: ($var1,$var2,$var3) $name, $doorlist"
		//log $pressaction
		//log "releaseaction $releaseaction"
	}
	method is_on {} {
		return [ is_on ]
	}
} else {
	call scripts/misc/animclassinit.tcl
	set_hoverable this 1
	set_anim this $standardanim $standardframe $ANIM_STILL
	set status "off"
	set_attrib this Schaltstatus 0
	set info_string ""
	set pressaction ""
	set fpressaction ""
	set releaseaction ""
	set switchmode "hold"
	if {[string range [get_objclass this] 9 13]=="knopf"} {
		set objicon "arrow_right"
	} else {
		if {[lindex [split [get_objclass this] "_"] end]=="up"} {
			set objicon "arrow_up"
		} else {
			set objicon "arrow_down"
		}
	}
	set opentime 0
	set predefined 0
	set name [get_objname this]
	set switchpos 0
	set doorlist 0

	set var1 "";set var2 "";set var3 ""
	timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]

	proc is_on {} {
		global status
		return [ expr {$status == "on"} ]
	}

	proc find_door {name} {
		if {[string is integer $name]} {return $name}
		global doorlist
		if { $doorlist == 0 } {
			set objlist [lnand 0 [obj_query this "-type {tool production} -range 1000"]]
			set doorlist [list]
			foreach item $objlist {
				if { [check_method [get_objclass $item] door_ident]} {
					lappend doorlist $item
				}
			}
		}
		foreach item $doorlist {
			if {[call_method $item get_uniquename]==$name} {return $item}
		}
	}



}
