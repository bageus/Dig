;# allgemeine hilfsfunktionen
call scripts/misc/animclassinit.tcl

proc get_civ_state {owner} {
	//get gnome list
	set gnomes [obj_query 0 -class Zwerg -owner $owner -cloaked 1]
	if {$gnomes==0} {set gnomes {}}
	//calculate sum of all exp of all gnomes
	set exp_sum 0
	foreach gnome $gnomes {
		set gnome_exp 0
		foreach attribut [get_expattrib] {
			fincr gnome_exp [get_attrib $gnome $attribut]
		}
		
		//scaling [0, MIN_EXP] with weight 1 and [MIN_EXP, MAX_EXP] with weight 1
		if {$gnome_exp > 1} {
			fincr exp_sum 1.0
			set gnome_exp [expr {$gnome_exp-1.0}]
			//MAX_EXP scaling
			fincr exp_sum [expr {$gnome_exp/(2.0-1.0)}]
		} else {
			//MIN_EXP scaling
			fincr exp_sum [expr {$gnome_exp/1.0}]
		}
	}
	//population scaling
	set exp_sum [expr {(22.0/22.0*$exp_sum)}]
	
	return [expr {($exp_sum+[gamestats numbuiltprodclasses $owner])*0.01}]
}
;# generator der innerhalb eines bandes eine zufallszahl generiert
proc randomize {base variance} {
	set temp [expr $base*$variance]
	set lbound [random [expr $base-$temp]]
	set ubound [random [expr $base+$temp]]
	logdebug "randomize: [random $lbound $ubound]"
	return [hfloor [expr [random $lbound $ubound]+0.5]]
}


;# gibt fuer alle gameobjecte gueltige eigenschaften aus
proc general_object_dump object {
	log "dump_object [get_objclass $object] $object [get_objname $object]"
	log "lock: [get_lock $object]"
	log "owner: [get_owner $object]"
	log "is contained: [is_contained $object]"
	log "current_state: [state_get $object]"
	log "inventorylist: [inv_cnt $object] [inv_list $object]"
	log "tasklist: [tasklist_cnt $object] [tasklist_list $object]"
	log "position: [get_pos $object]"
	log "physic: [get_physic $object]"
	log "view in fog: [get_viewinfog $object]"
	log "light: [get_light $object]"
	log "autolight: [get_autolight $object]"
	log "climbability:  [get_climbability $object]"
	log "selectable: [get_selectable $object]"
	foreach name [get_attrib_names] {
		log "$name: [get_attrib $object $name]"
	}
}

proc autodef_SimpleItem {ItemName AnimName {bMaterial 0}} {
	def_attrib $ItemName 0 10000 0
	def_class $ItemName none material 0 {} "
		call scripts/misc/animclassinit.tcl
		if { $bMaterial } {
			call scripts/classes/items/calls/resources.tcl
		}
		class_disablescripting
		class_defaultanim $AnimName
		method change_owner {new_owner} {
			add_owner_attrib \[get_owner this\] \[get_objclass this\] -1
			set_owner this \$new_owner
			add_owner_attrib \[get_owner this\] \[get_objclass this\] 1
		}
		obj_init \"
    		if { $bMaterial } {
    			call scripts/classes/items/calls/resources.tcl
    		}
			set_anim this $AnimName 0 \$ANIM_STILL
			set_viewinfog this 1
			set_storable this 1
			set_physic this 1
			set_attrib this weight 0.08
			set_hoverable this 1
		\"
	"
}
