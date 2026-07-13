
def_class GnomeStartPoint  none info 0 {} {

	obj_init {
		set GnomeID -1
		set GnomeName "noname"

		if { [get_mapedit] } {
			set_anim this trigger_fahne2.standard 0 0
		}


		proc TransferToGnome {} {
			global GnomeID GnomeName
			transfer_attribs this $GnomeID
			set_objname $GnomeID $GnomeName
    		set_attrib $GnomeID atr_Hitpoints 	1.0
    		set_attrib $GnomeID atr_Nutrition 	1.0
    		set_attrib $GnomeID atr_Alertness 	1.0
    		set_attrib $GnomeID atr_Mood 		1.0
		}

		proc TransferFromGnome {} {
			global GnomeID GnomeName
			transfer_attribs $GnomeID this
			set GnomeName [get_objname $GnomeID]
		}

		set iTOwner -1

		proc AskServer {meth} {
    		set GO [obj_query this -class GameObserver -owner 0]
    		if { $GO == 0 } {
    			log "GnomeStartPoint: Error no GameObserver found !!"
    			return -1
    		}
            return [call_method $GO $meth [get_ref this]]
		}

		set CWTeam -1
		proc CWInit {iTeam} {
			global CWTeam
			set CWTeam $iTeam
		}
	}

	method CWInit {iTeam} {
		CWInit $iTeam
	}

	method destroy {} {
		log "***************************************destr"
		catch { del this }
	}

	method change_owner {iNewOwner} {
		log "***************************************chowner $iNewOwner"
		set_owner this $iNewOwner
	}

	handle_event evt_timer0 {

	}

	method SetTargetOwner {iOwner} {
		global iTOwner
		set iTOwner iOwner
	}

	method StartGame {} {
		global GnomeID GnomeName GnomeOwner iTOwner

		;# neuen Zwerg erzeugen
		set GnomeID [new Zwerg]
		set_pos $GnomeID [get_pos this]
		set_autolight $GnomeID 1

		;# Namen merken oder setzen
		if {$GnomeName=="noname"} {
			log "Initial gnome Create"
			TransferFromGnome
		} else {
			log "Transfering to Gnome"
			TransferToGnome
		}

		if { $iTOwner == -1 } {
			set iTOwner [get_owner this]
		}

		set_owner $GnomeID $iTOwner

		if { $CWTeam != -1 } {
			call_method $GnomeID set_counterwiggle [expr $CWTeam + 1]
		}

		call_method $GnomeID init
		call_method $GnomeID add_logoff_code "call_method [get_ref this] DieNotify"
	}

	method DieNotify {} {
		if { [obj_valid $GnomeID] } {
			TransferFromGnome
		}
	}

	method ShutGame {} {
		global GnomeID

		;# alten Zwerg löschen
		if { [obj_valid $GnomeID] } {
			TransferFromGnome
			call_method $GnomeID destroy
			set GnomeID -1
		}
	}
}

def_class CS_HostagePoint none info 0 {} {

	obj_init {
		set HostageID -1

		if { [get_mapedit] } {
			set_anim this trigger_fahne2.standard 0 0
		}
	}

	method StartGame {} {
		global HostageID

		;# neue Geisel erzeugen
		set HostageID [new Geisel]
		set_pos $HostageID [get_pos this]
		set_owner $HostageID -1
		return $HostageID
	}

	method DieNotify {} {
		if {obj_valid $HostageID} {
		}
	}

	method ShutGame {} {
		global HostageID

		;# alte Geisel löschen
		if { [obj_valid $HostageID] } {
			call_method $HostageID destroy
			set HostageID -1
		}
	}
}

def_class CS_RescueZone none info 0 {} {
    class_defaultmaterial modelnozwrite
	obj_init {
		if { [get_mapedit] } {
			set_anim this rescuepoint.standard 0 0
		}
		set Range 5
	}

	method StartGame {} {
		set_anim this rescuepoint.standard 0 0
	}

	method DieNotify {} {
	}

	method ShutGame {} {
	}

	method GetRange {} {
		global Range
		return $Range
	}
}

def_class CS_BombPlace none info 0 {} {
    class_defaultmaterial modelnozwrite
	obj_init {
		if { [get_mapedit] } {
			set_anim this bombenplatz.standard 0 0
		}
	}

	method StartGame {} {
		set_anim this bombenplatz.standard 0 0
	}

	method DieNotify {} {
	}

	method ShutGame {} {
	}
}

def_class CS_Bomb none info 0 {} {
	obj_init {
   		set ObjID -1
		if { [get_mapedit] } {
			set_anim this bombe.standard 0 2
		}
	}

	method StartGame {} {
		set ObjID [new Bombe]
		set_pos $ObjID [get_pos this]
		set_owner $ObjID -1
		return $ObjID
	}

	method DieNotify {} {
	}

	method ShutGame {} {
		if { [obj_valid $ObjID] } {
			call_method $ObjID destroy
			set ObjID -1
		}
	}
}

def_class CF_FlagPoint none info 0 {} {
	obj_init {
   		set ObjID -1
		if { [get_mapedit] } {
			set_anim this capturetheflag.wehen 0 2
		}
	}

	method StartGame {} {
		set ObjID [new Flagge]
		set_pos $ObjID [get_pos this]
		set_owner $ObjID [get_owner this]
		call_method $ObjID StartGame

		return $ObjID
	}

	method DieNotify {} {
		call_method $ObjID DieNotify
	}

	method ShutGame {} {
		call_method $ObjID ShutGame
		if { [obj_valid $ObjID] } {
			call_method $ObjID destroy
			set ObjID -1
		}
	}
}

def_class CF_Ecke none info 0 {} {
	obj_init {
		set_anim this ecke_verlies_a.standard 0 0
	}

	method StartGame {} {
	}

	method DieNotify {} {
	}

	method ShutGame {} {
	}

	method destroy {} { del this }
}

def_class Flagge metal tool 0 {} {
	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this capturetheflag.wehen 0 2
		set_attrib this PilzAge 0
		set_attrib this atr_Hitpoints 1.0

		set_hoverable this 0
		set_selectable this 0

		set ObjID -1
		set CallbackID -1
		set OrgPos {0 0 0}

		proc wait_time {time} {
    		state_disable this
    		action this wait $time {state_enable this}
		}
	}
	method StartGame {} {
		state_triggerfresh this scan
		set ObjID -1
		set OrgPos [get_pos this]
	}

	method DieNotify {} {
	}

	method ShutGame {} {
		state_trigger this disabled
		state_disable this
		set ObjID -1
	}

	method destroy {} { del this }

	state scan {

		// Catched
		if { $ObjID != -1 } {
			if { [obj_valid $ObjID] } {
				set rp [obj_query this -class CF_FlagRescuePoint -owner [get_owner $ObjID] -range 2]
				set fl [obj_query this -class Flagge -owner [get_owner $ObjID] -range 2]

				if { $rp != 0 && $fl != 0 } {
					log "zurueck!!!!"
					call_method_static GameObserver ExecuteAt 0 "call_method this FlagCallback [get_owner $ObjID]"
					state_trigger this disabled
					state_disable this
					return
				}

				wait_time 0.5
				return
			} else {
				set ObjID -1
			}
		}

		// Free
		set zl [obj_query this -class Zwerg -owner other -range 2]
		if { $zl != 0 } {
			set gnome [lindex $zl 0]
			link_obj this $gnome 7
			set ObjID $gnome
		}

		// Back
		set zl [obj_query this -class Zwerg -owner own -range 2]
		if { $zl != 0 } {
			set_pos this $OrgPos
			link_obj this
		}

		wait_time 0.5
	}

	state disabled {
		log "Warning: Flagge entered illegal state"
		state_disable this
	}
}

def_class CF_FlagRescuePoint none info 0 {} {
	obj_init {
   		set ObjID -1
		if { [get_mapedit] } {
			set_anim this rescuepoint.standard 0 0
		}
	}

	method StartGame {} {
		set ObjID [new CF_Ecke]
		set_pos $ObjID [get_pos this]
		set_owner $ObjID [get_owner this]
		set_anim this rescuepoint.standard 0 0
		return $ObjID
	}

	method DieNotify {} {
	}

	method ShutGame {} {
		if { [obj_valid $ObjID] } {
			call_method $ObjID destroy
			set ObjID -1
		}
	}

}

set cs_obj_list {Kleiner_Heiltrank Heiltrank Grosser_Heiltrank Unverwundbarkeitstrank Unsichtbarkeitstrank Grillpilz Grillhamster \
				 AK47 MP5 M4 Para M3_super_90 Duals Awp Deagle}

foreach item $cs_obj_list {

	set defcode {
    	obj_init {
    		set ObjID -1
    		set FassID -1

    		if { [get_mapedit] } {
    			set_anim this tonne.ani 0 0
    		}
    	}
    	method StartGame {} {
    		global ObjID FassID

    		;# neues item erzeugen

    		set FassID [new Schatztonne]
    		set_pos $FassID [get_pos this]
    		set_owner $FassID -1

    		set ObjID [new item]
    		set_pos $ObjID [get_pos this]
    		set_owner $ObjID -1
    	}
    	method DieNotify {} {
    	}

    	method destroy {} { del this }

    	method ShutGame {} {
    		global ObjID FassID

    		;# altes item löschen
    		if { [obj_valid $ObjID] } {
    			del $ObjID
    			set ObjID -1
    		}
    		if { [obj_valid $FassID] } {
    			del $FassID
    			set FassID -1
    		}

    	}
    }

    set defcode [string map "item $item" $defcode]
    def_class CS_OP_$item none info 0 {} $defcode

}


