// Gesichteranimationen der Zwerge

if {[in_class_def]} {

	def_event evt_fanim_enable

	handle_event evt_fanim_enable {
		enable_auto_fanim
	}

	method start_fanim {fanimname params} {
		start_fanim_sequence $fanimname $params
	}
	method set_special_feeling_fanim {feeling} {
		disable_auto_fanim
		set_fanim_feeling $feeling
	}
	method reset_fanim_feeling {} {
		enable_auto_fanim
	}
	method disable_fanim_feeling {} {
		disable_auto_fanim
	}
	method set_eyefocus {ref} {
		set_eye_focus $ref
	}
	method update_eyefocus {} {
		set current_common_fanim [subst \$fanim_$current_common_mood]
		adjust_eye_focus
		set_fanim $current_common_fanim
	}

} else {

	set current_fanim {0 0}
	set current_common_fanim {0 0}
	set current_common_mood "normal_normal"
	set auto_fanim_state 1
	set eye_focus 0
	set is_counterwiggle 0
	set trap_mode 0

	// Nummerierung:
	// 		Nr.		Gesichter:				Augen:
	// 		 0		normal					normal
	// 		 1		angewiedert				Angestrengt
	// 		 2		Angst					Angst
	// 		 3		boese					erstaunt
	// 		 4		eingeschnappt			fies
	// 		 5		fies					gluecklich
	// 		 6		fies2					leer
	// 		 7		Weinen					muede
	// 		 8		Zunge					schlaefrig
	// 		 9		A_I						schlafen
	// 		10		C_D						skeptisch
	// 		11		E						Sonnenbrille
	// 		12		F_V						wach
	//		13		M_B						zugekniffen
	//		14		O_U						wach_links
	// 		15		W_Q						wach_rechts
	//		16,17							wach_oben, wach_unten
	//		18-21							normal_l,r,o,u
	//		22-25							schlaefrig_l,r,o,u
	//		26-29							muede_l,r,o,u
	//		l r o u							links rechts oben unten (jenachdem)

	// Definition der Gesichtsausdrücke:
	set fanim_bad_awake				{3 12} ;# 00
	set fanim_bad_normal			{3 0}
	set fanim_bad_dizzy				{3 8}
	set fanim_bad_tired				{3 7}
	set fanim_bad_sleep				{3 9}
	set fanim_normal_awake			{0 12}
	set fanim_normal_normal			{0 0}
	set fanim_normal_dizzy			{0 8}
	set fanim_normal_tired			{0 7}
	set fanim_normal_sleep			{0 9}
	set fanim_good_awake			{6 12}
	set fanim_good_normal			{6 0}
	set fanim_good_dizzy			{6 8}
	set fanim_good_tired			{6 7}
	set fanim_good_sleep			{6 9} ;# 24
	set fanim_normal_sceptical		{0 10} ;
	set fanim_normal_astounded		{0 3}

	set fanim_fight_v0				{2 4}
	set fanim_fight_v1              {3 4}
	set fanim_fight_v2              {5 4}
	set fanim_fight_v3              {6 4}
	set fanim_fight_flee            {2 2}

	// Definitionen von Gesichtsanimationen (frameweise)
	set fanim_happy_event			{{0 0} {10 3} {11 3} {9 12} {9 5} {9 5} {11 5} {10 3}}

	// Definitionen von Fülleranimationen
	// Blinzeln
	set fanim_awake_blinzeln		{12 12 12 9 12 12 12 12 12 12 12 9 12 12 12 12 12 12 12 9}
	set fanim_normal_blinzeln		{0 0 0 9 0 0 0 0 0 0 0 9 0 0 0 0 0 0 0 9}
	set fanim_dizzy_blinzeln		{8 8 8 9 8 8 8 8 8 8 8 9 8 8 8 8 8 8 8 9}
	set fanim_tired_blinzeln		{7 7 7 9 7 7 7 7 7 7 7 9 7 7 7 7 7 7 7 9}
	// Pfeifen
	set fanim_face_pfeifen			{14 15 15 14}
	// Zunge
	set fanim_face_Zunge_lange		{8 8 8 8 8 8}
	set fanim_face_Zunge_kurz		{8 8 8}

	// Definition der Verwendung der Füller (Name, erlaubte Stimmungen, Häufigkeit in 100 Sekunden)
	set fanim_filler_sequences [list]
	lappend fanim_filler_sequences [list mood_eyes_blinzeln {00 01 02 03 10 11 12 13 20 21 22 23} 9.0]
	// lappend fanim_filler_sequences [list face_pfeifen {10 11 20 21 22} 2.0]
	// lappend fanim_filler_sequences [list face_Zunge_lange {04 14 24} 5.0]
	// lappend fanim_filler_sequences [list face_Zunge_kurz {00 01 02 03 10 11 12 13 20 21 22 23} 2.0]

	proc random_fanim_sequence {} {
		global current_common_mood fanim_filler_sequences
		set mood [string map {bad 0 normal 1 good 2 _ "" awake 0 dizzy 2 tired 3 sleep 5} $current_common_mood]
		set rnd [random 100.0]
		set sum 0.0
		foreach seq $fanim_filler_sequences {
			if {[lsearch [lindex $seq 1] $mood]==-1} {continue}
			fincr sum [lindex $seq 2]
			if {$rnd<$sum} {
				start_fanim_sequence [lindex $seq 0]
				break
			}
		}
	}
	proc set_fanim {varlist} {
		global current_fanim current_common_mood is_counterwiggle trap_mode
		if {$trap_mode} {return}
		set i 3 ; set j 0
		foreach varia $varlist {
			if {$varia=="face"} {
				continue
			}
			if {$varia!="eyes"} {
				if {[string is alpha $varia]} {
					set alert [lindex [split $current_common_mood "_"] 1]
					if {$alert!="sleep"} {
						set add [string map {awake 0 normal 4 dizzy 8 tired 12} $alert]
						set add [expr {14+$add}]
						switch $varia {
							"r" {incr add 1}
							"o" {incr add 2}
							"u" {incr add 3}
						}
						set varia $add
					}
				}
				if {!($i==3&&$is_counterwiggle)} {
					set_textureanimation this $i $varia
					set current_fanim [lreplace $current_fanim $j $j $varia]
				}
			}
			incr i ; incr j
		}
	}
	proc start_fanim_sequence {fanimname args} {
		global current_common_fanim current_common_mood is_counterwiggle trap_mode
		if {$trap_mode} {return}
		set submesh "both" ; set startframe 0 ; set animtype 1
		if {[llength $args]==1} {set args [join $args]}
		foreach param $args {
			if {[string index $param 0]=="-"} {set cmd [string trim $param "-"];continue}
			switch $cmd {
				"mesh" {set submesh $param}
				"type" {set animtype $param}
				"frame" {set startframe $param}
			}
		}
		//log "$submesh, $startframe, $animtype ($fanimname)"
		if {[llength $fanimname]>1||[string is integer $fanimname]} {
			set framelist $fanimname
		} else {
			if {[string range $fanimname 0 3]=="mood"} {
				switch [string range $fanimname 5 8] {
					"eyes" {
						set submesh "eyes"
						set fanimname [string replace $fanimname 0 8 [lindex [split $current_common_mood "_"] 1]]
					}
					"face" {
						set submesh "face"
						set fanimname [string replace $fanimname 0 8 [lindex [split $current_common_mood "_"] 0]]
					}
					"both" {
						set submesh "both"
						set fanimname [string replace $fanimname 0 8 ${current_common_mood}]
					}
				}
			} elseif {[string range $fanimname 0 3]=="face"} {
				set submesh "face"
				set fanimname face_[string range $fanimname 5 end]
			} elseif {[string range $fanimname 0 3]=="eyes"} {
				set submesh "eyes"
				set fanimname eyes_[string range $fanimname 5 end]
			}
			global fanim_$fanimname
			set framelist [subst \$fanim_$fanimname]
		}
		//log "$fanimname -> $framelist"
		set facelist [list]
		set eyeslist [list]
		set framecount 0
		foreach entry $framelist {
			if {$submesh=="face"} {set faceframe $entry} {set faceframe [lindex $entry 0]}
			if {$submesh=="eyes"} {set eyesframe $entry} {set eyesframe [lindex $entry 1]}
			if {[string is alpha $faceframe]} {set faceframe [lindex $current_common_fanim 0]}
			if {[string is alpha $eyesframe]} {
				if {[string first $eyesframe "lrou"]==-1} {
					set eyesframe [lindex $current_common_fanim 1]
				} else {
					set add [string map {awake 0 normal 4 dizzy 8 tired 12} [lindex [split $current_common_mood "_"] 1]]
					set add [expr {14+$add}]
					switch $eyesframe {
						"r" {incr add 1}
						"o" {incr add 2}
						"u" {incr add 3}
					}
					set eyesframe $add
				}
			}
			lappend facelist $faceframe
			lappend eyeslist $eyesframe
			incr framecount
		}
		//log "\{$facelist\} \{$eyeslist\}"
		if {$animtype==1} {disable_auto_fanim [expr {$framecount*0.1}]}
		if {$submesh!="eyes"&&!$is_counterwiggle} {
			lappend facelist [lindex $current_common_fanim 0]
			//log "[gettime] set_textureanimation this 3 \{$facelist\} $startframe $animtype"
			eval "set_textureanimation this 3 \{$facelist\} $startframe $animtype"
		}
		if {$submesh!="face"} {
			lappend eyeslist [lindex $current_common_fanim 1]
			//log "[gettime] set_textureanimation this 4 \{$eyeslist\} $startframe $animtype"
			eval "set_textureanimation this 4 \{$eyeslist\} $startframe $animtype"
		}
	}
	proc disable_auto_fanim {{time 0} {mesh none}} {
		global auto_fanim_state
		set auto_fanim_state 0
		if {$time} {
			//log "timing evt_fanim_enable at [expr {[gettime] + $time}]"
	//		if {$mesh=="none"} {
				timer_event this evt_fanim_enable -attime [expr {[gettime] + $time}]
	//		} else {
	//			timer_event this evt_fanim_enable -attime [expr {[gettime] + $time}] -text1 $mesh
	//		}
		}
	}
	proc enable_auto_fanim {{mesh ""}} {
		global auto_fanim_state current_common_fanim
		set auto_fanim_state 1
	//	if {$mesh!=""} {
	//		if {$mesh=="eyes"} {set id 1} {set id 0}
	//		set_fanim [list $mesh [lindex $current_common_fanim $id]]
	//	} else {
			set_fanim $current_common_fanim
	//	}
	}
	proc set_fanim_feeling {feeling} {
		global fanim_$feeling current_common_fanim eye_focus auto_fanim_state current_fanim
		set current_common_fanim [subst \$fanim_$feeling]
		if {$eye_focus&&$auto_fanim_state} {
			if {[obj_valid $eye_focus]} {
				adjust_eye_focus
			} else {
				set eye_focus 0
			}
		} elseif {$current_common_fanim==$current_fanim} {
			return
		}
		set_fanim $current_common_fanim
	}
	proc set_eye_focus {ref} {
		global eye_focus
		set eye_focus $ref
	}
	proc adjust_eye_focus {} {
		global eye_focus current_common_fanim
		if {[dist_between this $eye_focus]<10} {
			if {[string first [get_objtype $eye_focus] "gnomemonster"]!=-1} {
				set efp [vector_add [get_pos $eye_focus] [get_linkpos $eye_focus 10]]
			} else {
				set efp [get_pos $eye_focus]
			}
			set mpos [vector_add [get_pos this] [get_linkpos this 10]]
			set efy [expr {[lindex $efp 1]-[lindex $mpos 1]}]
			set efl [vector_dist3d $mpos $efp]
			set angle [get_anglexz $mpos $efp]
			set angle [expr {$angle - [get_roty this]}]
			if {$efl>0.01} {set height [expr {atan($efy/$efl)}]} {set height 0.0}
			if {$angle<-3.14} {fincr angle 6.28} {if {$angle>3.14} {fincr angle -6.28}}
			if {abs($angle)<0.5&&abs($height)<0.4||abs($angle)>2.14} {
				set dir "v"
			} elseif {abs($angle)<abs($height)} {
				if {$height>0.0} {set dir "u"} {set dir "o"}
			} else {
				if {$angle>0.0} {set dir "l"} {set dir "r"}
			}
			if {$dir!="v"} {lrep current_common_fanim 1 $dir}
			//log "setting focus of [get_objname this] to $eye_focus ($dir) $angle $height"
		}
	}
}
