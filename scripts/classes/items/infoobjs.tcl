def_class Info_Fog_Obw_Nacht_a none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.085 0.093 0.32 32 40
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}


def_class Info_Fog_Brains_a none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.066 0.063 0.165 32 70
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}


def_class Info_Fog_Brains_b none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.066 0.063 0.165 32 40
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}


def_class Info_Fog_Brains_c none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.066 0.063 0.165 32 30
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}


def_class Info_Pos_Troll none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_TrollX none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_TrollSpeier none info 0 {} {
	call scripts/misc/info_obj.tcl
	def_event evt_timer0
	def_event evt_repeat_spaw
	method activate_spaw {} {
		set mypos [get_pos this]
		activate
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
		proc activate {} {
			if {[get_max_fow this]>100||[llength [obj_query this -class Troll -range 32]]<4} {
				log "No Trolls sent ([get_max_fow this])"
				del this
				return
			}
			global mypos
			set pa [get_owner_attrib 0 PlayerAggressivity]
			if {$pa<0.4} {
				del this
				return
			}
			set nextweapons {}
			if {$pa>0.9} {
				lappend nextweapons {Dolch_1 5}
				lappend nextweapons {Lanze_2 2} ;# Z1
				lappend nextweapons {Krumsaebel Trollschild_1 2} ;# E1+S0
				lappend nextweapons {Hellebarde 2} ;# Z2
				lappend nextweapons {Streitkolben Trollschild_1 2} ;# Z0
			} elseif {$pa>0.85} {
				lappend nextweapons {Dolch_1 5}
				lappend nextweapons {Lanze_2 2} ;# Z1
				lappend nextweapons {Dolch_1 Trollschild_1 5} ;# E1+S0
				lappend nextweapons {Hellebarde 2} ;# Z2
				lappend nextweapons {Streitkolben 2} ;# Z0
			} elseif {$pa>0.8} {
				lappend nextweapons {+Keule Keule}
				lappend nextweapons {Lanze_1 5} ;# Z0
				lappend nextweapons {Dolch_1 Trollschild_1 5} ;# E1+S0
				lappend nextweapons {Lanze_2 2} ;# Z1
				lappend nextweapons {Streitkolben 2} ;# Z0
			} elseif {$pa>0.75} {
				lappend nextweapons {+Keule Keule}
				lappend nextweapons {Dolch_1 Trollschild_1 5} ;# E1+S0
				lappend nextweapons {Lanze_2 2} ;# Z1
				lappend nextweapons {Streitkolben 2} ;# Z0
			} elseif {$pa>0.7} {
				lappend nextweapons {}
				lappend nextweapons {Lanze_1 5} ;# Z0
				lappend nextweapons {Dolch_1 Trollschild_1 5} ;# E1+S0
				lappend nextweapons {Lanze_2 2} ;# Z1
			} elseif {$pa>0.65} {
				lappend nextweapons {+Keule Keule}
				lappend nextweapons {Lanze_1 5} ;# Z0
				lappend nextweapons {Dolch_1 Trollschild_1 5} ;# E1+S0
			} elseif {$pa>0.6} {
				lappend nextweapons {}
				lappend nextweapons {+Keule Keule}
				lappend nextweapons {Lanze_1 5} ;# Z0
			} elseif {$pa>0.55} {
				lappend nextweapons {+Keule Keule} ;# E0
				lappend nextweapons {Dolch_1 Trollschild_1 5} ;# E1+S0
			} elseif {$pa>0.5} {
				lappend nextweapons {Dolch_1} ;# E1
				lappend nextweapons {Lanze_1 5} ;# Z0
			} elseif {$pa>0.45} {
				lappend nextweapons {+Keule Keule} ;# E0
				lappend nextweapons {Dolch_1} ;# E1
			} else {
				lappend nextweapons {}
				lappend nextweapons {+Keule Keule} ;# E0
			}
			set trollcnt [llength $nextweapons]
			//lappend nextweapons {Zauberstab 4} ;# Z2
			log "Info_Pos_TrollSpeier [get_ref this] activated ($trollcnt) ($nextweapons) ($pa)"
			sel /obj
			log "Info_Pos_TrollSpeier [get_ref this] sending Trolls ([get_max_fow this])"
			set weaponstogive $nextweapons
			for {set i 0} {$i<$trollcnt} {incr i} {
				set tr [new Troll "" $mypos {0 0 0}]
				set ifo {{name asb_01} {walkorder {1 2}} {occupation guard} {speier 1}}
				if {$weaponstogive!=""} {
					set wc [lrem weaponstogive 0]
					set shield 0
					foreach item $wc {
						if {[string is integer $item]} {
							lappend ifo "texture $item"
						} else {
							if {$shield} {
								lappend ifo "shield $item"
							} else {
								if {[string index $item 0]=="+"} {
									inv_add $tr [new [string trimleft $item "+"]]
								} else {
									lappend ifo "weapon $item"
									set shield 1
								}
							}
						}
					}
				}
				call_method $tr Editor_Set_Info $ifo
			}
		}
	}
}

def_class Info_Pos_Spinne none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
		set nummer 0
	}
}

def_class Info_Pos_Zwerg none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_Uebergang_UM_1 none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_Uebergang_UM_2 none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_Uebergang_MK_1 none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_Uebergang_MK_2 none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_Uebergang_KL_1 none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_Uebergang_KL_2 none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_Uebergang_Endkampf none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Pos_ZwergTmp none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}



def_class WaterFlag none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}


def_class Info_Alarm_Troll none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}



def_class Info_Lore_Waypoint none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}


def_class Info_Fenris none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}



def_class Info_Riesenelfe_Waypoint none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}


def_class Info_Riesenelfe_Target none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}


def_class Karte none material 0 {} {
	class_defaultanim schriftrolle.standard
	method change_owner {no} {
		set_owner this $no
	}

	method use {} {
		picture_box data/gui/karte_a.tga
	}

	obj_init {
		set_anim this schriftrolle.standard 0 0
		set_hoverable this 1
	}
}

def_class Info_Drache_Waypoint none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Drache_Waypoint2 none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Drache_Digpoint none info 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_selectable this 0
		set_hoverable this 0
	}
}


def_class Info_Coll_a none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_a.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_b none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_b.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_c none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_c.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_d none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_d.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_e none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_e.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_f none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_f.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_g none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_g.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_h none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_h.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_i none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_i.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_j none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_j.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_k none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_k.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_l none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_l.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_m none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_m.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_n none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_n.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_o none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_o.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_p none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_p.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_q none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_q.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Coll_r none info 0 {} {
	call scripts/misc/info_obj.tcl
	class_physcategory 3
	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this kolli_box_r.standard 0 0
		if {![get_mapedit]} {
			set_texturevariation this 1
			set_visibility this 1
		}
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog none info 0 {} {
	call scripts/misc/info_obj.tcl
    def_event evt_timer0

	method init {} {
		set undefined 0.5
		set r [get_info r]
		set g [get_info g]
		set b [get_info b]
		set undefined 5
		set depth [get_info depth]
		set undefined 15
		set size [get_info size]

		horizon_set [get_pos this] $r $g $b $depth $size
	}

	handle_event evt_timer0 {
		del this
	}

	obj_init {
		call scripts/misc/info_obj.tcl
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -interval 1 -userid 0 -attime [expr [gettime] + 1]
	}
}

def_class Info_Fog_Urw_a none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		log "horizon_set [get_pos this] 0.305 0.375 0.429 32 70"
		horizon_set [get_pos this] 0.305 0.375 0.429 32 70
		if {![get_mapedit]} {del this}
	}

	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Urw_b none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.305 0.375 0.429 32 40
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Urw_c none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.305 0.375 0.429 32 30
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0

	}
}





def_class Info_Fog_Urw_d none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		log "horizon_set [get_pos this] 0.305 0.375 0.429 32 15"
		horizon_set [get_pos this] 0.305 0.375 0.429 32 15
		if {![get_mapedit]} {del this}
	}

	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}








def_class Info_Fog_Swf_a none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.449 0.523 0.105 32 70
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Swf_b none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.449 0.523 0.105 32 40
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Swf_c none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.449 0.523 0.105 32 30
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Kris_a none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.371 0.379 0.684 32 100
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Kris_b none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.371 0.379 0.684 32 70
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Kris_c none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.371 0.379 0.684 32 40
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Kris_d none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.371 0.379 0.684 32 30
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Lava_a none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.527 0 0 32 70
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Lava_b none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.527 0 0 32 40
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class Info_Fog_Lava_c none info 0 {} {
	call scripts/misc/info_obj.tcl
	method init {} {
		horizon_set [get_pos this] 0.527 0 0 32 30
		if {![get_mapedit]} {del this}
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this fog_kreuz.standard 0 0
		set_collision this 1
		set_selectable this 0
		set_hoverable this 0
	}
}

def_class FogRemover none info 0 {} {
	call scripts/misc/info_obj.tcl
	def_event event_delete
	def_event event_activate
	def_event evt_timer0

	method fog_remove_timed {o x y time} {
		global owner xrange yrange
		set owner $o
		set xrange $x
		set yrange $y
		timer_event this event_activate -repeat 0 -userid 0 -attime [expr [gettime]+$time]
	}

	method fog_remove {owner xrange yrange} {
		set_owner this $owner
		set_fogofwar this $xrange $yrange
	}

	method timer_delete {time} {
		timer_event this event_delete -repeat 0 -userid 0 -attime [expr [gettime]+$time]
	}

	method Editor_Set_Info {ifo} {
		set info_string $ifo
		foreach entry $ifo {
			eval "set $entry"
		}
	}

	handle_event event_activate {
		set_owner this $owner
		set_fogofwar this $xrange $yrange
	}

	handle_event event_delete {
		del
	}

	handle_event evt_timer0 {
		if {$on} {
			call_method this fog_remove 0 $xrange $yrange
		}
	}

	obj_init {
		call scripts/misc/info_obj.tcl
		set_owner this -1
		set_autolight this false
		set_physic this false
		set_fogofwar this -1 -1
		set_selectable this 0
		set_hoverable this 0
		set_visibility this 0
		set info_string {}

		set xrange 5
		set yrange 3
		set owner 0
		set on 0

		timer_event this evt_timer0 -repeat 0 -interval 1 -userid 0 -attime [expr [gettime] + 1]
	}
}

def_class Info_Fog_Aufdecker none info 0 {} {
	call scripts/misc/info_obj.tcl
	def_event evt_timer0
	def_event evt_fogremove
	def_event evt_fogremove_multi
	method Editor_Set_Info {ifo} {
		log "Eval Editor_Info"
		set info_string $ifo
		foreach entry $ifo {
			set val [lindex $entry 1]
			switch [lindex $entry 0] {
				"vorschau"		{ set preview $val }
				"sicht"			{ set sight $val }
				"repeat"		{ set repeats $val }
				"xrange"		{ set xrange $val }
				"yrange"		{ set yrange $val }
				"owner"			{ set actowner $val }
				"name"			{ set name $val }
				"koppeln"		{ set connect_to $val }
				"inaktiv"		{ set inactive $val}
				"grau"			{ set grey 1}
			}
		}
	}
	method remote_fogoff {owner onoff} {
		if {$onoff} {
			set_owner this $owner
			set_fogofwar this ${prefix}$xrange ${prefix}$yrange
			log "IFA remote fog for $owner ${prefix}$xrange ${prefix}$yrange"
		} else {
			set_fogofwar this -1 -1
		}
	}
	method entry_to_connect {ref} {
		set connected [lor $connected $ref]
	}
	method get_name {} {
		return $name
	}
	method activate {} {
		set inactive 0
	}
	obj_init {
		call scripts/misc/info_obj.tcl
		timer_event this evt_timer0 -attime [expr {[gettime] + 1}]
		timer_event this evt_fogremove -repeat -1 -interval 20 -userid 1 -attime [expr {[gettime] + 2}]
		set preview 2
		set sight 1
		set repeats -1
		set xrange 10
		set yrange 5
		set info_string {{vorschau 2} {sicht 1} {repeat -1} {xrange 10} {yrange 5} {owner any}}
		set actowner "any"
		set prefix ""
		set name ""
		set connect_to ""
		set connected ""
		set inactive 0
		set grey 0
	}
	handle_event evt_timer0 {
		set bbox [list [expr {-1*($xrange+$preview)}] [expr {-1*($yrange+$preview)}] -10 [expr {$xrange+$preview}] [expr {$yrange+$preview}] 5]
		if {!$sight} {set prefix "-"}
		if {$connect_to!=""} {
			timer_unset this 1
			set foggerlist [lnand 0 [obj_query this -class Info_Fog_Aufdecker -range 200]]
			if {$foggerlist!=""} {
				foreach fogger $foggerlist {
					if {[call_method $fogger get_name]==$connect_to} {
						call_method $fogger entry_to_connect [get_ref this]
						return
					}
				}
			}
			log "Warning: no Info_Fog_Aufdecker $name found in 200 meters ([get_ref this])"
		}
	}
	handle_event evt_fogremove {
		if {$inactive} {return}
		//log "IFA checking ($repeats)"
		if {$repeats} {
			set g [obj_query this -class Zwerg -boundingbox $bbox -owner $actowner -limit 1 -cloaked 1]
			if {$g} {
				set fogowner [get_owner $g]
				if {$grey} {
					set mx [get_posx this]
					set my [get_posy this]
					set xn [expr {int($mx-$xrange)}]
					set xp [expr {int($mx+$xrange)}]
					set yn [expr {int($my-$yrange)}]
					set yp [expr {int($my+$yrange)}]
					remove_black_fog $fogowner $xn $yn $xp $yp
					log "IFA reveal fog for $g ($fogowner) $xrange $yrange -> $xn $yn $xp $yp"
				} else {
					set_owner this $fogowner
					set_fogofwar this ${prefix}$xrange ${prefix}$yrange
					log "IFA reveal fog for $g ($fogowner) ${prefix}$xrange ${prefix}$yrange"
				}
				foreach fogger $connected {
					call_method $fogger remote_fogoff $fogowner 1
				}
				if {$repeats>0} {incr repeats -1}
			} else {
				if {!$grey} {
					set_fogofwar this -1 -1
				}
				foreach fogger $connected {
					call_method $fogger remote_fogoff 0 0
				}
			}
		} else {
			if {!$grey} {
				set_fogofwar this -1 -1
			}
			timer_unset this 1
			log "IFA deleting"
			del this
		}
	}
	handle_event evt_fogremove_multi {
		// funktioniert nicht, weil nur fuer einen Owner aufdeckbar
		set gl [obj_query this -class Zwerg -boundingbox $bbox -owner $actowner -cloaked 1]
		if {$gl==0} {set gl ""}
		set ownermask {0 0 0 0 0 0 0 0}
		foreach g $gl {
			lrep ownermask [get_owner $g] 1
		}
		for {set i 0} {$i<8} {incr i} {
			if {[lindex $ownermask $i]} {
				set_fogofwar this $i ${prefix}$xrange ${prefix}$yrange
			} else {
				set_fogofwar this $i -1 -1
			}
		}
	}
}

def_class Info_Sound_a none info 0 {} {
	call scripts/misc/info_obj.tcl

	state play {
		sound play fe_schritt1 1
		state_disable this;
		action this wait 2 {state_enable this}
		return
	}

	obj_init {

		call scripts/misc/info_obj.tcl
		set info_string "{blah fahsel}"
		set_anim this lscr_a.standard 0 0
		set_collision this 0
		set_selectable this 0
		set_hoverable this 0
		state_disable this

		set sounds 0
		set time   0
		set level  0
	}
}


