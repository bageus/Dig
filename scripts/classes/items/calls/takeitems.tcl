if { [in_class_def] } {

	def_event evt_takeitems

	handle_event evt_takeitems {
		init_take
	}


} else {

	timer_event this evt_takeitems -userid 6 -repeat 0 -attime [expr [gettime] + 0.2]

    proc init_take {} {
    	set transplist [obj_query this "-type transport -boundingbox \{-0.32 -0.32 -0.64 0.32 0.32 0.64\}"]
    	set objlist [obj_query this "-type \{material tool\} -boundingbox \{-0.32 -0.32 -0.64 0.32 0.32 0.64\}"]
    	set boxlist [obj_query this "-flagpos boxed -boundingbox \{-0.32 -0.32 -0.64 0.32 0.32 0.64\}"]
    	set enemylist [obj_query this "-class Wuker -boundingbox \{-0.32 -0.32 -0.64 0.32 0.32 0.64\}"]

    	set objlist [lnand 0 [lor $transplist $objlist $boxlist $enemylist]]
    	
    	//log "TAKEITEMs [get_objname this]: $objlist"

    	foreach item $objlist {
    		set taken "not"
    		set itemclass [get_objclass $item]
    		if {[check_method $itemclass oeffnen]} {continue}
    		if { $itemclass != "Pilz" }	{
    			if { [string range $itemclass 0 5] != "Schatz" || [string length $itemclass] == 10 } {
    				inv_add this $item
    				set_rotx $item 0.0
    				set_rotz $item 0.0
    				set_owner $item [get_owner this]
    				set taken ""
    			}
    		}
    		//log "[get_objname this]: Item found: [get_objname $item] - was $taken taken"
    	}
    }
}
