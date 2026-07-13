call scripts/misc/utility.tcl

def_class _Weiche_1 fight material 1 {} {}
def_class _Weiche_2 fight material 1 {} {}
def_class _Weiche_3 fight material 1 {} {}
def_class _Weiche_4 fight material 1 {} {}
def_class _Kamera   fight material 1 {} {}


// Dampflore, für das Lorenbahn - Puzzle

def_class Dampflore metal production 2 {} {
	call scripts/misc/animclassinit.tcl	
	call scripts/misc/genericprod.tcl
	
	def_event evt_btn_on
	handle_event evt_btn_on {
		global trackswitch_1 trackswitch_2 trackswitch_3 trackswitch_4 signallist
		
		update_camera
		set trackswitch_1 [expr {![get_prod_slot_cnt this _Weiche_1]}]
		set trackswitch_2 [expr {![get_prod_slot_cnt this _Weiche_2]}]
		set trackswitch_3 [expr {![get_prod_slot_cnt this _Weiche_3]}]
		set trackswitch_4 [expr {![get_prod_slot_cnt this _Weiche_4]}]
		set valuelist "$trackswitch_1 $trackswitch_2 $trackswitch_3 $trackswitch_4"

		for {set i 0} {$i < 4} {incr i} {
			set j [lindex $signallist $i]
			if {$j > 0} {
				call_method $j set_trackswitch [lindex $valuelist $i]
 
			}
		}
	}
		

	method prod_item_actions {itemtobuild} {
		return [list]
	}
	
	
	method activate {} {
		global is_destroyed
		
		if {$is_destroyed} {
			return
		}
		
		set_selectedobject [get_ref this]
		set_prod_slot_cnt this _Kamera 1
		if {$position != "init"} {
			remove_fog
		}
		update_camera
	}
	
	// schalte Weiche Nummer (1-4) um
	
	method set_trackswitch {number value} {
		global trackswitch_1 trackswitch_2 trackswitch_3 trackswitch_4 signallist
		
		set var "trackswitch_$number"
		set $var $value
		
		set var "_Weiche_$number"
    	set_prod_slot_cnt this $var [expr !$value]

		if {[lindex $signallist $number] != 0} {
			call_method [lindex $signallist $number] set_trackswitch $value
		}
	}
	
	
	method set_power {new_power} {
		global power waggonlist
//		log "Dampflore: power changed to $new_power"
		
		if {$power == 0  &&  $new_power == 1} {
			set power $new_power
			call_method this go
		} 

		set power $new_power
		if {$new_power == 1} {
			set_anim this dampflore.fahren 0 $ANIM_LOOP
			set_particlesource this 0 1
    	   	set_forceipol this 1
	    	foreach waggon $waggonlist {
	   			call_method $waggon set_ismoving 1
	   			set_forceipol $waggon 1
	       	}			
		} else {
			set_anim this dampflore.standanim 0 $ANIM_LOOP
			set_particlesource this 0 0
	    	foreach waggon $waggonlist {
	   			call_method $waggon set_ismoving 0
	       	}			
		}
	}
	
	
	method go {} {
		global waggonlist varlist position power 
		global trackswitch_1 trackswitch_2 trackswitch_3 trackswitch_4
		global $varlist

		if {$power == 0  &&  $position != "init"} {
			log "Dampflore stop"
			set_particlesource this 0 0
			return
		}
		
		set waypoints [list]
		switch $position {
			init	{ set waypoints [concat $wp_init_start]; set position start }
			start	{ set waypoints [concat $wp_start_x1 $wp_x1_x2]; set position x2 }
			x2      { if {$trackswitch_1 == 0} {
					  	  set waypoints [concat $wp_x2_x3 $wp_x3_d $wp_to_a $wp_a_x1 $wp_x1_x2]; set position x2
					  } else {
					      set waypoints [concat $wp_x2_b $wp_to_e $wp_e_x4]; set position x4
					  }
				  	}
			x3      { if {$trackswitch_1 == 0} {
					  	  set waypoints [concat $wp_x3_x2 $wp_x2_x1 $wp_x1_a $wp_to_q $wp_q_x9 $wp_x9_r $wp_to_n $wp_n_x7]; set position x7
					  } else {
					      set waypoints [concat $wp_x3_c $wp_to_h $wp_h_x5 $wp_x5_i $wp_to_f $wp_f_j $wp_to_a $wp_a_x1 $wp_x1_x2]; set position x2
					  }
				  	}
			x4      { if {$trackswitch_2 == 0} {
					  	  set waypoints [concat $wp_x4_x5 $wp_x5_i $wp_to_f $wp_f_j $wp_to_a $wp_a_x1 $wp_x1_x2]; set position x2
					  } else {
					      set waypoints [concat $wp_x4_g $wp_to_b $wp_b_x2 $wp_x2_x1 $wp_x1_a $wp_to_q $wp_q_x9 $wp_x9_r $wp_to_n $wp_n_x7]; set position x7  
					  }
				  	}
			x5      { if {$trackswitch_2 == 0} {
					  	  set waypoints [concat $wp_x5_x4 $wp_x4_e $wp_to_k $wp_k_x6]; set position x6
					  } else {
					      set waypoints [concat $wp_x5_h $wp_to_c $wp_c_x3 $wp_x3_d $wp_to_a $wp_a_x1 $wp_x1_x2]; set position x2
					  }
				  	}
			x6      { if {$trackswitch_3 == 0} {
					  	  set waypoints [concat $wp_x6_x7 $wp_x7_n $wp_to_r $wp_r_x9]; set position x9
					  } else {
					      set waypoints [concat $wp_x6_l $wp_to_o $wp_o_x8]; set position x8
					  }
				  	}
			x7      { if {$trackswitch_3 == 0} {
					  	  set waypoints [concat $wp_x7_x6 $wp_x6_k $wp_to_p $wp_p_x8 $wp_x8_o $wp_to_d $wp_d_x3]; set position x3
					  } else {
					      set waypoints [concat $wp_x7_m $wp_to_j $wp_j_f $wp_to_i $wp_i_x5]; set position x5
					  }
				  	}
			x8      { if {$trackswitch_4 == 0} {
					  	  set waypoints [concat $wp_x8_x9 $wp_x9_r $wp_to_n $wp_n_x7]; set position x7
					  } else {
					      set waypoints [concat $wp_x8_p $wp_to_l $wp_l_x6 $wp_x6_k $wp_to_p $wp_p_x8 $wp_x8_o $wp_to_d $wp_d_x3]; set position x3
					  }
				  	}
			x9      { if {$trackswitch_4 == 0} {
					  	  set waypoints [concat $wp_x9_x8 $wp_x8_o $wp_to_d $wp_d_x3]; set position x3
					  } else {
					      set waypoints [concat $wp_x9_q $wp_to_s $wp_s_final1]; set position final1
					  }
				  	}
			final1  { set waypoints [concat $wp_final1_end]; set position final2
					  change_particlesource this 1 3 {0 -0.5 0} {0 0 0} 64 1 0
					  set_particlesource this 1 1
					  sound play lore_crash 1
				  	}
			final2	{ log "End of Way reached." 
					  set position end
					  destroy
					 }
					  
			default { log "WARNING: Lorenbahn position variable has illegal value: $position"; set position end }
		}
//    	log "waypoints:  $waypoints"
//   	log "waggonlist: $waggonlist" 
    	
		action this lore "-waypoints \{ $waypoints \} -trailers \{ $waggonlist \} -gravity 1 -friction 0.05" {log "Lore action finished; position: $position"; if {$position != "end"  && $position != "start"} {call_method this go}} {}
	}
   	

    method init {} {
    	global origin_x_001 origin_y_001 origin_x_002 origin_y_002 origin_x_003 origin_y_003 origin_x_004 origin_y_004
    	global trackswitch_1 trackswitch_2 trackswitch_3 trackswitch_4 power position
    	global minspeed_default maxspeed_default varlist
    	global waggonlist
    	
    	set_prod_slot_cnt this _Weiche_1 1
    	set_prod_slot_cnt this _Weiche_2 1
    	set_prod_slot_cnt this _Weiche_3 1
    	set_prod_slot_cnt this _Weiche_4 1
    	set_prod_slot_cnt this _Kamera	1
    	
    	// Koordinaten der Templates ermitteln
    	
   		set templatelist [obj_query this "-class Info_Lore_Waypoint"]

    	foreach template $templatelist {
			set templatenumber [call_method $template get_info "template"]
			set var "origin_x_$templatenumber"
			set $var [get_posx $template]
			set var "origin_y_$templatenumber"
			set $var [get_posy $template]
			call_method $template destroy
    	}

    	// Weichen Ausgangsstellung
    	// 0 - Durchfahrt
    	// 1 - Abbiegen
    
    	set trackswitch_1 0
    	set trackswitch_2 0
    	set trackswitch_3 0
    	set trackswitch_4 0
    
    	set minspeed_default 2
    	set maxspeed_default 4
    	
    	set power 0
		set_owner this 2
		set_fogofwar this 0 0		
    	
    	// Position: eine der Positionen, an denen die Bahn abbiegen bzw. anhalten kann
    	// init	  start   end  x2  x3  x4  x5  x6  x7  x8  x9
    	
    	set position init
    	
    	find_waggons
		find_signals
    	
//        find_waypoints	             ;// nur für Erzeugung der Wegpunktliste
    	set_waypoints
    	
		catch {	sm_add_event Lorenbahn_geloest }
    	
    	call_method this go
    }
   	
   
   	def_event evt_timer0
   	handle_event evt_timer0 {
   		set templatelist [obj_query this "-class Info_Lore_Waypoint"]
   		if {[llength $templatelist] == 4} {
   			log "Dampflore: Found all 4 Templates - now starting init..."
   	   		call_method this init
   	   	} else {
   	   		log "Dampflore: Could not find all 4 Templates (found [llength $templatelist] so far), will try again later"
			timer_event this evt_timer0 -repeat 0 -attime [expr [gettime]+3]
   	   	}
   	}
   	

   	def_event evt_timer1
   	handle_event evt_timer1 {
   		global camera_is_following
   		if {![is_selected this]} {
			update_camera
		}
   	}   	

	obj_init { 
		call scripts/misc/autodef.tcl
		call scripts/misc/genericprod.tcl
		call scripts/misc/file_wrapper.tcl 		


		// kümmert sich um die Verfolger-Kamera

		proc update_camera {} {
			global camera_is_following
	   		if {[is_selected this]  &&  [get_prod_slot_cnt this _Kamera]} {
   				if {$camera_is_following == 0} {
   					set_camerafollow this 1.75
   					set camera_is_following 1 
   				}
   			} else {
   				if {$camera_is_following == 1} {
   					set_camerafollow -1
   					set camera_is_following 0
   				}
   			}
		}

		
		
		// sucht in der Nähe der Dampflore nach Waggons und hängt sie an
		
		proc find_waggons {} {
		    global waggonlist
		    set currentdistance 1.5
		    
		    set waggonlist [list]
		    foreach waggon [obj_query this "-class Lorenwaggon -range 20"] {
				if {$waggon == 0} {break;}
		        lappend waggonlist "$waggon $currentdistance"	
		        set currentdistance [expr $currentdistance + 1.5]
		    }
		    log "Dampflore: found Waggons: $waggonlist"
		}
		
		
		// sucht nach den Signalen für die Lorenbahn
		
		proc find_signals {} {
			global signallist
			
			set signallist ""
			set s [obj_query this "-class Weichensignal_1 -limit 1"]
			lappend signallist $s 
			set s [obj_query this "-class Weichensignal_2 -limit 1"]
			lappend signallist $s 
			set s [obj_query this "-class Weichensignal_3 -limit 1"]
			lappend signallist $s 
			set s [obj_query this "-class Weichensignal_4 -limit 1"]
			lappend signallist $s 
		}
		
		
		
		// diese Funktion dient nur der Erstellung des Wegpunkt-Files und sollte später nicht mehr benötigt werden
		// alle Lore_Waypoint - Infoobjekte werden im Logger mit Koordinaten, Rotation und Infotexten ausgegeben
		// die Wegpunktdatei muß dann im Texteditior sinnvoll zusammenkopiert werden :-)
		
		proc find_waypoints {} {
			log "Dumping Waypoints ---------------------------------------------------------------"
		
			set x_origin 16
			set y_origin 128
		
			set info_obj_list [obj_query this "-type info -range 100"]
			set waypointlist [list]
			foreach item $info_obj_list {
				if { "Lore" == [lindex [split [get_objname $item] "_"] 1] } {
					if { "Waypoint" == [lindex [split [get_objname $item] "_"] 2] } {
						log "[expr [get_posx $item] - $x_origin] [expr [get_posy $item] - $y_origin] [get_posz $item]    [get_rot $item]   0 0   [call_method $item get_info "way"] [call_method $item get_info "nr"]"
						call_method $item destroy
					}
				}
			}
			log "done ----------------------------------------------------------------------------"
		
			return $waypointlist
		}	
		
		
		proc remove_fog {} {
			global fog_removed fog_remover_list
			
			log "Dampflore: removing fog of war"
			if {$fog_removed} {
				return
			}
			set fog_removed 1
			set fog_remover_list ""
			global origin_x_001 origin_y_001 origin_x_002 origin_y_002 origin_x_003 origin_y_003 origin_x_004 origin_y_004

			set i [new FogRemover "" "[expr $origin_x_001 + 24] [expr $origin_y_001 + 13] 10" "0 0 0"]
			call_method $i fog_remove_timed 0 10 10 0
			lappend fog_remover_list $i
			set i [new FogRemover "" "[expr $origin_x_001 + 52] [expr $origin_y_001 + 18.5] 4" "0 0 0"]
			call_method $i fog_remove_timed 0 28 12 1
			lappend fog_remover_list $i

			set i [new FogRemover "" "[expr $origin_x_003 + 24] [expr $origin_y_003 + 8] 9" "0 0 0"]
			call_method $i fog_remove_timed 0 25 18 16	
			lappend fog_remover_list $i
			set i [new FogRemover "" "[expr $origin_x_003 + 54] [expr $origin_y_003 + 18] 7" "0 0 0"]
			call_method $i fog_remove_timed 0 20 10 24
			lappend fog_remover_list $i
			set i [new FogRemover "" "[expr $origin_x_003 + 72.75] [expr $origin_y_003 + 21] 11" "0 0 0"]
			call_method $i fog_remove_timed 0 17 10 32
			lappend fog_remover_list $i

			set i [new FogRemover "" "[expr $origin_x_002 + 17] [expr $origin_y_002 + 10.5] 14.5" "0 0 0"]
			call_method $i fog_remove_timed 0 20 10 40
			lappend fog_remover_list $i
			set i [new FogRemover "" "[expr $origin_x_002 + 37] [expr $origin_y_002 + 12] 7" "0 0 0"]
			call_method $i fog_remove_timed 0 22 5 48
			lappend fog_remover_list $i
			set i [new FogRemover "" "[expr $origin_x_002 + 32] [expr $origin_y_002 + 8.5] 4" "0 0 0"]
			call_method $i fog_remove_timed 0 12 5 56
			lappend fog_remover_list $i

			set i [new FogRemover "" "[expr $origin_x_004 + 33] [expr $origin_y_004 + 10] 10" "0 0 0"]
			call_method $i fog_remove_timed 0 24 10 64
			lappend fog_remover_list $i
		}
				
		
		// Liest die Wegpunktdatei aus und speichert jede Teilstrecke in einer globalen Listenvariable
		// Fehler in der Wegpunktdatei werden NICHT abgefangen sondern führen zu Fehlern im Skript!!!
		
		proc set_waypoints {} {
			global origin_x_001 origin_y_001 origin_x_002 origin_y_002 origin_x_003 origin_y_003 origin_x_004 origin_y_004
			global minspeed_default maxspeed_default varlist
			
			set varlist [list]
			log "Dampflore: setting waypoints..."
			set wpfile [open {data\scripts\classes\story\lorenbahn_waypoints.txt}]
			while {[set command [gets $wpfile]] != -1} {
		//		log "command $command"
				if {[lindex $command 0] == "wp"} {
					set varname "wp_[lindex $command 1]_[lindex $command 2]"
		//			log "varname: $varname"
					lappend varlist $varname
					global $varname
					set $varname [list]
					set origin_x origin_x_[lindex $command 3]
					set origin_y origin_y_[lindex $command 3]
				} elseif {[llength $command] == 0} {
		//			log "blank line"
				} elseif {[lindex $command 0] == "end"} {
		//			log "end waypoint"
		//			log "waypointlist: $varname: [expr $$varname]"
				} else {
					set point [list]
					lappend point "[expr $$origin_x + [lindex $command 0]] [expr $$origin_y + [lindex $command 1]] [lindex $command 2]"
					lappend point "[lindex $command 3] [lindex $command 4] [lindex $command 5]"
		
					if {[lindex $command 6] == 0} {
						lappend point $minspeed_default
					} else {
						lappend point [expr $minspeed_default + [lindex $command 6]]
					}
		
					if {[lindex $command 7] == 0} {
						lappend point $maxspeed_default
					} else {
						lappend point [expr $maxspeed_default + [lindex $command 7]]
					}
		
		//			log "point: $point"
					lappend $varname $point			
				}
				
				if {[eof $wpfile] == 1} {
					break;
				}
			}
			
		//	log "varlist: $varlist"
			close $wpfile
			log "Dampflore: setting waypoints done!"
		}
	
	
		proc destroy {} {
			global camera_is_following waggonlist fog_remover_list is_destroyed
			
	  		free_particlesource this 0
			set posx [get_posx this]
			set posy [get_posy this]
			set posz [get_posz this]
				  
			foreach waggon $waggonlist {
				call_method $waggon set_isempty 1
			}
					  
			// Kohle!!!!
			for {set i 0} {$i < 40} {incr i} {
				sel /obj
				set_pos [new Kohle] "[expr $posx + 1 + [random 6.0]] $posy [expr $posz + 0.5 + [random 4.0]]"
			}
					  
			set_hoverable this 0
			set_selectable this 0
			set_anim this dampflore.standanim 0 0
			if {[is_selected this]} {
				set_selectedobject -1
			}

	   		if {$camera_is_following == 1} {
   				set_camerafollow -1
   				set camera_is_following 0
		  	}
		  	
		  	foreach obj $fog_remover_list {
				del $obj
		  	}
		  	
			catch {	sm_set_event Lorenbahn_geloest }		  	
		  	set is_destroyed 1
		}
	
		
   		set camera_is_following 0
		set fog_removed 		0
		set is_destroyed 		0
		set fog_remover_list	""
		set signallist 			""
		set waggonlist 			""

		set_anim this dampflore.standanim 0 $ANIM_LOOP
			
		timer_event this evt_timer0 -repeat 0 -attime [expr [gettime]+1]
		timer_event this evt_timer1 -interval 1 -repeat -1
					
		set_physic this 0
		set_hoverable this 0
		set_selectable this 0
		
		set_prod_switchmode this 1
		set_prod_materialneed this 0
		set_prod_schedule this 0
		set_prodautoschedule this 0
		set_prod_enabled this 0
		set_prod_directevents this 1
		set_fogofwar this 0 0		
		
		set_forceipol this 1

		// Rauch
		change_particlesource this 0 11 {0 -0.5 0} {0 0 0} 128 4 0 4
		set_particlesource this 0 0
	}
}



def_class Lorenwaggon metal dummy 2 {} {
	call scripts/misc/autodef.tcl

	method set_ismoving {bool} {
		global ANIM_LOOP
		
		if {$bool} {
			set_anim this anhaenger_b.fahren 0 $ANIM_LOOP
		} else {
			set_anim this anhaenger_b.standard 0 $ANIM_LOOP
		} 
	}
	
	method set_isempty {bool} {
		if {$bool} {
			set_anim this anhaenger.standard 0 $ANIM_LOOP
		} else {
			set_anim this anhaenger_b.standard 0 $ANIM_LOOP
		} 		
	}

	obj_init { 
		call scripts/misc/autodef.tcl
		set_anim this anhaenger_b.standard 0 $ANIM_LOOP
		set_physic this 0
		set_hoverable this 0
		set_selectable this 0
		set_attrib this hitpoints 1
		set_forceipol this 1
	}
}


def_class Weichensignal_1 metal transport 2 {} {
	call scripts/misc/animclassinit.tcl

	method set_trackswitch {value} {
		global trackswitch
		
		set trackswitch $value
		log "Signal: switched to $value"
		
		if {$trackswitch == 0} {
			set_anim this lore_schild_a.standard 0 $ANIM_STILL
		} else {
			set_anim this lore_schild_a.still 0 $ANIM_STILL
		}
	}

	obj_init { 
		call scripts/misc/animclassinit.tcl	

		set_anim this lore_schild_a.standard 0 $ANIM_STILL
	
		global trackswitch
		set trackswitch 0
					
		set_physic this 0
		set_hoverable this 0
		set_selectable this 0
		set_owner this 2
	}
}



def_class Weichensignal_2 metal transport 2 {} {
	call scripts/misc/animclassinit.tcl

	method set_trackswitch {value} {
		global trackswitch
		
		set trackswitch $value
		log "Signal: switched to $value"
		
		if {$trackswitch == 0} {
			set_anim this lore_schild_b.standard 0 $ANIM_STILL
		} else {
			set_anim this lore_schild_b.still 0 $ANIM_STILL
		}
	}

	obj_init { 
		call scripts/misc/animclassinit.tcl	
		set_anim this lore_schild_b.standard 0 $ANIM_STILL
	
		set trackswitch 0
			
		set_physic this 0
		set_hoverable this 0
		set_selectable this 0
		set_owner this 2						
	}
}


def_class Weichensignal_3 metal transport 2 {} {
	call scripts/misc/animclassinit.tcl

	method set_trackswitch {value} {
		global trackswitch
		
		set trackswitch $value
		log "Signal: switched to $value"
		
		if {$trackswitch == 0} {
			set_anim this lore_schild_c.standard 0 $ANIM_STILL
		} else {
			set_anim this lore_schild_c.still 0 $ANIM_STILL
		}
	}

	obj_init { 
		call scripts/misc/animclassinit.tcl	
		set_anim this lore_schild_c.standard 0 $ANIM_STILL
	
		set trackswitch 0
			
		set_physic this 0
		set_hoverable this 0
		set_selectable this 0
		set_owner this 2
	}
}



def_class Weichensignal_4 metal transport 2 {} {
	call scripts/misc/animclassinit.tcl

	method set_trackswitch {value} {
		global trackswitch
		
		set trackswitch $value
		log "Signal: switched to $value"
		
		if {$trackswitch == 0} {
			set_anim this lore_schild_d.standard 0 $ANIM_STILL
		} else {
			set_anim this lore_schild_d.still 0 $ANIM_STILL
		}
	}

	obj_init { 
		call scripts/misc/animclassinit.tcl	
		set_anim this lore_schild_d.standard 0 $ANIM_STILL

		set trackswitch 0

		set_physic this 0
		set_hoverable this 0
		set_selectable this 0
		set_owner this 2
	}
}
