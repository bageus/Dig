call scripts/misc/utility.tcl

def_class Grabstein stone production 1 {} {

	def_event evt_btn_on
	handle_event evt_btn_on {}
	
	method prod_items {} {return ""}
	
	method set_standardanim {} {
		global standard_anim ANIM_LOOP
		set_anim this $standard_anim 0 $ANIM_LOOP
	}
	
	method init {} {
		log "Grabstein init passed"
		global m_anim mutze
		sel /obj
		set mutze [new Dummy_Muetze_a]
		set_anim $mutze $m_anim\.standard 0 0
		link_obj $mutze this 0
		//set_selectable this 0
		//set_owner this -1
		set_prodautoschedule this 0
		set_prod_enabled this 0
		set_prod_directevents this 1
	}
	
	method set_m_anim {anim} {
		set m_anim $anim
	}

	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericprod.tcl

	class_defaultanim grabstein.typ_a
	class_defaultanim grabstein.typ_b
	class_defaultanim grabstein.typ_c
	class_defaultanim grabstein.typ_d
	class_flagoffset 0.0 -3.2

	obj_init {
		call scripts/misc/genericprod.tcl
		set typnr [irandom 4]
		set standard_anim grabstein.typ_[string index abcd $typnr]
		set_objvariation this $typnr

		log "STANIM: $standard_anim"
		set_anim this $standard_anim 0 $ANIM_LOOP
		set_energyconsumption this 0
		set_collision this 1

		set build_dummys [list 6 2 3 3]
		set max_buildup_step [llength $build_dummys]
		set buidup_anis {unten_rechtsstein unten_linksstein unten_rechtsstein unten_linksstein}
		set damage_dummys {2 6}
		
		set m_anim muetze_a
	}
	
	obj_exit {
		catch {del $mutze}
	}
}

