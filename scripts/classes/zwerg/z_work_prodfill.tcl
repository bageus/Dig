// z_prodfill.tcl

if {[in_class_def]} {

	state_enter prodfill_dispatch {
		gnome_idle this 1
		if {$current_tool_item != 0} {
			tasklist_add this "prod_changetool 0"
		}
		if {$current_workplace != 0} {
#			log "change back to work"
			state_triggerfresh this work_dispatch
			return
		}
		prodfill_random
	}

	state prodfill_dispatch {
		set idletimeout 0
		if { [tasklist_cnt this] > 0 } {
			set command [tasklist_get this 0]
			tasklist_rem this 0
#			log "[get_objname this]-prodfill:$command remaining: [tasklist_cnt this]"
			eval $command
			return
		} else {
			state_triggerfresh this work_idle
		}
	}

	state_leave prodfill_dispatch {
		gnome_idle this 0
	}

} else {

	set prodfill_activities [list]
	lappend prodfill_activities "prodfill_standsleep"
	lappend prodfill_activities "prodfill_smokepipe"
	lappend prodfill_activities "prodfill_cartwheel"
	lappend prodfill_activities "prodfill_handstand"
	lappend prodfill_activities "prodfill_pressup"
	lappend prodfill_activities "prodfill_carve"
	lappend prodfill_activities "prodfill_knit"
	lappend prodfill_activities "prodfill_stretch"
	lappend prodfill_activities "prodfill_jumproping"
	lappend prodfill_activities "prodfill_footbaging"
	set prodfill_activities [list]
	lappend prodfill_activities {Feuerstelle {warmbutt sniffatfood jumpfire}}
	lappend prodfill_activities {Hauklotz {cleanfloor pullup lean}}
	lappend prodfill_activities {Steinmetz {lean vaultover cleanfloor}}
	lappend prodfill_activities {Farm {sitfence}}
	lappend prodfill_activities {Brauerei {lean drinktub cleanfloor}}
	lappend prodfill_activities {Schreinerei {lean cleanfloor}}
	lappend prodfill_activities {Schmiede {lean gymanvil taichi warmbutt}}
	lappend prodfill_activities {Schmelze {warmhands}}
	lappend prodfill_activities {Tischlerei {}}
	lappend prodfill_activities {Saegewerk {lean cleanfloor}}
	lappend prodfill_activities {Waffenschmiede {taichi warmbutt}}
	lappend prodfill_activities {Dampfhammer {washhands warmhands cleanfloor}}
	lappend prodfill_activities {Laufrad {taichi}}
	lappend prodfill_activities {Wachhaus {}}
	lappend prodfill_activities {Lager {lean read}}
	lappend prodfill_activities {Bar {lean tapdrink}}
	lappend prodfill_activities {Dojo {taichi matrixdojo standjog}}
	lappend prodfill_activities {Schule {}}
	lappend prodfill_activities {Theater {boo cheer applaud}}
	lappend prodfill_activities {Fitnessstudio {}}
	lappend prodfill_activities {Dreherei {cleanfloor}}
	lappend prodfill_activities {Wasserrad {}}
	lappend prodfill_activities {Mittelalterkueche {}}
	lappend prodfill_activities {Industriekueche {}}
	lappend prodfill_activities {Luxuskueche {}}
	lappend prodfill_activities {Dampfmaschine {lean}}
	lappend prodfill_activities {Hochofen {lean warmbutt}}
	lappend prodfill_activities {Schleiferei {cleanfloor}}
	lappend prodfill_activities {Waffenfabrik {}}
	lappend prodfill_activities {Kristallschmiede {cleanfloor}}
	lappend prodfill_activities {Reaktor {}}
	lappend prodfill_activities {Labor {takedrugs}}
	lappend prodfill_activities {Krankenhaus {electrify takedrugs cleanfloor}}
	lappend prodfill_activities {Tempel {pray}}
	lappend prodfill_activities {Bowlingbahn {}}
	lappend prodfill_activities {Bordell {lean bedjump}}
	lappend prodfill_activities {Disco {}}
	set prodfill_defaultacts {standsleep stretch carve knit handstand smokepipe teeter scratch jumpbend impatient}

	proc prodfill_pray {} {
		global prodplace
		tasklist_add this "rotate_toback"
		tasklist_add this "prod_anim praystart"
		tasklist_add this "prod_anim prayloop"
		tasklist_add this "prod_anim prayloop"
		tasklist_add this "prod_anim prayloop"
		tasklist_add this "prod_anim praystop"
		tasklist_add this "rotate_tofront"
	}

	proc prodfill_lean {} {
		global prodplace
		if {[get_objclass $prodplace]=="Bordell"} {
			tasklist_add this "walk_dummy $prodplace 4"
		} else {
			tasklist_add this "walk_dummy $prodplace 5"
		}
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim leanstart"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanloop"
		tasklist_add this "prod_anim leanstop"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_cleanfloor {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 1"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim cleanfloorstart"
		tasklist_add this "prod_anim cleanfloorloop"
		tasklist_add this "prod_anim cleanfloorloop"
		tasklist_add this "prod_anim cleanfloorloop"
		tasklist_add this "prod_anim cleanfloorloop"
		tasklist_add this "prod_anim cleanfloorloop"
		tasklist_add this "prod_anim cleanfloorloop"
		tasklist_add this "prod_anim cleanfloorstop"
		tasklist_add this "prod_anim tired"
	}

	proc prodfill_warmbutt {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 2"
		tasklist_add this "rotate_toback"
		tasklist_add this "prod_anim warmbutt"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_warmhands {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 2"
		tasklist_add this "rotate_toback"
		tasklist_add this "prod_anim warmhands"
		tasklist_add this "prod_anim warmhands"
		tasklist_add this "rotate_tofront"
	}

	proc prodfill_standjog {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 3"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim standjogstart"
		tasklist_add this "prod_anim standjogloop"
		tasklist_add this "prod_anim standjogloop"
		tasklist_add this "prod_anim standjogloop"
		tasklist_add this "prod_anim standjogloop"
		tasklist_add this "prod_anim standjogloop"
		tasklist_add this "prod_anim standjogloop"
		tasklist_add this "prod_anim standjogloop"
		tasklist_add this "prod_anim standjogloop"
		tasklist_add this "prod_anim standjogstop"
		tasklist_add this "prod_anim kneebend"
	}

	proc prodfill_standsleep {} {
		global prodplace
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim standsleepstart"
		tasklist_add this "prod_anim standsleeploop"
		tasklist_add this "prod_anim standsleeploop"
		tasklist_add this "prod_anim standsleeploop"
		tasklist_add this "prod_anim standsleeploop"
		tasklist_add this "prod_anim standsleeploop"
		tasklist_add this "prod_anim standsleeploop"
		tasklist_add this "prod_anim standsleeploop"
		tasklist_add this "prod_anim standsleeploop"
		tasklist_add this "prod_anim standsleepstop"
		tasklist_add this "prod_anim stretch"
	}

	proc bedjump {} {
		global prodplace
		tasklist_add this "walk_dummy $prod_place 1"
		tasklist_add this "rotate_toright"
		tasklist_add this "play_anim brothelb"
		tasklist_add this "play_anim brothelb"
		tasklist_add this "breathe"
	}

	proc prodfill_sitedge {} {
		set place [get_place -center [lreplace [get_pos this] 2 2 16] -rect -4 -2.5 4 0 -except this]
		if {[lindex $place 0]<1} {prodfill_default;return}
		tasklist_add this "walk_pos \{$place\}"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim sitdown_edge"
		set rnd [format "%010o" [irandom 1073741824]]
		for {set i 0} {$i<10} {incr i} {
			if {1&[string index $rnd $i]} {
				tasklist_add this "prod_anim sitedgeloopa"
			} else {
				tasklist_add this "prod_anim sitedgeloopb"
			}
		}
		tasklist_add this "prod_anim standup_edge"
		//tasklist_add this "walk_random"
	}

	proc prodfill_default {} {
		global prodfill_defaultacts
		eval prodfill_[lindex $prodfill_defaultacts [irandom [llength $prodfill_defaultacts]]]
	}

	proc prodfill_takedrugs {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 4"
		tasklist_add this "rotate_toright"
		tasklist_add this "prod_anim takedrugs"
		tasklist_add this "prod_anim laydown"
		tasklist_add this "prod_anim sleepside"
		tasklist_add this "prod_anim sleepside"
		tasklist_add this "prod_anim sleepside"
		tasklist_add this "prod_anim sleepwalk"
		tasklist_add this "prod_anim sleepside"
		tasklist_add this "prod_anim sleepside"
		tasklist_add this "prod_anim sleeptosit"
		tasklist_add this "prod_anim sitfloorstill"
		tasklist_add this "prod_anim sitfloorstill"
		tasklist_add this "prod_anim standup"
		tasklist_add this "prod_anim tired"
	}

	proc prodfill_electrify {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 4"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim electrify"
		tasklist_add this "prod_anim electrify"
		tasklist_add this "prod_anim electrify"
		tasklist_add this "prod_anim hungry"
	}

	proc prodfill_sniffatfood {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 0"
		tasklist_add this "rotate_toright"
		tasklist_add this "prod_anim sniffatfood"
		tasklist_add this "prod_anim sniffatfood"
		tasklist_add this "prod_anim hungry"
	}

	proc prodfill_matrixdojo {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 0"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim matrixdojo"
	}

	proc prodfill_smokepipe {} {
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim smokepipestart"
		tasklist_add this "prod_anim smokepipeloop"
		tasklist_add this "prod_anim smokepipeloop"
		tasklist_add this "prod_anim smokepipeloop"
		tasklist_add this "prod_anim smokepipeloop"
		tasklist_add this "prod_anim smokepipeloop"
		tasklist_add this "prod_anim smokepipestop"
		tasklist_add this "prod_anim cough"
	}

	proc prodfill_tapdrink {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 0"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim tapdrinkstart"
		tasklist_add this "prod_anim tapdrinkloop"
		tasklist_add this "prod_anim tapdrinkloop"
		tasklist_add this "prod_anim tapdrinkloop"
		tasklist_add this "prod_anim tapdrinkstop"
		tasklist_add this "prod_anim wipenose"
	}

	proc prodfill_read {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 7"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim read"
	}

	proc prodfill_cartwheel {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 6"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim cartwheel"
		tasklist_add this "prod_anim cartwheel"
		tasklist_add this "prod_anim cartwheel"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_handstand {} {
		tasklist_add this "prod_anim handstandstart"
		tasklist_add this "prod_anim handstandloop"
		tasklist_add this "prod_anim handstandloop"
		tasklist_add this "prod_anim handstandloop"
		tasklist_add this "prod_anim handstandloop"
		tasklist_add this "prod_anim handstandloop"
		tasklist_add this "prod_anim handstandstop"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_pullup {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 6"
		tasklist_add this "rotate_toleft"
		tasklist_add this "prod_anim pullupstart"
		tasklist_add this "prod_anim pulluploop"
		tasklist_add this "prod_anim pulluploop"
		tasklist_add this "prod_anim pulluploop"
		tasklist_add this "prod_anim pulluploop"
		tasklist_add this "prod_anim pulluploop"
		tasklist_add this "prod_anim pulluploop"
		tasklist_add this "prod_anim pullupstop"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim wipenose"
	}

	proc prodfill_pressup {} {
		tasklist_add this "prod_anim pressupstart"
		tasklist_add this "prod_anim pressuploop"
		tasklist_add this "prod_anim pressuploop"
		tasklist_add this "prod_anim pressuploop"
		tasklist_add this "prod_anim pressuploop"
		tasklist_add this "prod_anim pressuploop"
		tasklist_add this "prod_anim pressuploop"
		tasklist_add this "prod_anim pressuploop"
		tasklist_add this "prod_anim pressuploop"
		tasklist_add this "prod_anim pressupstop"
		tasklist_add this "prod_anim tired"
	}

	proc prodfill_vaultover {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 7"
		tasklist_add this "rotate_toleft"
		tasklist_add this "prod_anim jumpa"
		tasklist_add this "prod_anim vaultover"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_washhands {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 7"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim washhands"
		tasklist_add this "prod_anim washhands"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_taichi {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 1"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim wait"
		tasklist_add this "prod_anim wait"
		tasklist_add this "prod_anim taichi"
		tasklist_add this "prod_anim taichi"
		tasklist_add this "prod_anim taichi"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_jumpfire {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 6"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim wait"
		tasklist_add this "prod_anim jumpa"
		tasklist_add this "prod_anim jumpfire"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_gymanvil {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 1"
		tasklist_add this "rotate_toright"
		tasklist_add this "prod_anim gymanvil"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_carve {} {
		tasklist_add this "rotate_tofront"
		tasklist_add this "change_particlesource this 8 26 {0 0 0} {0 0 0.1} 16 1 0 1"
		tasklist_add this "prod_anim carvestart"
		tasklist_add this "prod_anim carveloop"
		tasklist_add this "set_particlesource this 8 5"
		tasklist_add this "prod_anim carveloop"
		tasklist_add this "set_particlesource this 8 5"
		tasklist_add this "prod_anim carveloop"
		tasklist_add this "set_particlesource this 8 5"
		tasklist_add this "prod_anim carveloop"
		tasklist_add this "set_particlesource this 8 5"
		tasklist_add this "prod_anim carveloop"
		tasklist_add this "set_particlesource this 8 5"
		tasklist_add this "prod_anim carvestop"
		tasklist_add this "prod_anim wait"
		tasklist_add this "free_particlesource this 8"		;// wird evtl. nicht ausgeführt, ist aber besser als nichts :-)
	}

	proc prodfill_knit {} {
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim knitstart"
		tasklist_add this "prod_anim knitloop"
		tasklist_add this "prod_anim knitloop"
		tasklist_add this "prod_anim knitloop"
		tasklist_add this "prod_anim knitloop"
		tasklist_add this "prod_anim knitloop"
		tasklist_add this "prod_anim knitstop"
		tasklist_add this "prod_anim cough"
	}

	proc prodfill_drinktub {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 3"
		tasklist_add this "rotate_toright"
		tasklist_add this "prod_anim drinktubstart"
		tasklist_add this "prod_anim drinktubloop"
		tasklist_add this "prod_anim drinktubloop"
		tasklist_add this "prod_anim drinktubloop"
		tasklist_add this "prod_anim drinktubstop"
	}

	proc prodfill_sitfence {} {
		global prodplace
		//log "!!!Zwerg [get_ref this] prodfill_sitfence"
		//tasklist_add this "walk_dummy $prodplace 3"
		set pos [vector_add [get_pos $prodplace] "-1.5 0.0 0.0"]
		tasklist_add this "walk_pos \{$pos\}"
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim sitfencestart"
		tasklist_add this "prod_anim sitfenceloop"
		tasklist_add this "prod_anim sitfenceloop"
		tasklist_add this "prod_anim sitfenceloop"
		tasklist_add this "prod_anim sitfenceloop"
		tasklist_add this "prod_anim sitfencestop"
		tasklist_add this "prod_anim stretch"
	}

	proc prodfill_boo {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 1"
		tasklist_add this "rotate_toback"
		tasklist_add this "prod_anim boo"
		tasklist_add this "prod_anim boo"
		tasklist_add this "rotate_tofront"
	}

	proc prodfill_cheer {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 2"
		tasklist_add this "rotate_toback"
		tasklist_add this "prod_anim cheer"
		tasklist_add this "rotate_tofront"
	}

	proc prodfill_applaud {} {
		global prodplace
		tasklist_add this "walk_dummy $prodplace 5"
		tasklist_add this "rotate_toback"
		tasklist_add this "prod_anim applaud"
		tasklist_add this "rotate_tofront"
	}

	proc prodfill_stretch {} {
		tasklist_add this "prod_anim stretch"
		tasklist_add this "prod_anim wait"
		tasklist_add this "prod_anim cough"
	}

	proc prodfill_teeter {} {
		tasklist_add this "prod_anim teeter_t"
		tasklist_add this "prod_anim scratchhead"
		tasklist_add this "prod_anim wipenose"
	}

	proc prodfill_scratch {} {
		tasklist_add this "prod_anim scratch"
		tasklist_add this "prod_anim teeter_w"
		tasklist_add this "prod_anim breathe"
	}

	proc prodfill_jumpbend {} {
		tasklist_add this "prod_anim jumpb"
		tasklist_add this "prod_anim kneebend"
		tasklist_add this "prod_anim leftright"
	}

	proc prodfill_impatient {} {
		tasklist_add this "prod_anim listenastart"
		tasklist_add this "prod_anim listenaloop"
		tasklist_add this "prod_anim listenaloop"
		tasklist_add this "prod_anim listenaloop"
		tasklist_add this "prod_anim listenaloop"
		tasklist_add this "prod_anim listenaloop"
		tasklist_add this "prod_anim listenastop"
		tasklist_add this "prod_anim listenbstart"
		tasklist_add this "prod_anim listenbloop"
		tasklist_add this "prod_anim listenbloop"
		tasklist_add this "prod_anim listenbloop"
		tasklist_add this "prod_anim listenbloop"
		tasklist_add this "prod_anim listenbloop"
		tasklist_add this "prod_anim listenbstop"
		tasklist_add this "prod_anim impatient"
	}

	proc prodfill_jumproping {} {
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim wait"
		tasklist_add this "prod_anim jumproping"
		tasklist_add this "prod_anim jumproping"
		tasklist_add this "prod_anim jumproping"
		tasklist_add this "prod_anim jumproping"
		tasklist_add this "prod_anim jumproping"
		tasklist_add this "prod_anim jumproping"
		tasklist_add this "prod_anim jumproping"
		tasklist_add this "prod_anim jumproping"
		tasklist_add this "prod_anim wait"
		tasklist_add this "prod_anim tired"
	}

	proc prodfill_footbaging {} {
		global prodplace
		tasklist_add this "rotate_tofront"
		tasklist_add this "prod_anim wait"
		tasklist_add this "prod_anim footbaga"
		tasklist_add this "prod_anim footbaga"
		tasklist_add this "prod_anim footbagb"
		tasklist_add this "prod_anim footbagc"
		tasklist_add this "prod_anim footbaga"
		tasklist_add this "prod_anim footbaga"
		tasklist_add this "prod_anim footbagc"
		tasklist_add this "prod_anim footbagb"
		tasklist_add this "prod_anim footbagc"
		tasklist_add this "prod_anim footbaga"
		tasklist_add this "prod_anim wait"
	}

	proc prodfill_random {} {
		global prodfill_activities prodplace myref
		set prodplace [obj_query this -boundingbox {-4 -7 -1 4 5 1} -type production -limit 1 -owner own]
		if { $prodplace } {
			set testlist [obj_query $prodplace "-type gnome -range 2"]   ;#suche zwerg der in dessen Nähe ist
			if {[lnand "0 $myref" $testlist]!=""} {
				set prodplace 0
			} elseif { [get_boxed $prodplace] == 1 || [get_buildupstate $prodplace] == 0 } {
				set prodplace 0
			}
		}
		if { $prodplace } {
			set ppclass [get_objclass $prodplace]
			set idx [lsearch -glob $prodfill_activities "$ppclass *"]
			if {$idx==-1} {
				log "WARNING: no prodfill list for $ppclass"
				set prodact ""
			} else {
				set actlist [lindex [lindex $prodfill_activities $idx] 1]
				if {$actlist==""} {
					set prodact ""
				} else {
					set prodact [lindex $actlist [irandom [llength $actlist]]]
				}
			}
		} else {
			set prodact ""
		}
		if {$prodact==""} {
		//	if {[get_posz this]<rand()*5+8} {
				set prodact default
		//	} else {
		//		set prodact sitedge
		//	}
		}
#		log "[get_objname this]-nextactivity: $nextactivity , nr: $idxsel"
		eval prodfill_$prodact
		if {[get_prodautoschedule this]} {tasklist_add this "walk_around"}
	}

}
