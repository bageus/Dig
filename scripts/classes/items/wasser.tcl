def_class Wasser none dummy 0 {} {
	call scripts/misc/info_obj.tcl

	obj_init {
		proc init_fsource {} {
			global undefined

			set_water 1
			set undefined -10000

			set volume [get_info volume]
			set vpf [get_info vpf]
			set height [get_info height]

			set_fsource this -pos {0 -1 0} -type water -vpf $vpf -volume $volume -height $height
		}

		call scripts/misc/info_obj.tcl
		if { [get_mapedit] } {init_fsource}												;# für Mapeditor
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]		;# für Spiel
	}

	def_event evt_timer0
	handle_event evt_timer0 {
		init_fsource
	}
}

def_class WasserTrans none dummy 0 {} {
	call scripts/misc/info_obj.tcl
	def_event evt_fillstop

	obj_init {
		proc init_fsource {} {
			global undefined type target waterfall

			set_water 1

			set type [get_info type]

			if { $type != $undefined } {

				if { $type == "in" } {

					set wt [obj_query this "-class WasserTrans -limit 1 -range 30"]
					if { $wt != 0 } {
				    	state_triggerfresh this idle
				    	set target $wt
				    	log "WasserTrans connected ([get_ref this]) -> ($wt)"
				    }
				    //set_fstopper this {0 1} {-5 5} 0
				    set_fstopper this {-3 0} {3 1} 0
				    //set_fsource this -pos {0 -1 0} -type water -vpf -9999
				    set waterfall [get_info waterfall]

				} elseif { $type == "out" } {
                	set_fsource this -pos {0 -1 0} -type water -vpf 0 -volume [get_info volume]
				}
    			//set volume [get_info volume]
    			//set vpf [get_info vpf]
    			//set height [get_info height]

    			//set_fsource this -pos {0 -1 0} -type water -vpf $vpf -volume $volume -height $height

    			//state_triggerfresh this idle
    		}
		}

		call scripts/misc/info_obj.tcl
		//init_fsource																	;# für Mapeditor
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]		;# für Spiel

		set undefined -10000
		set type $undefined
		set waterfall $undefined
	}

	def_event evt_timer0
	handle_event evt_timer0 {
		init_fsource
	}

	handle_event evt_fillstop {
		set_fsource this -vpf 0
		set vol [get_fsource this -volume]
		if { $vol == 0 } {
			//log "[get_ref this] : del"
			//del
		}
		create_particlesource 24 [get_pos this] {0 -0.15 3} 4 1
	}

	method out_fill {m} {
		set_fsource this -vpf [expr $m * [get_info vpf]]
		timer_event this evt_fillstop -repeat 0 -userid 0 -attime [expr [gettime]+1]
	}

	method get_type {} {
		return $type
	}

	state idle {
		set ownpos [get_pos this]
		set pos [vector_add $ownpos {0 -2 0}]

        set fill [get_fill [lindex $pos 0] [lindex $pos 1]]
        //log "[get_ref this] : fill=$fill"
        if { $fill > 0 } {
        	if { $target != 0 } {
            	if { ![obj_valid $target] } {
            		set target 0
            		return
            	}
            	call_method $target out_fill $fill
            }
        	if { $waterfall != $undefined } {
        		set partic 0
            	foreach item $waterfall {
            		if { $fill >= [random 0.3] || [irandom 7] == 1 } {
            			set pos [vector_add $item $ownpos]
            			set pos [vector_add $pos {0 -1 0}]
            			set pos [lreplace $pos 2 2 13]
            			create_particlesource 22 $pos {0 0 0} 4 1
            			set partic 1
            		}
            	}
            	if { $partic } {
          			create_particlesource 24 [vector_add $ownpos {0 -1 0}] {0 0 3} 8 1
            	}
            }
      	}

		state_disable this
	    action this wait 0.5 { state_enable this }
	}
}


def_class Sulfur none dummy 0 {} {
	call scripts/misc/info_obj.tcl

	obj_init {
		proc init_fsource {} {
			global undefined

			set_water 1
			set undefined -10000

			set volume [get_info volume]
			set vpf [get_info vpf]
			set height [get_info height]

			set_fsource this -pos {0 -1 0} -type sulfur -vpf $vpf -volume $volume -height $height
		}

		call scripts/misc/info_obj.tcl
		if { [get_mapedit] } {init_fsource}												;# für Mapeditor
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]		;# für Spiel
	}

	def_event evt_timer0
	handle_event evt_timer0 {
		init_fsource
	}
}

def_class Lava none dummy 0 {} {
	call scripts/misc/info_obj.tcl

	obj_init {
		proc init_fsource {} {
			global undefined

			set_water 1
			set undefined -10000

			set volume [get_info volume]
			set vpf [get_info vpf]
			set height [get_info height]

			set_fsource this -pos {0 -1 0} -type lava -vpf $vpf -volume $volume -height $height
		}

		call scripts/misc/info_obj.tcl
		if { [get_mapedit] } {init_fsource}												;# für Mapeditor
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]		;# für Spiel
	}

	def_event evt_timer0
	handle_event evt_timer0 {
		init_fsource
	}
}

def_class Wasserstopper none dummy 0 {} {
	obj_init {
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		set_anim this tuer_verlies.standard 0 0
		set_fstopper this {0 -2} {0 1} 0
		set type ""
	}
	def_event evt_timer0
	handle_event evt_timer0 {
		set_posz this 15.0
		set_posx this [hf2i [expr [get_posx this] + 0.5]]
		set_posy this [expr [hf2i [get_posy this]] + 0.5]
	}
}

def_class Wasserabsaug none dummy 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		set_anim this tuer_verlies.standard 0 0
		set vpf [get_info vpf]
		if { $vpf == 0 } {
			set vpf -9999
		}
		set_fsource this -pos {0 -0.2 0} -type water -vpf $vpf
	}
}

def_class WasserstopperH none dummy 0 {} {
	obj_init {
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		#set_anim this tuer_verlies.standard 0 0
		set_fstopper this {-3 0} {3 1} 0
		set type ""
	}
	def_event evt_timer0
	handle_event evt_timer0 {
		set_posz this 15.0
		set_posx this [hf2i [expr [get_posx this] + 0.5]]
		set_posy this [expr [hf2i [get_posy this]] + 0.5]
		set_fstopper this {-3 0} {3 1} 0
		set_rotz this 1.57
	}
}

def_class WasserstopperV none dummy 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		#set_anim this tuer_verlies.standard 0 0
		set_fstopper this {0 -2} {0 1} 0
		set type ""
	}
	def_event evt_timer0
	handle_event evt_timer0 {
		set_posz this 15.0
		set_posx this [hf2i [expr [get_posx this] + 0.5]]
		set_posy this [expr [hf2i [get_posy this]] + 0.5]
		set_fstopper this {0 -2} {0 1} 0
	}
}


def_class WasserabsaugTitanic metal protection 2 {} {
	call scripts/misc/autodef.tcl
	//call scripts/misc/info_obj.tcl

	def_event evt_timer0
	handle_event evt_timer0 {
		// aufs Grid ziehen
		if {[get_boxed this] == 0} {
//			log "Drawing drain to grid..."
			set_posz this 15.0
			set_posx this [expr [hf2i [get_posx this]] + 0.5]
			set_posy this [expr [hf2i [get_posy this]] + 0.5]
		}
	}


	def_event evt_timer_update
	handle_event evt_timer_update {
		if {[get_boxed this] || $active == 0 } {
			return
		}

//		if {[isunderwater [vector_add [get_pos this] {-1 -0.01 0}]] ||  [isunderwater [vector_add [get_pos this] {1 -0.01 0}]]} {
		if {[isunderwater [vector_add [get_pos this] {0 -0.01 0}]]} {
//			log "Gulli arbeitet!"
			set_particlesource this 0 1
			set_particlesource this 1 1
		} else {
			set_particlesource this 0 0
			set_particlesource this 1 0
		}
	}


	method start {val} {
		set active 1
		set_fsource	this -vpf -1.0 -type water -pos {0 -0.01 0}
	}

	set_class_anim abfluss gulli.standard
	class_defaultanim gulli.standard
	class_physcategory 3

	obj_init {
		call scripts/misc/autodef.tcl
		//call scripts/misc/info_obj.tcl
		set_anim this gulli.standard 0 $ANIM_STILL

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
   		timer_event this evt_timer_update -repeat -1 -interval 1 -userid 0

		change_particlesource this 0 7 {0 1 -0.2} {0 0 0.1} 256 8 0
		change_particlesource this 1 21 {0 1 -0.2} {0 0 0} 64 4 0
		set_particlesource this 0 0
		set_particlesource this 1 0

		set_attrib this weight 0.1
		set_attrib this hitpoints 0.5

		set_hoverable this 0
		set_selectable this 0

		set active 0

	}
}



def_class GulliSuper none dummy 0 {} {
	call scripts/misc/info_obj.tcl
	obj_init {
		call scripts/misc/info_obj.tcl
		set_collision this 0
		set_selectable this 0
		set_hoverable this 0

		set vpf [get_info vpf]
		if { $vpf == 0 } {
			set vpf -9999
		}
		set_fsource this -pos {0 -0.2 0} -vpf $vpf
	}
}


// Abflüsse sichtbar, vom Spieler baubar

def_class Abfluss metal protection 2 {} {
	call scripts/misc/autodef.tcl
	call scripts/misc/genericprod.tcl

	def_event evt_timer0
	handle_event evt_timer0 {
		// aufs Grid ziehen
		if {[get_boxed this] == 0} {
//			log "Drawing drain to grid..."
			set_posz this 15.0
			set_posx this [expr [hf2i [get_posx this]] + 0.5]
			set_posy this [expr [hf2i [get_posy this]] + 0.5]
		}
	}


	def_event evt_timer_update
	handle_event evt_timer_update {
		if {[get_boxed this]} {
			return
		}

//		if {[isunderwater [vector_add [get_pos this] {-1 -0.01 0}]] ||  [isunderwater [vector_add [get_pos this] {1 -0.01 0}]]} {
//		if {[isunderwater [vector_add [get_pos this] {0 -0.01 0}]]} {
		if {[isunderwater [vector_add [get_pos this] {0 -0.02 0}]]  ||  [isunderwater [vector_add [get_pos this] {0 -1.02 0}]]} {
//			log "Gulli arbeitet!"
			set_particlesource this 0 1
			set_particlesource this 1 1
		} else {
			set_particlesource this 0 0
			set_particlesource this 1 0
		}
	}


	set_class_anim abfluss gulli.standard
	class_defaultanim gulli.standard
	class_physcategory 3

	obj_init {
		call scripts/misc/autodef.tcl
		call scripts/misc/genericprod.tcl
		set_anim this gulli.standard 0 $ANIM_STILL

		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
   		timer_event this evt_timer_update -repeat -1 -interval 1 -userid 0

		change_particlesource this 0 7 {0 1 -0.2} {0 0 0.1} 256 8 0
		change_particlesource this 1 21 {0 1 -0.2} {0 0 0} 64 4 0
		set_particlesource this 0 0
		set_particlesource this 1 0

		set_attrib this weight 0.1
		set_attrib this hitpoints 0.5

		set_fsource	this -vpf -1.0 -pos {0 -0.01 0}		;# -type water
	}
}

