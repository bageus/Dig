call scripts/init/animinit.tcl



if {[in_class_def]} {
# class definition part
	
//	def_attrib [get_class_name] 0 10000 0
	call scripts/misc/animclassinit.tcl

	method change_owner {new_owner} {
//		add_owner_attrib [get_owner this] [get_objclass this] -1
		set_owner this $new_owner
//		add_owner_attrib [get_owner this] [get_objclass this] 1
	}

} else {
# obj_init part

	set_viewinfog this 1
	set_storable this 1
	set_physic this 1
	
}
