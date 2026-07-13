map create 500 200

;# create dummy object !!! don't touch it !!!
sel /obj

generate_color_variation 0 0 200 200 0

set_view 131.78 23.95 2.5 0.22 0.57		;# set inital camera view (x y zoom)

//gametime reset

set_light_begin 33
set_view_begin 11

call data/templates/obw_tutorial.tcl
MapTemplateSet 32 0
call data/templates/obw_wettkampf.tcl
MapTemplateSet 160 0
call data/templates/obw_szene.tcl
MapTemplateSet 288 32

proc s2010 {} {
	set ref [obj_query 0 "-class Trigger_Tutorial -limit 1"]
	if {!$ref} {return}
	set gnome [obj_query 0 "-class Zwerg -owner 0 -limit 1"]
	call_method [obj_query $ref -class Zauntor_a -range 100 -limit 1] oeffnen -1
	set_pos $gnome {110 30.55 12}
	set_view 110 30 1.5 -0.2 0.1
	ref_set $ref tocheck gnome1deselected...
	tasklist_clear $ref
	state_trigger $ref checking
}
proc r2010 {} {
	set ref [obj_query 0 "-class Trigger_Tutorial -limit 1"]
	if {!$ref} {return}
	catch {del [ref_get $ref gnome_ref1]}
	catch {del [ref_get $ref gnome_ref4]}
	ref_set $ref tocheck gnome1deselected...
	tasklist_clear $ref
	state_trigger $ref checking
}
proc s2015 {} {
	set ref [obj_query 0 "-class Trigger_Tutorial -limit 1"]
	if {!$ref} {return}
	sel /obj
	set fire [obj_query $ref -class Feuerstelle -owner 0 -limit 1]
	if {$fire} {
		set_pos $fire {100 30.5 10}
		set_boxed $fire 0
		set_buildupstate $fire 1
		set ps [obj_query $fire -class Pilzstamm -range 3 -limit 1]
		if {!$ps} {
			new Pilzstamm "" [get_pos $fire] {0 0 0}
		}
		set g [obj_query $fire -class Zwerg -owner 0 -range 20 -limit 1]
		if {$g} {set_attrib $g exp_Holz 0.02}
	}
	ref_set $ref tocheck harvested1
	tasklist_clear $ref
	state_trigger $ref checking
}
proc s2020 {} {
	set ref [obj_query 0 "-class Trigger_Tutorial -limit 1"]
	if {!$ref} {return}
	ref_set $ref tocheck zeltunboxed
	tasklist_clear $ref
	state_trigger $ref checking
	set fire [obj_query 0 -class Feuerstelle -owner 0 -limit 1]
	foreach g [obj_query $ref -class Zwerg -owner 0] {
		if {[set idx [inv_find $g Feuerstelle]]!=-1} {
			inv_rem $g $idx
		}
	}
	set_pos $fire {100 30.5 10}
	set_boxed $fire 0
	set_buildupstate $fire 1
	qnew Zelt
}
proc r2020 {} {
	set ref [obj_query 0 "-class Trigger_Tutorial -limit 1"]
	if {!$ref} {return}
	catch {del [ref_get $ref gnome_ref3]}
	catch {del [ref_get $ref gnome_ref4]}
	ref_set $ref tocheck zeltunboxed
	tasklist_clear $ref
	state_trigger $ref checking
}
proc sdig {} {
	set ref [obj_query this "-class Trigger_Tutorial -limit 1"]
	if {!$ref} {return}
	ref_set $ref tocheck gnomehaseaten
	tasklist_clear $ref
	state_trigger $ref checking
	set g [obj_query $ref -class Zwerg -owner 0]
	sel /obj
	set item [new Grillpilz]
	del $item
	ref_set $ref comp_val [list $g [list $item $item]]
}
proc sdoor {} {
	set ref [obj_query 0 -class Trigger_Tutorial -limit 1]
	if {!$ref} {return}
	set door [ref_get $ref door]
	call_method $door oeffnen [get_selectedobject] -1
	set_owner_attrib 0 digenable0 1
}
proc s2022 {} {
	set ref [obj_query 0 -class Trigger_Tutorial -limit 1]
	if {!$ref} {return}
	ref_set $ref tocheck .truereturn.....
	tasklist_clear $ref
	state_trigger $ref checking
}
proc s2500 {} {
	set ref [obj_query 0 -class Trigger_Tutorial -limit 1]
	if {!$ref} {return}
	ref_set $ref tocheck gnomeexper.
	tasklist_clear $ref
	state_trigger $ref checking
	ref_set $ref walktarget [obj_query this -class Trainingspuppe -limit 1]
	ref_set $ref try_increase 0.02
	set gnome [obj_query 0 -class Zwerg -owner 0 -limit 1]
	add_expattrib $gnome exp_F_Kungfu 0.021
	set_pos $gnome {70 42.5 13}
	set_view 70 42 2 -0.2 0
}

proc tr {} {return [obj_query 0 -class {Trigger_Tutorial Trigger_Tournament} -limit 1]}
proc sr {} {return [obj_query 0 -class Sequence_triggered]}

proc s2125 {} {
	set ref [obj_query 0 -class Trigger_Tournament -limit 1]
	if {!$ref} {return}
	ref_set $ref tocheck objectreached
	tasklist_clear $ref
	state_trigger $ref checking
	set fire [obj_query 0 -class Feuerstelle -pos [vector_add [get_pos $ref] {30 0 20}] -range 100 -limit 1]
	ref_set $ref comp_val $fire
	set fpos [vector_add [get_pos $fire] {-3 0 2}]
	set standarte [ref_get $ref standarte1]
	foreach gnome [obj_query $standarte -class Zwerg -owner 0 -range 15] {
		set_pos $gnome $fpos
	}
	foreach gnome [obj_query $standarte -class Zwerg -owner {1 2 3} -range 55] {
		if {$gnome} {del $gnome}
	}
	set_view 188 65 1.5 -0.2 0
}
proc s2130 {} {
	set ref [obj_query this -class Trigger_Tournament -limit 1]
	if {!$ref} {return}
	ref_set $ref tocheck itemstransported
	tasklist_clear $ref
	state_trigger $ref checking
	set brewer [obj_query this -class Brauerei -pos [vector_add [get_pos $ref] {60 0 20}] -range 100 -limit 1]
	set_owner $brewer 0
	ref_set $ref brewery $brewer
	set brewpos [vector_add [get_pos $brewer] {-3 0 3}]
	sel /obj
	for {set i 0} {$i<10} {incr i} {
		new Pilzhut "" $brewpos {0 0 0}
	}
	for {} {$i<15} {incr i} {
		new Pilzstamm "" $brewpos {0 0 0}
	}
	foreach gnome [obj_query $brewer -pos [vector_add [get_pos $ref] {50 50 0}] -class Zwerg -owner 0 -range 100 -limit 2] {
		set_pos $gnome $brewpos
	}
	set_view 228 65 1.5 -0.2 0
}
proc s2140 {} {
	set ref [obj_query this -class Trigger_Tournament -limit 1]
	if {!$ref} {return}
	ref_set $ref tocheck objectreached.
	tasklist_clear $ref
	state_trigger $ref checking
	set ham [obj_query this -class Dummy_Obw_goldhamster -range 300 -limit 1]
	ref_set $ref comp_val $ham
	set gnome [obj_query this -pos {240 60 15} -class Zwerg -owner 0 -range 50 -limit 1]
	set_pos $gnome [vector_add [get_pos $ham] {-11 0 6}]
	set_view 250 62 1.5 -0.2 0
}
proc s2160 {} {
	set ref [obj_query this -class Trigger_Tournament -limit 1]
	if {!$ref} {return}
	ref_set $ref tocheck gnomeneartodeath
	tasklist_clear $ref
	state_trigger $ref checking
	set trib [obj_query 0 -class Dummy_Obw_tribuene -limit 1]
	set gnome [obj_query $trib -class Zwerg -owner 0 -range 200 -limit 1]
	set fpos [vector_add [get_pos $trib] {0 0 6}]
	set_pos $gnome $fpos
	sel /obj
	set gn [new Zwerg "" $fpos {0 0 0}]
	set_owner $gn 2
	set_pos $gn $fpos
	set_attrib $gn atr_Hitpoints 0.8
	set_attrib $gnome atr_Hitpoints 0.7
	set_view 228 42 1.5 -0.2 0
}

proc camp {} {
	set ref [obj_query this -class Trigger_Tournament -limit 1]
	if {!$ref} {return}
	ref_set $ref tocheck gnomeneartodeath
	tasklist_clear $ref
	state_trigger $ref checking
	set trib [obj_query this -class Dummy_Obw_tribuene -limit 1]
	if {$trib} {
		set g [obj_query $trib -class Zwerg -owner 0 -range 200 -limit 1]
		if {$g} {
			set_pos $g [vector_add [get_pos $trib] {0 0 4}]
			set_attrib $g atr_Hitpoints 0.7
			sel /obj
			inv_add $g [new Schwert]
		}
		set g [obj_query $trib -class Zwerg -owner 2 -range 200 -limit 1]
		if {!$g} {
			set g [new Zwerg]
			set_owner $g 2
		}
		set_pos $g [vector_add [get_pos $trib] {2 0 4}]
		set_attrib $g atr_Hitpoints 0.7
		sel /obj
		inv_add $g [new Schwert]
		set_diplomacy 0 2 "enemy"
		set_view 227 29 1.2 -0.1 0.1
	}

}
