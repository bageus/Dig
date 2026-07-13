	// Allgemeines für die (produzierbaren) Tueren


	method prod_item_actions item {
		return [list]
	}


	def_event evt_btn_on
    handle_event evt_btn_on {
		global undefined

        // falls die Tür abgebaut werden soll: öffnen
		if {[get_prod_pack this] == 1} {
			set_prod_slot_cnt this _Automatisch 0
			set_prod_slot_cnt this _Offen 10
	        set_prod_slot_cnt this _Verschlossen 0
            set_doorproperties this open
            log "[get_objname this] scheduled for packing... opening up!"
            return
    	} else {
    	    if {[get_prod_enabled this] != 0} {
    	        set_prod_enabled this 0
    	    }
    	}

        // Falls alles aus: zuletzt geklicktes einschalten
        if {[get_prod_slot_cnt this _Offen] == 0  &&  [get_prod_slot_cnt this _Verschlossen] == 0  &&
            [get_prod_slot_cnt this _Automatisch] == 0} {
            set_prod_slot_cnt this [event_get this -text2] 10
        }

		if {[get_prod_slot_cnt this _Offen] != 0} {
			set undefined open
		} elseif {[get_prod_slot_cnt this _Verschlossen] != 0} {
			set undefined closed
		} elseif {[get_prod_slot_cnt this _Automatisch] != 0} {
			set undefined openforfriends
		}
		set_doorproperties this $undefined
    }



	def_event evt_timer0
	handle_event evt_timer0 {
		// Tür aufs Grid ziehen
		if {[get_boxed this] == 0} {
			log "Drawing door to grid..."
			set_posz this 15.0
			set_posx this [hf2i [expr [get_posx this] + 0.5]]
			set_posy this [expr [hf2i [get_posy this]] + 0.5]
		}
		call_method this init

		;// pathfinding-Einfluss
		create_doorlogic this
		set_doorproperties this openforfriends
	}


	method Editor_Set_Info {ifo} {
		global info_string
		set info_string $ifo
	}


	method get_info {name} {
		global info_string undefined
		foreach item $info_string {
			set inam [lindex $item 0]
			set ival [lindex $item 1]
			if { $name == $inam } {
				return $ival
			}
		}
		return $undefined
	}


	// Init - Methode für Unpackfrombox

	method init {} {
    	set_collision this 1
		set_prod_enabled this 0
		set_prod_slot_cnt this _Automatisch 10
		set_prod_slot_cnt this _Offen 0
        set_prod_slot_cnt this _Verschlossen 0
		set_doorproperties this openforfriends
	}

