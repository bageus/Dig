//# STOPIFNOT FULL
//odin.tcl

def_class Hand_Gottes none monster 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericfight.tcl

	set_class_anim	takeclocka			odinshand.uhr_nehmen_a
	set_class_anim	takeclockb			odinshand.uhr_nehmen_b
	set_class_anim	startclock			odinshand.uhr_start
	set_class_anim	clockring			odinshand.wecken
	set_class_anim	tabnose				odinshand.nase_tippen
	set_class_anim	gesturea			odinshand.geste_a
	set_class_anim	gestureb			odinshand.geste_b
	set_class_anim	gesturebloop		odinshand.geste_b_loop
	set_class_anim	gesturec			odinshand.geste_c
	set_class_anim	gesturecloop		odinshand.geste_c_loop
	set_class_anim	gestured			odinshand.geste_d
	set_class_anim	gesturee			odinshand.geste_e
	set_class_anim	gesturef			odinshand.geste_f
	set_class_anim	gestureg			odinshand.geste_g
	set_class_anim	gesturegloop		odinshand.geste_g_loop
	set_class_anim	gestureend			odinshand.geste_ende
	set_class_anim	gestureendloop		odinshand.geste_ende_loop

	class_defaultanim odinshand.stand

	method idle_anim {} {
		// absichtlich leer!
	}

	obj_init {
		set_anim this odinshand.stand 0 $ANIM_STILL
		set_viewinfog this 0
		set_hoverable this 0

	}

}

def_class Odin none monster 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericfight.tcl

	class_defaultanim odin.d_2170_warten

 	method idle_anim {} {
		// absichtlich leer!
	}

	obj_init {

		set_anim this odin.d_2170_warten 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0

	}

}
def_class Odin_Walhalla none monster 0 {} {
	call scripts/misc/animclassinit.tcl
	call scripts/misc/genericfight.tcl

	class_defaultanim odin.d_2170_einweg

 	method idle_anim {} {
		// absichtlich leer!
	}

	obj_init {

		set_anim this odin.d_2170_einweg 0 $ANIM_LOOP
		set_viewinfog this 0
		set_hoverable this 0

	}

}

