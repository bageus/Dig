// zwerg methods
method get_trapped {type} {
	if {$type=="petrify"} {
		set trap_time 30
		set trap_mode 0
		set trap_type "petrify"
	}
	if {$type=="splat"} {
		create_particlesource 8 [vector_add [get_pos this] {0 -0.5 0}] {0 -0.1 0} 32 1
		set trap_time 3
		set trap_mode 0
		set trap_type "splat"
	}
	tasklist_clear this
	state_triggerfresh this trapped
}


method inv_rem_all {} {
	inv_rem_all
}


method baby_to_gnome {gender name worktime nutri alert mood hitpo emax attribs age} {
	log "erwachsen: $gender $name $nutri $alert $mood $hitpo"
	log "wt: $worktime"
	set gnome_gender $gender
	set_objgender this $gender
	if { $gnome_gender == "female" } {
		set_alternateanimdb this true
	} else {
		set_alternateanimdb this false
	}
	set_objname this $name
	set_attrib  this atr_Nutrition 	$nutri
	set_attrib  this atr_Alertness 	$alert
	set_attrib  this atr_Mood	   	$mood
	set_attrib  this atr_Hitpoints 	$hitpo
	set_attrib  this atr_ExpMax 	$emax

	set iattr 0
	foreach attribut [get_expattrib] {
		set_attrib this $attribut [lindex $attribs $iattr]
		incr iattr
	}
	log "***** [llength $worktime]"
	if { [llength $worktime] == 2 } {
		set_worktime this [lindex $worktime 0] [lindex $worktime 1]
		log "set_worktime this [lindex $worktime 0] [lindex $worktime 1]"
	}
	set was_baby 1

	set birthtime [expr {[gettime]-$age}]
	set_attrib this GnomeAge $birthtime
}


// Diese Methode dient zur Wiederauferstehung von Zwergen: ein frisches Zwergenobjekt kann damit auf die
// Werte eines längst verstorbenen Zwerges gesetzt werden <g>
// falls der Partner noch existiert und noch keinen "Neuen" hat, werden die beiden wieder vereint...

method ressurection {gender name worktime emax attribs age} {
	set gnome_gender $gender
	set_objgender this $gender
	if { $gnome_gender == "female" } {
		set_alternateanimdb this true
	} else {
		set_alternateanimdb this false
	}
	set_objname this $name
	set_attrib  this atr_Nutrition 	0.25
	set_attrib  this atr_Alertness 	0.1
	set_attrib  this atr_Mood	   	0.1
	set_attrib  this atr_Hitpoints 	0.1

	set_attrib  this atr_ExpMax 	$emax

	set iattr 0
	foreach attribut [get_expattrib] {
		set_attrib this $attribut [lindex $attribs $iattr]
		incr iattr
	}
	if { [llength $worktime] == 2 } {
		set_worktime this [lindex $worktime 0] [lindex $worktime 1]
	}
	set birthtime [expr {[gettime]-$age}]
	set_attrib this GnomeAge $birthtime
}


method get_gender {} {
	global gnome_gender
	return $gnome_gender
}

method get_current_occupation {} {
	return $current_occupation
}

method set_log {log} {
	set logon $log
}

method idle_anim {} {
	set_idle_anim
}

method fight_idle_anim {} {
	set_fight_idleanim
}

method cause_fleeing {} {
	tasklist_clear this
	run_away
}

method seq_hidetool {} {
	change_tool_finish_in
}

method seq_showtool {toolclass} {
	global current_tool_class
	global current_tool_item
	if {$toolclass==$current_tool_class} {
		 return
	}
	if {$toolclass != 0} {
		set outobj [new $toolclass]
		set_physic $outobj 0
		change_tool_finish_out $outobj
	}
}


method seq_checktool {} {
	global current_tool_item
	return $current_tool_item
}


method walk_outofplacement {prodref} {
	tasklist_add this "walk_out_of $prodref"
}



// liefert 1, wenn der Zwerg eine Taucherglocke trägt

method is_wearing_divingbell {} {
	global  is_wearing_divingbell
	return $is_wearing_divingbell
}

method get_muetze_ref {} {
	global current_muetze_ref
	return $current_muetze_ref
}


method get_nameofmuetze {category} {
	return [get_nameofmuetze_proc $category]
}


// nur für den Sequenzer: liefert immer den Namen der gewünschten Kategorie, auch bei
// nicht-menschlichen Spielern!

method get_nameofmuetze_seq {category} {
	return [get_nameofmuetze_proc $category 1]
}


method change_muetze {category} {
	prod_change_muetze $category
}

method create_muetze {muetze} {
	create_muetze $muetze
}

method del_current_muetze {} {
	del_current_muetze
}

method set_counterwiggle {party} {
	set is_counterwiggle $party
}

method getbirthtime {} {
	global birthtime
	return $birthtime
}

method init {} {
	timer_unset this 1
	if {$gnome_initialized} {return}
	global gnome_gender was_baby current_muetze myref is_campaign is_counterwiggle
	global myhairs haircolor myglasses is_tutorial is_human birthtime clothing hatcolor
	if {[is_storymgr]} {
		set is_campaign 1
		set hatcolor 0
	} else {
		set is_campaign 0
	}
	if {[get_objownertype this]==2} {
		set is_human 0
	} else {
		set is_human 1
	}
	if {[obj_query this -class {Trigger_Tutorial Trigger_Tournament} -limit 1]} {
		set is_tutorial 1
		set hatcolor 0
	} else {
		set is_tutorial 0
	}

	set myref [get_ref this]
	if {!$was_baby} {auto_choose_workingtime this}
	set nam 0
	if { $gnome_gender == "unset" } {
		if { ! [minimalrun] } {
			set gend [get_info gender]
			if { $gend != 0 } {
				set gnome_gender $gend
			} else {
				if {[gethours]<2.0} {
					set og [lnand 0 [obj_query this -class Zwerg -owner 0 -flagpos male]]
					set og [concat $og [lnand 0 [obj_query this -class Zwerg -owner 0 -flagpos female]]]
					set ogl [llength $og]
					if {$ogl==4} {
						set gnome_gender [lindex {male female} [irandom 2]]
					} else {
						set gnome_gender [auto_choose_gender this]
					}
					switch $ogl {
						0 { set birthtime [expr {[gettime]-7*1800}] }
						1 { set birthtime [expr {[gettime]-6*1800}] }
						2 { set birthtime [expr {[gettime]-4*1800}] }
						3 { set birthtime [expr {[gettime]-3*1800}] }
						4 { set birthtime [expr {[gettime]-1*1800}] }
					}
					set_attrib this GnomeAge $birthtime
				} else {
					set gnome_gender [auto_choose_gender this]
				}
			}
		} else {
			set gnome_gender "male"
		}
		set nam [get_info name]
	}
	if {$is_counterwiggle} {set gnome_gender "male"}
	set_objgender this $gnome_gender
	if { $nam != 0 } {
		set_objname this $nam
	}
	if { $gnome_gender == "female" } {
		set_alternateanimdb this true
		if { $nam == 0 && !$was_baby } {
			set_objname this auto female
		}
	} else {
		if { $nam == 0 && !$was_baby } {
			set_objname this auto male
		}
	}
	if { $was_baby == 0 } {
		// Axel: Freizeit von Zwerg 3 und 4 wird andersgeschlechtigen von Z 1 und 2 gleichgeschaltet (Wunsch von Thomas)
		if {[gethours]<1.0} {
			set othergender [string map {female male male female} $gnome_gender]
			set othergender [obj_query this "-class Zwerg -owner own -flagpos $othergender"]
			if {$othergender==0} {set othergender ""}
			foreach og $othergender {
				if {![call_method $og get_reprodpartner]} {
					call_method this reprod_becomepartner $og
					call_method $og reprod_becomepartner $myref
					log "Vermählung [get_objname this] ($myref) [get_objname $og] ($og)"
					if {[llength $othergender]<2} {set wt 0.0} {set wt 6.0}
					set_worktime this $wt 6.0
					set_worktime $og $wt 6.0
					break
				} else {
					set_worktime this 0.0 6.0
				}
			}
			unset othergender
			catch {unset og}
			catch {unset ogl}
		}
	}
	sel /obj
	set own [get_owner this]
	set m 0
	set clothing 00
	set txtvarlist ""
	set hairlist 0
	set clan [get_objrace this]
	//set clan $own
	set clanname [lindex {"" Voodoo_ Knocker_ Brain_ Vampir_} $clan]
	set myhairs 0
	set b_brille 0
	if {$hatcolor==-1} {set hatcolor [expr {[get_player_color $own]+1}]}

	switch ${gnome_gender}$clan {
		"male0" {
			set txtvarlist {00 11 22 33 44}
			set hairlist {0 0 0 1 1 2 3 4 4}
			}
		"male1" {
			set txtvarlist {55 66}
			sel /obj
			set myhairs [new Dummy_Voodoo_haare_a]
			link_obj $myhairs this 4
			set hairlist {0 0 0 1 1 2 2}
			}
		"male2" {
			set txtvarlist {77 88}
			set hairlist {0 0 1 1 1 3 4 4}
			}
		"male3" {
			set txtvarlist {99 9A}
			sel /obj
			set myglasses [new Dummy_Brain_Brille]
			link_obj $myglasses this 4
			set hairlist {0 0 1 1 2}
			}
		"male4" {
			set txtvarlist {AB}
			set hairlist {0 0 0 2}
			}
		"female0" {
			set txtvarlist {00 01 02 03 04 10 11 12 13 14 20 21 22 23 24 30 31 32 33 34 40 41 42 43 44}
			set hairlist {0 2 3 4}
			}
		"female1" {
			set txtvarlist {55 66}
			set hairlist {7 8 9 10 11 12}
            sel /obj
			set myhairs [new Dummy_Voodoo_haare_b]
			link_obj $myhairs this 4
			}
		"female2" {
			set txtvarlist {77 87}
			set hairlist {0 2 3 4}
			}
		"female3" {
			set txtvarlist {98}
			set hairlist {1 1 1 1 13}
			}
		"female4" {
			set txtvarlist {A9}
			set hairlist {1 1 1 13}
			}
		default {
			set m 0
			if {[get_objclass this]=="Zuschauer"} {
				switch $gnome_gender {
					"male" {
						set m [new Dummy_Muetze_a]
						}
					"female" {
						set m [new Dummy_Muetze_b]
						set hairlist {0 1 2 3 4}
						}
				}
			} else {
				switch $gnome_gender {
					"male" {
						;#set m [new Dummy_Muetze_d]
						}
					"female" {
						;#set m [new Dummy_Muetze_h]
						set hairlist {0 1 2 3 4}
						}
				}
			}
		}
	}

	if { $m != 0 } {
		set current_muetze_name [get_objname $m]
		set current_muetze_ref $m ;#????
		link_obj $m this 4
	}
	// Vordefinierte Kleidungsvarianten
	if {$txtvarlist!=""} {
		set clanlist [obj_query this "-class Zwerg -owner own -flagpos $gnome_gender"]
		if {$clanlist==0} {set clanlist ""}
		set txtvarorig $txtvarlist
		set nilist [list]
		set tilist [list]
		set elist [list]
		foreach g $clanlist {
			set otv [call_method $g get_clothing]
			if {$otv=="xx"} {continue}
			if {[lsearch $txtvarlist $otv]==-1} {
					set nilist [land $otv $nilist]
	//				lappend tilist $otv
			} else {
				lappend elist $otv
				set txtvarlist [lnand $otv $txtvarlist]
			}
		}
		//log $txtvarlist
		if {$txtvarlist==""} {set txtvarlist [lnand [land $nilist $elist] $txtvarorig]}
		if {$txtvarlist==""} {log "clothing choice twice !!!";set txtvarlist $txtvarorig}
		set clothing [lindex $txtvarlist [irandom [llength $txtvarlist]]]
		//log "[get_objname this]: ($txtvarlist),($nilist),($elist),$clothing"
		unset txtvarlist nilist tilist elist txtvarorig
		unset clanlist
	}
	if {$is_counterwiggle} {
		set style [expr {[irandom 2]+$is_counterwiggle*2-2}]
		set_textureanimation this 0 [expr {$style+13}]
		set_textureanimation this 1 [expr {$style+14}]
		set haircolor [expr {$style+6}]
		set_textureanimation this 2 $haircolor
		set_textureanimation this 3 [expr {$style+16}]
	} else {
		set_textureanimation this 0 [scan [string index $clothing 0] %x]
		set_textureanimation this 1 [scan [string index $clothing 1] %x]
		// Haarfarbe
		set haircolor [lindex $hairlist [irandom [llength $hairlist]]]
		set_textureanimation this 2 $haircolor
		if {$myhairs} {
			set_textureanimation $myhairs 0 $haircolor
		}
	}
	// Editor gesetzte Erfahrungen
	foreach atr [concat atr_ExpMax [get_expattrib]] {
		if {[set val [get_info $atr]]} {
			set_attrib this $atr $val
		}
	}
	add_expattrib this exp_Nahrung 0.0
	unset atr val
#####################################################################################
	if {[get_info profession] == "madscientist"} {
		tasklist_clear this
		set_prodautoschedule this 0
		state_triggerfresh this mad_scientist
	}

	set gnome_initialized 1
}


method destroy {} {
	destroy
}

method add_logoff_code {code} {
	append logoff_code " ; $code"
}

method get_clothing {} {
	return $clothing
}


method Editor_Set_Info {ifo} {
	set info_string $ifo
}

method shell_eval {cmd} {
	print [eval $cmd]
}

method burn {} {
	if { $is_burning } { return }
	set is_burning 1
	#action this wait 0.1 {state_enable this} {state_enable this}
	change_particlesource this 4 27 {0 0 0} {0 0 0} 256 16 0 0 0 1
	set_particlesource this 4 1
	timer_event this evt_burnend -repeat 1 -interval 1 -userid 0 -attime [expr [gettime] + 3]
}

method fall {} {
	set anim [lindex "kungfumiddlehith kungfubackhith kungfuheadhith kungfubottomhith" [irandom 4]]
	state_disable this
	action this anim $anim {state_enable this}
}

method get_age {} {
	return [calc_age]
}

method seq_idle {} {
	seq_idle
}

# returns 1 falls Zwerg gegen die Wand gickt, ansonsten 0
method is_wall {dist} {
    set check_point [get_point_in_direction this $dist]
    set x [lindex $check_point 0]
    set y [expr [lindex $check_point 1] - 0.5]
    set z [lindex $check_point 2]
    set hmap [get_hmap $x $y]
    log "HMAP = $hmap"
    if { $hmap > $z} {
    	return 1
    }
    return 0
}


#############################################################################

method set_is_campaign {} {
	set is_campaign 1
}

method get_clan_exp_factor {attr} {
	return [clan_exp_factor $attr]
}


// macht den Zwerg unsichtbar (oder das Gegenteil)
// TIMER ID 3 ist für Unsichtbarkeit reserviert!

method set_invisibility {invisible time} {

	if {$invisible} {
		if {[get_cloaked this]} {
			timer_unset this 3		;# alte Timer löschen, Unsichtbarkeit wird verlängert!
		}

		set_cloaked this 1
		set_rendermaterial this additiveonly
		set_hoverable this 0
		create_particlesource 39 [vector_add [get_pos this] {0 -1 1}] {0 0 0} 1 0.2

		// Dauer der Tarnung einstellen:
   		timer_event this evt_cloakend_warning -repeat 1 -interval 1 -userid 3 -attime [expr {[gettime] + $time - 20}]
		log "[get_objname this] is now invisible!"

	} else {

		if {[get_cloaked this] == 0} {
			return
		}

		set_cloaked this 0
		set_rendermaterial this default
		set_hoverable this 1
		timer_unset this 3
		log "[get_objname this] is no longer invisible!"
	}
}



// macht den Zwerg unverwundbar (oder das Gegenteil)
// TIMER ID 4 ist für Unverwundbarkeit reserviert!

method set_invulnerability {invulnerable time} {

	if {$invulnerable} {
		if {[get_invulnerable this]} {
			timer_unset this 4
		}

		set_invulnerable this 1
		create_particlesource 40 [vector_add [get_pos this] {0 -1 1}] {0 0 0} 1 0.2
		change_particlesource this 9 13 {0 -0.3 0} {0 -0.1 0} 32 2 0
		set_particlesource this 9 1

		// Dauer der Unverwundbarkeit einstellen:
   		timer_event this evt_invulnerableend_warning -repeat 1 -interval 1 -userid 4 -attime [expr {[gettime] + $time - 5}]

	} else {

		set_invulnerable this 0
		free_particlesource this 9
		timer_unset this 4
	}
}
