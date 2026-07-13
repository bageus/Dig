call scripts/init/animinit.tcl

SetDummyClassesAll {noidiobj noviewinfog} {
{ Muetze_a muetze_a }
{ Muetze_b muetze_b }
{ Voodoo_Muetze_a voodoo_muetze_a }
{ Voodoo_Muetze_b voodoo_muetze_b }
{ Knocker_Muetze_a knocker_helm_a }
{ Knocker_Muetze_b knocker_helm_b }
{ Brain_Muetze_a brain_muetze_a }
{ Brain_Muetze_b brain_muetze_b }
{ Brain_Brille brain_brille}
{ Vampir_Muetze_a vampir_kaputze_a }
{ Vampir_Muetze_b vampir_kaputze_b }

{ Scientist_haare brain_brille }
{ Voodoo_haare_a voodoo_haare_a }
{ Voodoo_haare_b voodoo_haare_b }
{ Wuerfel wuerfel }
{ Wuerfelbecher wuerfelbecher }
{ Joint_a joint_a }
{ Joint_b joint_b }
{ Joint_c joint_c }
{ Krone krone }
{ Bigbart bigbart }
{ Trompete trompete}


{ Muetze_dienstleistung_a muetze_dienstleistung_a }
{ Muetze_energiemagie_a muetze_energiemagie_a }
{ Muetze_kampf_01_a muetze_kampf_01_a }
{ Muetze_kampf_02_a muetze_kampf_02_a }
{ Muetze_kampf_03_a muetze_kampf_03_a }
{ Muetze_metall_a muetze_metall_a }
{ Muetze_holz_a muetze_holz_a }
{ Muetze_stein_a muetze_stein_a }
{ Muetze_stein_aus_a muetze_stein_aus_a }
{ Muetze_nahrung_a muetze_nahrung_a }
{ Muetze_arbeitslos_a muetze_arbeitslos_a }

{ Muetze_dienstleistung_b muetze_dienstleistung_b }
{ Muetze_energiemagie_b muetze_energiemagie_b }
{ Muetze_kampf_01_b muetze_kampf_01_b }
{ Muetze_kampf_02_b muetze_kampf_02_b }
{ Muetze_kampf_03_b muetze_kampf_03_b }
{ Muetze_metall_b muetze_metall_b }
{ Muetze_holz_b muetze_holz_b }
{ Muetze_stein_b muetze_stein_b }
{ Muetze_nahrung_b muetze_nahrung_b }
{ Muetze_arbeitslos_b muetze_arbeitslos_b }
{ Koenigsschlafmuetze koenigsschlafmuetze }

}

def_class Zipfelmuetze energy material 2 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim muetze_a.standard

	method set_parameters {gender name worktime expmax attribs age clan} {
		global m_gender m_name m_worktime m_expmax m_attribs m_age ANIM_STILL

		set m_gender   $gender
		set m_name     $name
		set m_worktime $worktime
		set m_expmax   $expmax
		set m_attribs  $attribs
		set m_age      $age
		log "Zipfelmuetze got $m_gender $m_name $clan"
		switch ${m_gender}$clan {
			"male0" {set manim muetze_a}
			"male1" {set manim voodoo_muetze_a}
			"male2" {set manim knocker_helm_a}
			"male3" {set manim brain_muetze_a}
			"male4" {set manim vampir_kaputze_a}
			"female0" {set manim muetze_b}
			"female1" {set manim voodoo_muetze_b}
			"female2" {set manim knocker_helm_b}
			"female3" {set manim brain_muetze_b}
			"female4" {set manim vampir_kaputze_b}
			default {set manim muetze_a}
		}
		set_anim this $manim\.standard 0 $ANIM_STILL
		set_objname this $m_name\s[lmsg "Muetze"]
	}

	method get_gender {} {
		global m_gender
		return $m_gender
	}

	method get_name {} {
		global m_name
		return $m_name
	}

	method get_worktime {} {
		global m_worktime
		return $m_worktime
	}

	method get_expmax {} {
		global m_expmax
		return $m_expmax
	}

	method get_attribs {} {
		global m_attribs
		return $m_attribs
	}

	method get_age {} {
		global m_age
		return $m_age
	}

	method get_anim {} {
		global manim
		return $manim
	}

	method from_wall {} {
		set x [get_posx this]
    	set y [get_posy this]
    	set z [get_posz this]
		set bboxz [lindex [get_negbbox this] 2]
   		set checkpoint [expr $z + $bboxz - 1.0]
		set hmappoint [get_hmap $x $y]
		if {$hmappoint > $checkpoint} {
			set diff [expr $hmappoint - $checkpoint]
  			set_posz this [expr $z + $diff]
   		 }
	}

	// Zerstörung der Totenmütze nach einer festgesetzten Zeit
	// Falls Zerstörung nicht sicher möglich ist (weil Mütze im Inventory ist z.B.) wird die
	// Frist zur Zerstörung verlängert

	def_event evt_timer_destruction
	handle_event evt_timer_destruction {
		if {[is_contained this]} {
			// Neuer Versuch: alle 20 Minuten	(20*60 = 1200 Sekunden)
			timer_event this evt_timer_destruction -repeat 1 -interval 1 -userid 0 -attime [expr [gettime]+600]
			log "Zipfelmütze could not be destroyed, will try again later"
			return
		}

		create_particlesource 6 [get_pos this] {0 -0.1 0.1} 8 1
		set_physic this 0
		set_hoverable this 0
		set_storable this 0
		set_viewinfog this 0
		set_forceipol this 1
		set_vel this {0 0.03 0}
		log "Zipfelmütze about to be destroyed!"
		timer_event this evt_timer_deletion -repeat 1 -interval 1 -userid 0 -attime [expr [gettime]+15]
	}

	def_event evt_timer_deletion
	handle_event evt_timer_deletion {
		log "Zipfelmütze destroyed!"
		del this
	}


	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this muetze_a.standard 0 $ANIM_STILL

		// Zerstörung der Zipfelmütze nach 60 Minuten (60 * 60 = 3600 Sekunden)
		timer_event this evt_timer_destruction -repeat 1 -interval 1 -userid 0 -attime [expr [gettime]+3600]
		log "Zipfelmütze created!"

		set_attrib this nutrivalue 0
		set_attrib this weight 0.05
		set m_name "Niemand"
		set manim muetze_a
	}
}

// Taucherglocke, vom Spieler baubar

def_class Taucherglocke metal tool 2 {} {
	call scripts/misc/autodef.tcl
	class_defaultanim taucherglocke_a.standard

	method use {user} {
		if {[is_contained this]} {
			if {[inv_find_obj $user this] >= 0} {
				if {![call_method $user is_wearing_divingbell]} {
					tasklist_add $user "wear_divingbell 1"
				} else {
					tasklist_add $user "remove_divingbell 1"
				}
			}
		}
	}

	method set_ownergender gender {
		if {$gender == "male"} {
			set_anim this taucherglocke_a.standard 0 $ANIM_STILL
		} elseif {$gender == "female"} {
			set_anim this taucherglocke_b.standard 0 $ANIM_STILL
		} else {
			log "WARNING!!! -  taucherglocke.tcl : set_ownergender : illegal gender"
		}
	}

	method let_it_look_like_darth_vaders {} {
		set_anim this darth_vader.standard 0 $ANIM_STILL
	}

	obj_init {
		call scripts/misc/autodef.tcl
		set_anim this taucherglocke_a.standard 0 $ANIM_STILL

		set_attrib this weight 0.1
		set_attrib this hitpoints 0.5
	}
}
