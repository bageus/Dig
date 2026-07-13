call scripts/misc/utility.tcl
call scripts/init/animinit.tcl

def_class Steinschleuder wood tool 1 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim steinschleuder.standard
	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this steinschleuder.standard 0 $ANIM_STILL

		set_attrib this weight 0.05
		set_attrib this attackpoints_bal 0.1
	}
}

def_class PfeilUndBogen wood tool 1 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim bogen.standard
	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this bogen.standard 0 $ANIM_STILL

		set_attrib this weight 0.05
		set_attrib this attackpoints_bal 0.12
	}
}

def_class Schild wood tool 1 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim schild.standard
	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this schild.standard 0 $ANIM_STILL

		set_attrib this weight 0.05
		set_attrib this hitpoints 0.5
	}
}
###Die Waffen einer Frau###
def_class Titten none dummy 0 {} {
	def_event evt_dummy
	handle_event evt_dummy {}
	method dummy {} {}
	call scripts/misc/autodef.tcl
	class_defaultanim busen.standard

	obj_init {
		call scripts/misc/autodef.tcl
		set_physic this 0
		set_collision this 1
		set_anim this busen.standard 0 $ANIM_LOOP
	}
}

def_class Schwert metal tool 1 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim schwert.standard
	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this schwert.standard 0 $ANIM_STILL

		set_attrib this weight 0.08
		set_attrib this attackpoints_sr 0.15
	}
}


def_class Metallschild metal tool 2 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim metallschild.standard
	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this metallschild.standard 0 $ANIM_STILL

		set_attrib this weight 0.08
		set_attrib this hitpoints 0.7
	}
}

def_class Buechse metal tool 3 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim buechse.standard
	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this buechse.standard 0 $ANIM_STILL

		set_attrib this weight 0.05
		set_attrib this attackpoints_bal 0.3
	}
}

def_class Lichtschwert energy tool 4 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim lichtschwert.standard
	method get_out {} {
		set_anim this lichtschwert.raus 0 $ANIM_ONCE
	}
	method get_in {} {
		set_anim this lichtschwert.rein 0 $ANIM_ONCE
	}
	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this lichtschwert.standard 0 $ANIM_STILL

		set_attrib this weight 0.1
		set_attrib this attackpoints_sr 0.45
	}
}

def_class Kristallschild metal tool 4 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim kristallschild.standard
	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this kristallschild.standard 0 $ANIM_STILL

		set_attrib this weight 0.05
		set_attrib this hitpoints 1.2
	}
}

SetWeaponClasses {
	{Keule 			keule}
	{Streitkolben	streitkolben}
	{Krumsaebel		krumsaebel}
	{Hellebarde		hellebarde}
	{Lanze_1		lanze_01}
	{Lanze_2		lanze_02}
	{Zauberstab		zauberstab}
	{Dolch_1		dolch_01}
	{Trollschild_1	trollschild_01}
	{Trollschild_2	trollschild_02}
	{Trollschild_3	trollschild_03}
	{Streitaxt		streitaxt}
	{Axt_1			axt_01}
	{Axt_2			axt_02}
	{Axt_3			axt_03}
	{Axt_4			axt_04}
	{Axt_unq_1		axt_unq_01}
	{Axt_unq_2		axt_unq_02}
	{Axt_unq_3		axt_unq_03}
	{Axt_unq_4		axt_unq_04}
	{Schwert_1		schwert_01}
	{Schwert_2		schwert_02}
	{Dolch_2		dolch_02}
	{Schild_1		schild_01}
	{Schild_2		schild_02}
	{Schild_3		schild_03}
	{Schild_unq_1	schild_unq_01}
	{Schild_unq_2	schild_unq_02}
	{Amulett_1		amulett_01}
	{Amulett_2		amulett_02}
	{Amulett_3		amulett_03}
	{Schwert_3		schwert_03}
	{Schwert_4		schwert_04}
	{Drachenschuppe	drachenschild}
	{AK47			ak}
	{MP5			mp5}
	{M4				m4}
	{Para			para}
	{M3_super_90	m3_super_90}
	{Duals			duals}
	{Awp			awp}
	{Deagle			deagle}


}

def_class Bombe fight tool 0 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim bombe.standard

	method activate {status} {
		if {!$status} {
			set bpl [obj_query this -class CS_BombPlace -limit 1 -range 5]
			if { $bpl != 0 } {
    			set_attrib this PilzAge 2.0
    			call_method_static GameObserver ExecBroadcast "sound play c4_plant 1 \{[get_pos this]\};sound playglobal bombpl 2 ;\
    														   newsticker new [net localid] -text \"Bomb has been planted\" "

		proc CWPlayerLose {iPlayer} {
			if {[net localid] == $iPlayer} {
				loadtextwin Lose_DM
			} else {
				newsticker new [net localid] -text "Spieler [expr $iPlayer + 1] ist ausgeschieden"
			}
		}

    			state_triggerfresh this active
    			eval $on_cb
    		}
		} else {
			set_attrib this PilzAge 1.0
			call_method_static GameObserver ExecBroadcast "sound play c4_disarm 1 \{[get_pos this]\};sound playglobal bombdef 2 ;\
														   newsticker new [net localid] -text \"Bomb has been defused\""
			action this wait 0.1 {} {}
			state_trigger this disabled
			eval $def_cb
		}
	}

	method destroy {} {
		del this
	}

	method_const get_status {} {
		set at [get_attrib this PilzAge]
		if { $at < 0.9 } {return "off"}
		if { $at > 1.9 } {return "on"}
		return "def"
	}

	method set_on_callback {code} {
		set on_cb $code
	}

	method set_def_callback {code} {
		set def_cb $code
	}

	method set_exp_callback {code} {
		set exp_cb $code
	}

	state disabled {
		state_disable this
	}

	state active {
		add_attrib this atr_Hitpoints -0.02
		set hp [get_attrib this atr_Hitpoints]

		set b 1
		if { $hp < 0.8 } {
			// beeep
			set b 2
		}
		if { $hp < 0.6 } {
			// beeep beeep
			set b 3
		}
		if { $hp < 0.4 } {
			// beeep beeep beeep
			set b 3
		}
		if { $hp < 0.2 } {
			// beeep beeep beeep beeep beeep
			set b 4
		}
		if { $hp < 0.01 } {
			// kawummmmmm...............................
			set b -1
		}

		if { $b > 0 } {
			call_method_static GameObserver ExecBroadcast "sound play c4_beep$b 2 \{[get_pos this]\}"
		} else {
			call_method_static GameObserver ExecBroadcast "sound playglobal c4_explode1 3 ;\
														   screenvibe 0	0.15 	0.1 	0.16 	100 	0.21 	114"

			create_particlesource this [get_pos this] {0 -0.03 0} 4 1
			eval $exp_cb
			del this
		}

		state_disable this
		action this wait 1.1 {state_enable this}
	}

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this bombe.standard 0 2
		set_attrib this PilzAge 0
		set_attrib this atr_Hitpoints 1.0

		set on_cb "log nueschtdefiniert_on"
		set def_cb "log nueschtdefiniert_defused"
		set exp_cb "log nueschtdefiniert_explode"
	}
}

